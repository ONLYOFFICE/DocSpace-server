# Spec Delta

## MODIFIED Requirements

### Requirement: Single tariff of a self-hosted installation

A self-hosted installation SHALL have one tariff shared by all its portals, read from the licence file
(`branding-and-licensing`), never from a billing service. An Enterprise installation without an activated
licence SHALL get a 30-day trial tariff on first read; one without a licence path (open source) SHALL report
`openSource` true and is never treated as not paid. Every operation that needs the billing service SHALL
answer 403 "Tariff service is not configured" there.

#### Scenario: Integration host without billing

- **WHEN** the owner calls `getCustomerBalance`, `topUpDeposit` or `updatePayment` on an installation with no billing service
- **THEN** the system answers 403 regardless of the caller's role, and an anonymous caller 401

#### Scenario: Open-source installation

- **WHEN** the owner reads `getPortalTariff` on an installation without a licence path
- **THEN** `openSource` is true, `enterprise` and `developer` false and `state` Paid

#### Scenario: Enterprise installation next to an editing service on trial

- **WHEN** the owner reads `getPortalTariff` on an Enterprise installation without a licence file whose editing service reports a trial ending on day D, earlier than 30 days after the first read, once the wizard was completed during the editing service's active trial or the daily re-read has run
- **THEN** `dueDate` is day D

#### Scenario: Enterprise installation before the editing service is read

- **WHEN** the owner reads `getPortalTariff` on such an installation before the wizard was completed during the editing service's active trial and before the daily re-read
- **THEN** `dueDate` is still 30 days after the first read
