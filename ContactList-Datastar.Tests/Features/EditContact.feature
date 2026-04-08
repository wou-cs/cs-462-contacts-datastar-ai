Feature: Edit a contact
  As a user
  I want to edit an existing contact in-place
  So that I can correct or update their information

  Background:
    Given the contacts application is running
    And I open the contacts page
    And I should see the seeded contact "Alice Smith"

  Scenario: Edit button is visible for each contact
    Then each contact row should have an "Edit" button

  Scenario: Clicking Edit shows an inline edit form
    When I click the "Edit" button for "Alice Smith"
    Then I should see an inline edit form for "Alice Smith"
    And the form should be pre-filled with Alice's current details

  Scenario: Saving a valid edit updates the contact
    When I click the "Edit" button for "Alice Smith"
    And I change the name to "Alice Johnson"
    And I click the "Save" button in the edit form
    Then I should see "Alice Johnson" in the contact list
    And I should not see "Alice Smith" in the contact list

  Scenario: Cancelling an edit preserves the original values
    When I click the "Edit" button for "Alice Smith"
    And I change the name to "Alice Johnson"
    And I click the "Cancel" button in the edit form
    Then I should see "Alice Smith" in the contact list
    And I should not see "Alice Johnson" in the contact list

  Scenario: Validation errors shown when saving with invalid data
    When I click the "Edit" button for "Alice Smith"
    And I clear the name field in the edit form
    And I click the "Save" button in the edit form
    Then I should see a validation error for the name field in the edit form
    And "Alice Smith" should remain unchanged in the contact list

  Scenario: Inline validation fires while editing
    When I click the "Edit" button for "Alice Smith"
    And I type an invalid email "not-an-email" in the edit form
    Then I should see a validation error for the email field in the edit form
