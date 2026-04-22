Feature: Delete a contact
  As a user
  I want to remove contacts I no longer need
  So that my list stays relevant

  Background:
    Given the contacts application is running
    And I open the contacts page
    And I should see the seeded contact "Alice Smith"

  Scenario: Each contact row has a Delete button
    Then each contact row should have an "Delete" button

  Scenario: Deleting a contact removes it from the list
    When I click the "Delete" button for "Bob Jones"
    Then I should not see "Bob Jones" in the contact list

  Scenario: Deleting one contact leaves the others intact
    When I click the "Delete" button for "Bob Jones"
    Then I should see "Alice Smith" in the contact list
    And I should see "Carol White" in the contact list
    And I should not see "Bob Jones" in the contact list
