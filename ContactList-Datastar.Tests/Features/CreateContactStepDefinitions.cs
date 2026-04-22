using OpenQA.Selenium;
using Reqnroll;
using Xunit;

namespace ContactList.Tests.Features;

[Binding]
public sealed class CreateContactStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public CreateContactStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When("I click the {string} button")]
    public void WhenIClickTheButton(string buttonText)
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var button = driver.FindElements(By.TagName("button"))
                .FirstOrDefault(b => b.Text.Trim() == buttonText && b.Displayed);
            if (button == null) return false;
            button.Click();
            return true;
        });
    }

    [Then("I should see the create contact form")]
    public void ThenIShouldSeeTheCreateContactForm()
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var form = driver.FindElements(By.CssSelector("div[data-show] .card")).FirstOrDefault();
            return form != null && form.Displayed;
        });
    }

    [Then("I should not see the create contact form")]
    public void ThenIShouldNotSeeTheCreateContactForm()
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var form = driver.FindElements(By.CssSelector("div[data-show] .card")).FirstOrDefault();
            return form == null || !form.Displayed;
        });
    }

    [When("I fill in the create form with name {string}, email {string}, phone {string}, category {string}")]
    public void WhenIFillInTheCreateForm(string name, string email, string phone, string category)
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var inputs = driver.FindElements(By.CssSelector("div[data-show] input.form-control"));
            return inputs.Count >= 3 && inputs.All(i => i.Displayed);
        });

        var driver = GetDriver();
        var formInputs = driver.FindElements(By.CssSelector("div[data-show] input.form-control"));
        if (!string.IsNullOrEmpty(name))
        {
            formInputs[0].Clear();
            formInputs[0].SendKeys(name);
        }
        if (!string.IsNullOrEmpty(email))
        {
            formInputs[1].Clear();
            formInputs[1].SendKeys(email);
        }
        if (!string.IsNullOrEmpty(phone))
        {
            formInputs[2].Clear();
            formInputs[2].SendKeys(phone);
        }

        if (!string.IsNullOrEmpty(category))
        {
            var select = driver.FindElement(By.CssSelector("div[data-show] select.form-control"));
            var option = select.FindElements(By.TagName("option"))
                .FirstOrDefault(o => o.GetAttribute("value") == category);
            option?.Click();
        }
    }

    [When("I click the {string} button in the create form")]
    public void WhenIClickTheButtonInTheCreateForm(string buttonText)
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var form = driver.FindElement(By.CssSelector("div[data-show] .card"));
            var button = form.FindElements(By.TagName("button"))
                .FirstOrDefault(b => b.Text.Trim() == buttonText);
            if (button == null) return false;
            button.Click();
            return true;
        });
    }

    [Then("I should see a validation error for the name field in the create form")]
    public void ThenIShouldSeeAValidationErrorForTheNameFieldInTheCreateForm()
    {
        AssertCreateFormFieldHasError(fieldIndex: 0);
    }

    [Then("I should see a validation error for the email field in the create form")]
    public void ThenIShouldSeeAValidationErrorForTheEmailFieldInTheCreateForm()
    {
        AssertCreateFormFieldHasError(fieldIndex: 1);
    }

    [Then("I should see a validation error for the phone field in the create form")]
    public void ThenIShouldSeeAValidationErrorForThePhoneFieldInTheCreateForm()
    {
        AssertCreateFormFieldHasError(fieldIndex: 2);
    }

    [Then("I should see a validation error for the category field in the create form")]
    public void ThenIShouldSeeAValidationErrorForTheCategoryFieldInTheCreateForm()
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var errors = driver.FindElements(By.CssSelector("div[data-show] .text-danger"));
            return errors.Any(e => !string.IsNullOrWhiteSpace(e.Text));
        });
    }

    private void AssertCreateFormFieldHasError(int fieldIndex)
    {
        BrowserWait.Until(GetDriver(), driver =>
        {
            var errors = driver.FindElements(By.CssSelector("div[data-show] .text-danger")).ToList();
            if (errors.Count <= fieldIndex) return false;
            return !string.IsNullOrWhiteSpace(errors[fieldIndex].Text);
        });
    }

    private IWebDriver GetDriver() => _scenarioContext.Get<IWebDriver>(nameof(IWebDriver));
}
