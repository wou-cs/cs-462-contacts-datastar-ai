Feature: Create a contact
  As a user
  I want to add a new contact via the inline form
  So that I can keep track of new people

  Background:
    Given the contacts application is running
    And I open the contacts page
    And I should see the seeded contact "Alice Smith"

  Scenario: Clicking Add New Contact reveals the create form
    When I click the "Add New Contact" button
    Then I should see the create contact form

  Scenario: Cancelling the create form hides it
    When I click the "Add New Contact" button
    And I click the "Cancel" button in the create form
    Then I should not see the create contact form

  Scenario: Creating a valid contact adds it to the list
    When I click the "Add New Contact" button
    And I fill in the create form with name "Dave Taylor", email "dave@example.com", phone "503-555-0199", category "Work"
    And I click the "Save" button in the create form
    Then I should see "Dave Taylor" in the contact list

  Scenario: Saving with a missing name shows a validation error
    When I click the "Add New Contact" button
    And I fill in the create form with name "", email "eve@example.com", phone "503-555-0100", category "Friend"
    And I click the "Save" button in the create form
    Then I should see a validation error for the name field in the create form

  Scenario: Saving with an invalid email shows a validation error
    When I click the "Add New Contact" button
    And I fill in the create form with name "Frank Green", email "not-an-email", phone "503-555-0100", category "Friend"
    And I click the "Save" button in the create form
    Then I should see a validation error for the email field in the create form

  Scenario: Saving without selecting a category shows a validation error
    When I click the "Add New Contact" button
    And I fill in the create form with name "Grace Hill", email "grace@example.com", phone "503-555-0100", category ""
    And I click the "Save" button in the create form
    Then I should see a validation error for the category field in the create form
