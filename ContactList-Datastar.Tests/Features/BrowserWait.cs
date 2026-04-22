using OpenQA.Selenium;

namespace ContactList.Tests.Features;

internal static class BrowserWait
{
    public static void Until(IWebDriver driver, Func<IWebDriver, bool> condition, int timeoutSeconds = 10)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        Exception? lastError = null;

        while (DateTime.UtcNow < timeoutAt)
        {
            try
            {
                if (condition(driver))
                    return;
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
