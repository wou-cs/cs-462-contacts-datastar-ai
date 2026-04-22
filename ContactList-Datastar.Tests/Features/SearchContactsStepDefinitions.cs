using OpenQA.Selenium;
using Reqnroll;

namespace ContactList.Tests.Features;

[Binding]
public sealed class SearchContactsStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public SearchContactsStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When("I type {string} into the search box")]
    public void WhenITypeIntoTheSearchBox(string text)
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var input = driver.FindElement(By.CssSelector("input[placeholder='Search contacts...']"));
            input.Clear();
            input.SendKeys(text);
            return true;
        });
    }

    [When("I clear the search box")]
    public void WhenIClearTheSearchBox()
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var input = driver.FindElement(By.CssSelector("input[placeholder='Search contacts...']"));
            input.Clear();
            // Datastar debounced input won't fire on Clear() alone — nudge it.
            input.SendKeys(" ");
            input.SendKeys(Keys.Backspace);
            return true;
        });
    }

    [Then("the empty-state message should be shown")]
    public void ThenTheEmptyStateMessageShouldBeShown()
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var contactList = driver.FindElement(By.Id("contact-list"));
            return contactList.Text.Contains("No contacts found", StringComparison.Ordinal);
        });
    }

    private IWebDriver GetDriver() => _scenarioContext.Get<IWebDriver>(nameof(IWebDriver));
}
