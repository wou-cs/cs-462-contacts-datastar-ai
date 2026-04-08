using OpenQA.Selenium;
using Reqnroll;
using Xunit;

namespace ContactList.Tests.Features;

[Binding]
public sealed class ViewContactsStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public ViewContactsStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given("the contacts application is running")]
    public void GivenTheContactsApplicationIsRunning()
    {
        Assert.False(string.IsNullOrWhiteSpace(TestAppHost.BaseUrl));
    }

    [Given("I open the contacts page")]
    [When("I open the contacts page")]
    public void WhenIOpenTheContactsPage()
    {
        var driver = GetDriver();
        driver.Navigate().GoToUrl($"{TestAppHost.BaseUrl}/Contact");
    }

    [Then("I should see the contacts heading")]
    public void ThenIShouldSeeTheContactsHeading()
    {
        WaitUntil(driver =>
        {
            var heading = driver.FindElement(By.TagName("h1"));
            return heading.Text == "Contacts";
        });
    }

    [Given("I should see the seeded contact {string}")]
    [Then("I should see the seeded contact {string}")]
    public void ThenIShouldSeeTheSeededContact(string contactName)
    {
        WaitUntil(driver =>
        {
            var contactList = driver.FindElement(By.Id("contact-list"));
            return contactList.Text.Contains(contactName, StringComparison.Ordinal);
        });
    }

    private IWebDriver GetDriver() => _scenarioContext.Get<IWebDriver>(nameof(IWebDriver));

    private void WaitUntil(Func<IWebDriver, bool> condition)
    {
        var driver = GetDriver();
        var timeoutAt = DateTime.UtcNow.AddSeconds(10);
        Exception? lastError = null;

        while (DateTime.UtcNow < timeoutAt)
        {
            try
            {
                if (condition(driver))
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is NoSuchElementException or StaleElementReferenceException)
            {
                lastError = ex;
            }

            Thread.Sleep(100);
        }

        throw new Xunit.Sdk.XunitException(
            $"Condition was not satisfied before timeout. Last error: {lastError?.Message ?? "none"}");
    }
}
