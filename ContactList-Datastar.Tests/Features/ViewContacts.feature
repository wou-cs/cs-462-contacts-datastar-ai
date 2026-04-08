Feature: View contacts
  As a user
  I want to open the contacts page
  So that I can see the seeded contacts list

  Scenario: Opening the contacts page shows seeded contacts
    Given the contacts application is running
    When I open the contacts page
    Then I should see the contacts heading
    And I should see the seeded contact "Alice Smith"
