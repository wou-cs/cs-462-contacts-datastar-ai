Feature: Search contacts
  As a user
  I want to search the contact list
  So that I can quickly find a specific person

  Background:
    Given the contacts application is running
    And I open the contacts page
    And I should see the seeded contact "Alice Smith"

  Scenario: Searching by name filters the list to the matching contact
    When I type "Alice" into the search box
    Then I should see "Alice Smith" in the contact list
    And I should not see "Bob Jones" in the contact list
    And I should not see "Carol White" in the contact list

  Scenario: Searching by email filters the list
    When I type "bob@example" into the search box
    Then I should see "Bob Jones" in the contact list
    And I should not see "Alice Smith" in the contact list

  Scenario: Searching by phone filters the list
    When I type "0103" into the search box
    Then I should see "Carol White" in the contact list
    And I should not see "Alice Smith" in the contact list

  Scenario: Searching by category filters the list
    When I type "Family" into the search box
    Then I should see "Carol White" in the contact list
    And I should not see "Alice Smith" in the contact list
    And I should not see "Bob Jones" in the contact list

  Scenario: Clearing the search restores all contacts
    When I type "Alice" into the search box
    And I clear the search box
    Then I should see "Alice Smith" in the contact list
    And I should see "Bob Jones" in the contact list
    And I should see "Carol White" in the contact list

  Scenario: Searching for a non-existent term shows the empty state
    When I type "Zebra" into the search box
    Then the empty-state message should be shown

  Scenario: Search is case-insensitive
    When I type "alice" into the search box
    Then I should see "Alice Smith" in the contact list
