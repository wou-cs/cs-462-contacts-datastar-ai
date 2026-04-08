#!/usr/bin/env bash
set -euo pipefail

# start-issue.sh — Pick a Jira issue and create a Gitflow feature branch from dev.
#
# Usage:
#   ./start-issue.sh              # interactive issue picker
#   ./start-issue.sh WOUDEMO-14   # skip picker, use issue key directly
#
# Prerequisites:
#   .env file with JIRA_SITE, JIRA_USER_EMAIL, JIRA_API_TOKEN, JIRA_PROJECT

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
BOLD='\033[1m'
RESET='\033[0m'

die() { echo -e "${RED}Error: $1${RESET}" >&2; exit 1; }
info() { echo -e "${CYAN}$1${RESET}"; }
ok() { echo -e "${GREEN}$1${RESET}"; }

# ── Load .env ─────────────────────────────────────────────────────
ENV_FILE="$SCRIPT_DIR/.env"
[[ -f "$ENV_FILE" ]] || die ".env file not found at $ENV_FILE"
set -a; source "$ENV_FILE"; set +a

[[ -n "${JIRA_SITE:-}" ]]      || die "JIRA_SITE not set in .env"
[[ -n "${JIRA_USER_EMAIL:-}" ]] || die "JIRA_USER_EMAIL not set in .env"
[[ -n "${JIRA_API_TOKEN:-}" ]]  || die "JIRA_API_TOKEN not set in .env"
[[ -n "${JIRA_PROJECT:-}" ]]    || die "JIRA_PROJECT not set in .env"

JIRA_AUTH="${JIRA_USER_EMAIL}:${JIRA_API_TOKEN}"
JIRA_BASE="https://${JIRA_SITE}/rest/api/3"

# Helper: call Jira REST API (GET)
jira_get() {
    curl -sf -u "$JIRA_AUTH" -H "Accept: application/json" "$1"
}

# Helper: call Jira REST API (POST with JSON body)
jira_post() {
    curl -sf -u "$JIRA_AUTH" -H "Content-Type: application/json" -H "Accept: application/json" -X POST -d "$2" "$1"
}

# ── Preflight checks ───────────────────────────────────────────────
command -v git    >/dev/null || die "git is not installed."
command -v curl   >/dev/null || die "curl is not installed."
command -v python3 >/dev/null || die "python3 is not installed."

# ── Ensure dev branch exists locally and is up to date ─────────────
git fetch origin --prune

if ! git rev-parse --verify origin/dev >/dev/null 2>&1; then
    die "Remote dev branch not found. Push a dev branch to origin first."
fi

if ! git rev-parse --verify dev >/dev/null 2>&1; then
    info "Creating local dev branch tracking origin/dev..."
    git branch dev origin/dev
fi

# ── Select a Jira issue ───────────────────────────────────────────
if [[ ${1:-} ]]; then
    ISSUE_KEY="$1"
    info "Using issue key: $ISSUE_KEY"
else
    info "Fetching open issues from ${JIRA_PROJECT}..."
    echo ""

    JQL="project=${JIRA_PROJECT}+AND+status!=Done+ORDER+BY+created+DESC"
    ISSUES_JSON=$(jira_get "${JIRA_BASE}/search/jql?jql=${JQL}&maxResults=20&fields=key,summary,status") \
        || die "Could not fetch issues. Check your .env credentials."

    # Format issues for display / selection
    ISSUE_LINES=$(echo "$ISSUES_JSON" | python3 -c "
import sys, json
data = json.load(sys.stdin)
for issue in data.get('issues', []):
    key = issue['key']
    summary = issue['fields']['summary']
    status = issue['fields']['status']['name']
    print(f'{key:<14}{status:<16}{summary}')
")

    if [[ -z "$ISSUE_LINES" ]]; then
        die "No open issues found in project ${JIRA_PROJECT}."
    fi

    # Try fzf for fuzzy picking, fall back to numbered list
    if command -v fzf >/dev/null 2>&1; then
        SELECTED=$(echo "$ISSUE_LINES" | fzf --prompt="Select issue: " --height=15 --reverse) || true
        ISSUE_KEY=$(echo "$SELECTED" | awk '{print $1}')
    fi

    if [[ -z "${ISSUE_KEY:-}" ]]; then
        echo -e "${BOLD}Open issues:${RESET}"
        echo ""
        echo "$ISSUE_LINES" | nl -ba -w2 -s'  '
        echo ""
        read -rp "Enter issue number or key: " SELECTION

        if [[ "$SELECTION" =~ ^[0-9]+$ ]] && (( SELECTION > 0 )); then
            ISSUE_KEY=$(echo "$ISSUE_LINES" | sed -n "${SELECTION}p" | awk '{print $1}')
        else
            ISSUE_KEY="$SELECTION"
        fi
    fi
fi

[[ -z "${ISSUE_KEY:-}" ]] && die "No issue selected."

# Normalize to uppercase
ISSUE_KEY=$(echo "$ISSUE_KEY" | tr '[:lower:]' '[:upper:]' | xargs)

# ── Fetch issue details ──────────────────────────────────────────
info "Fetching issue $ISSUE_KEY..."
ISSUE_JSON=$(jira_get "${JIRA_BASE}/issue/${ISSUE_KEY}?fields=summary,status") \
    || die "Could not fetch issue $ISSUE_KEY. Is the key correct?"

ISSUE_SUMMARY=$(echo "$ISSUE_JSON" | python3 -c "import sys,json; print(json.load(sys.stdin)['fields']['summary'])")
ISSUE_STATUS=$(echo "$ISSUE_JSON" | python3 -c "import sys,json; print(json.load(sys.stdin)['fields']['status']['name'])")

echo -e "  ${BOLD}$ISSUE_KEY${RESET}: $ISSUE_SUMMARY  [${ISSUE_STATUS}]"
echo ""

# ── Build branch name ─────────────────────────────────────────────
# Slugify: lowercase, replace non-alphanum with hyphens, collapse, trim
SLUG=$(echo "$ISSUE_SUMMARY" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/-/g' | sed 's/--*/-/g' | sed 's/^-//;s/-$//' | cut -c1-50)
BRANCH_NAME="feature/${ISSUE_KEY}-${SLUG}"

echo -e "Branch: ${BOLD}$BRANCH_NAME${RESET}"
read -rp "Create this branch? [Y/n] " CONFIRM
CONFIRM=${CONFIRM:-Y}
[[ "$CONFIRM" =~ ^[Yy]$ ]] || die "Aborted."

# ── Create feature branch from dev ────────────────────────────────
info "Updating dev from origin..."
git checkout dev
git pull origin dev

info "Creating feature branch..."
git checkout -b "$BRANCH_NAME"

# ── Transition issue to In Progress (best-effort) ─────────────────
info "Transitioning $ISSUE_KEY to In Progress..."

# Find the "In Progress" transition ID
TRANSITIONS_JSON=$(jira_get "${JIRA_BASE}/issue/${ISSUE_KEY}/transitions") || true

TRANSITION_ID=$(echo "${TRANSITIONS_JSON:-{}}" | python3 -c "
import sys, json
data = json.load(sys.stdin)
for t in data.get('transitions', []):
    if 'progress' in t['name'].lower():
        print(t['id'])
        break
" 2>/dev/null) || true

if [[ -n "${TRANSITION_ID:-}" ]]; then
    jira_post "${JIRA_BASE}/issue/${ISSUE_KEY}/transitions" "{\"transition\":{\"id\":\"${TRANSITION_ID}\"}}" >/dev/null 2>&1 \
        && ok "  Moved to In Progress." \
        || echo -e "  ${RED}Could not transition (workflow may differ).${RESET}"
else
    echo -e "  ${RED}No 'In Progress' transition available from current status.${RESET}"
fi

echo ""
ok "Ready to work on $ISSUE_KEY!"
ok "Branch: $BRANCH_NAME"
echo ""
echo "When done, push and create a PR into dev:"
echo "  gpush \"$ISSUE_KEY: <description>\""
echo "  gh pr create --base dev"
