# Spec Delta

## ADDED Requirements

### Requirement: Tariff writes are announced

Every write of a tariff, and its deletion, SHALL send `s:change-tariff`, with no data, to the quota room of each
portal it applies to, then the storage, paid-user and room limits of the written tariff as quota feature values and
the free flag when it changed. The shared tariff of a self-hosted installation applies to every active portal, the
default tariff of a hosted one to none. These are best effort: what cannot be read or sent is logged and left out,
and never fails the write.

#### Scenario: Shared tariff written

- **WHEN** the background re-read of a self-hosted installation brings its trial end closer
- **THEN** every active portal's quota room receives `s:change-tariff` and the quota feature values of the tariff

#### Scenario: Tariff deleted

- **WHEN** the tariff of a self-hosted installation is deleted, as when a license is rejected
- **THEN** every active portal's quota room receives `s:change-tariff` and no quota feature value

#### Scenario: Tariff day passed

- **WHEN** a trial tariff becomes not paid only because its end day passed, with no write
- **THEN** no `s:change-tariff` is sent; a client reads the new state when it reads the tariff again

#### Scenario: Quota of the tariff unreadable

- **WHEN** the quota of a written tariff cannot be read
- **THEN** the portals receive `s:change-tariff` without quota feature values, and a warning is logged

#### Scenario: Portals or state unreadable

- **WHEN** the portals of a written tariff, or its state, cannot be read
- **THEN** nothing is sent, a warning is logged, and the write stands

#### Scenario: Socket service address unusable

- **WHEN** a tariff write cannot build the request to the socket service
- **THEN** the write stands and one warning names the number of portals not told

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

- **WHEN** the owner reads `getPortalTariff` on an Enterprise installation without a licence file whose editing service reports an active trial ending on day D, earlier than 30 days after the first read, once the wizard was completed during the editing service's active trial or the daily re-read has run
- **THEN** `dueDate` is day D

#### Scenario: Enterprise installation before the editing service is read

- **WHEN** the owner reads `getPortalTariff` on such an installation before the wizard was completed during the editing service's active trial and before the daily re-read
- **THEN** `dueDate` is still 30 days after the first read
