using OpenQA.Selenium;
using Reqnroll;
using Xunit;

namespace ContactList.Tests.Features;

[Binding]
public sealed class EditContactStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public EditContactStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Then("each contact row should have an {string} button")]
    public void ThenEachContactRowShouldHaveAButton(string buttonText)
    {
        WaitUntil(driver =>
        {
            var rows = driver.FindElements(By.CssSelector("#contact-list tbody tr"));
            if (rows.Count == 0) return false;

            foreach (var row in rows)
            {
                var buttons = row.FindElements(By.TagName("button"));
                if (!buttons.Any(b => b.Text.Trim() == buttonText))
                    return false;
            }
            return true;
        });
    }

    [When("I click the {string} button for {string}")]
    public void WhenIClickTheButtonFor(string buttonText, string contactName)
    {
        WaitUntil(driver =>
        {
            var rows = driver.FindElements(By.CssSelector("#contact-list tbody tr"));
            foreach (var row in rows)
            {
                if (row.Text.Contains(contactName, StringComparison.Ordinal))
                {
                    var button = row.FindElements(By.TagName("button"))
                        .FirstOrDefault(b => b.Text.Trim() == buttonText);
                    if (button != null)
                    {
                        button.Click();
                        return true;
                    }
                }
            }
            return false;
        });
    }

    [Then("I should see an inline edit form for {string}")]
    public void ThenIShouldSeeAnInlineEditFormFor(string contactName)
    {
        WaitUntil(driver =>
        {
            var editForm = driver.FindElements(By.Id("edit-form"));
            return editForm.Count > 0 && editForm[0].Displayed;
        });
    }

    [Then("the form should be pre-filled with Alice's current details")]
    public void ThenTheFormShouldBePreFilledWithAlicesCurrentDetails()
    {
        // Wait for Datastar to apply signal values to the bound inputs
        WaitUntil(driver =>
        {
            var inputs = driver.FindElements(By.CssSelector("#edit-form input.form-control"));
            if (inputs.Count < 3) return false;
            // The signal patch may take a moment to propagate to the input values
            var nameValue = inputs[0].GetAttribute("value");
            return nameValue == "Alice Smith";
        });

        var driver = GetDriver();
        var formInputs = driver.FindElements(By.CssSelector("#edit-form input.form-control"));
        Assert.Equal("alice@example.com", formInputs[1].GetAttribute("value"));
        Assert.Equal("503-555-0101", formInputs[2].GetAttribute("value"));
    }

    [When("I change the name to {string}")]
    public void WhenIChangeTheNameTo(string newName)
    {
        WaitUntil(driver =>
        {
            var inputs = driver.FindElements(By.CssSelector("#edit-form input.form-control"));
            if (inputs.Count == 0) return false;
            var nameInput = inputs[0]; // Name is the first input
            nameInput.Clear();
            nameInput.SendKeys(newName);
            return true;
        });
    }

    [When("I click the {string} button in the edit form")]
    public void WhenIClickTheButtonInTheEditForm(string buttonText)
    {
        WaitUntil(driver =>
        {
            var editForm = driver.FindElement(By.Id("edit-form"));
            var button = editForm.FindElements(By.TagName("button"))
                .FirstOrDefault(b => b.Text.Trim() == buttonText);
            if (button != null)
            {
                button.Click();
                return true;
            }
            return false;
        });
    }

    [Then("I should see {string} in the contact list")]
    public void ThenIShouldSeeInTheContactList(string text)
    {
        WaitUntil(driver =>
        {
            var contactList = driver.FindElement(By.Id("contact-list"));
            return contactList.Text.Contains(text, StringComparison.Ordinal);
        });
    }

    [Then("I should not see {string} in the contact list")]
    public void ThenIShouldNotSeeInTheContactList(string text)
    {
        // Wait briefly for the table to update, then assert absence
        Thread.Sleep(500);
        var driver = GetDriver();
        var contactList = driver.FindElement(By.Id("contact-list"));
        Assert.DoesNotContain(text, contactList.Text);
    }

    [When("I clear the name field in the edit form")]
    public void WhenIClearTheNameFieldInTheEditForm()
    {
        WaitUntil(driver =>
        {
            var inputs = driver.FindElements(By.CssSelector("#edit-form input.form-control"));
            if (inputs.Count == 0) return false;
            var nameInput = inputs[0];
            nameInput.Clear();
            nameInput.SendKeys(Keys.Backspace);
            return true;
        });
    }

    [Then("I should see a validation error for the name field in the edit form")]
    public void ThenIShouldSeeAValidationErrorForTheNameFieldInTheEditForm()
    {
        WaitUntil(driver =>
        {
            var editForm = driver.FindElement(By.Id("edit-form"));
            var errorDivs = editForm.FindElements(By.CssSelector(".text-danger"));
            return errorDivs.Any(d => !string.IsNullOrWhiteSpace(d.Text));
        });
    }

    [Then("{string} should remain unchanged in the contact list")]
    public void ThenShouldRemainUnchangedInTheContactList(string contactName)
    {
        // Click cancel first to close the edit form, then verify original is still there
        var driver = GetDriver();
        try
        {
            var cancelButton = driver.FindElement(By.Id("edit-form"))
                .FindElements(By.TagName("button"))
                .FirstOrDefault(b => b.Text.Trim() == "Cancel");
            cancelButton?.Click();
        }
        catch (NoSuchElementException) { }

        WaitUntil(d =>
        {
            var contactList = d.FindElement(By.Id("contact-list"));
            return contactList.Text.Contains(contactName, StringComparison.Ordinal);
        });
    }

    [When("I type an invalid email {string} in the edit form")]
    public void WhenITypeAnInvalidEmailInTheEditForm(string email)
    {
        WaitUntil(driver =>
        {
            var inputs = driver.FindElements(By.CssSelector("#edit-form input.form-control"));
            if (inputs.Count < 2) return false;
            var emailInput = inputs[1]; // Email is the second input
            emailInput.Clear();
            emailInput.SendKeys(email);
            return true;
        });
    }

    [Then("I should see a validation error for the email field in the edit form")]
    public void ThenIShouldSeeAValidationErrorForTheEmailFieldInTheEditForm()
    {
        WaitUntil(driver =>
        {
            var editForm = driver.FindElement(By.Id("edit-form"));
            var emailError = editForm.FindElements(By.CssSelector(".text-danger"))
                .Where(d => !string.IsNullOrWhiteSpace(d.Text));
            return emailError.Any();
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
