# Spec Delta

## MODIFIED Requirements

### Requirement: Running the schedule

The scheduler SHALL run every 15 minutes and start the backup of every active portal whose next
run time (period and hour in the portal time zone, counted from the last run) has passed, unless
the tariff is restricted (`billing`). The run counts against the free allowance; beyond it
a paid session is opened only when the backup service is on. When the backup is not allowed, the
schedule SHALL be deleted and every active DocSpace admin e-mailed "scheduled backup failed".

#### Scenario: Scheduled run on a Business portal

- **WHEN** the hour of a daily schedule passes and the allowance of the month is not used up
- **THEN** a backup is queued as scheduled, `lastBackupTime` is updated, the audit trail records "scheduled backup started" by the backup service, and no completion letter is sent

#### Scenario: Allowance used up, service off

- **WHEN** the scheduler finds the allowance used up and the paid service switched off
- **THEN** the schedule is deleted, the admins receive the failure letter and `getBackupSchedule` answers an empty payload afterwards

#### Scenario: Portal in the grace period

- **WHEN** the tariff of the portal is not paid, or in the grace period without a delayed payment method
- **THEN** the scheduled backup is skipped and the schedule is kept

#### Scenario: Grace period of a delayed payment method

- **WHEN** the tariff of a hosted portal is in the grace period, its billing customer has a delayed payment method, and the hour of its schedule passes
- **THEN** the backup is queued as on a paid tariff and counts against the free allowance
