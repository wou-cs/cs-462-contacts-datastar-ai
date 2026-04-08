using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Reqnroll;

namespace ContactList.Tests;

[Binding]
public sealed class TestHooks
{
    private readonly ScenarioContext _scenarioContext;

    public TestHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        TestAppHost.Start();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        TestAppHost.Stop();
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1400,1200");

        var chromeBinary = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
        if (File.Exists(chromeBinary))
        {
            options.BinaryLocation = chromeBinary;
        }

        _scenarioContext.Set<IWebDriver>(new ChromeDriver(options), nameof(IWebDriver));
    }

    [AfterScenario]
    public void AfterScenario()
    {
        if (_scenarioContext.TryGetValue(nameof(IWebDriver), out IWebDriver? driver) && driver is not null)
        {
            driver.Quit();
            driver.Dispose();
        }
    }
}
