Feature: View contacts
  As a user
  I want to open the contacts page
  So that I can see the seeded contacts list

  Background:
    Given the contacts application is running
    When I open the contacts page

  Scenario: Opening the contacts page shows seeded contacts
    Then I should see the contacts heading
    And I should see the seeded contact "Alice Smith"

  Scenario: All three seeded contacts appear on page load
    Then I should see the seeded contact "Alice Smith"
    And I should see the seeded contact "Bob Jones"
    And I should see the seeded contact "Carol White"

  Scenario: Contact table shows name, email, phone, and category columns
    Then the contact table should have the following columns:
      | Column   |
      | Name     |
      | Email    |
      | Phone    |
      | Category |
