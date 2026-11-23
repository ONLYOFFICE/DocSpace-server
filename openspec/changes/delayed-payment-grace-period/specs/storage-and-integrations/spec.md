# Spec Delta

## MODIFIED Requirements

### Requirement: Docs Connect trial

`startDocsCloudTrial` SHALL need a configured billing service (403), a trial quota on the installation (404), a tariff
not restricted (402, `billing`) and no Docs Connect subscription on the portal (400). It answers true when the trial is
granted, adding the tenant's address to the Content Security Policy domains, and false when billing declines or its
answer cannot be read; after an unreadable answer the cached tariff is dropped, so the next check reads billing again.

#### Scenario: Subscription already there

- **WHEN** the tariff holds a Docs Connect trial, Docs Connect or Docs Connect Dev Pack quota
- **THEN** the answer is 400

#### Scenario: Granted

- **WHEN** the billing service grants the trial
- **THEN** the answer is true and the address of the assigned tenant is among the allowed domains

#### Scenario: Unreadable billing answer

- **WHEN** the billing service answers with something that is not a readable trial result
- **THEN** the answer is false and the cached tariff is cleared

#### Scenario: Grace period

- **WHEN** the portal is in the grace period, without a delayed payment method, and a DocSpace admin calls `startDocsCloudTrial`
- **THEN** the answer is 402; with a delayed payment method the call goes on to billing

### Requirement: Moving to Docs Connect Dev Pack

`calculateDevPack` SHALL price and `switchToDevPack` SHALL perform the move from an active Docs Connect subscription to
Dev Pack for `quantity` users. Both need a configured billing service and an administrator (403), a billing customer
(404) and a tariff not restricted (402, `billing`); a low `quantity`, no subscription or an existing Dev Pack give 400.

#### Scenario: Anonymous

- **WHEN** an anonymous caller calls either operation
- **THEN** the answer is 401

#### Scenario: No billing service

- **WHEN** any signed-in caller calls either on an installation without a billing service
- **THEN** the answer is 403

#### Scenario: Quantity below the minimum

- **WHEN** `quantity` is below the current number of Docs Connect users or the Dev Pack minimum, by default 10
- **THEN** the answer is 400 and contains "Invalid quantity", and nothing is asked of the billing service

#### Scenario: No Docs Connect subscription

- **WHEN** the portal holds no active Docs Connect subscription
- **THEN** the answer is 400 and contains "DocsCloud subscription is not active"

#### Scenario: DocSpace admin who is not the owner

- **WHEN** a DocSpace admin who is not the owner calls `calculateDevPack` or `switchToDevPack` on a subscribed portal
- **THEN** the switch is priced or performed, and `switchToDevPack` names the caller as the paying participant

#### Scenario: Not an administrator

- **WHEN** a signed-in caller who is not an administrator calls either operation on an installation with a billing service
- **THEN** the answer is 403 and contains "Access denied", and nothing is charged

#### Scenario: Simultaneous switches

- **WHEN** two `switchToDevPack` calls for one portal overlap
- **THEN** they run one after the other and the second gets 400 because Dev Pack is already set
