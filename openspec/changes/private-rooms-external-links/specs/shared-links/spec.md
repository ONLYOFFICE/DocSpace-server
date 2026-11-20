# Spec Delta: shared-links

## MODIFIED Requirements

Current texts, quoted from `openspec/specs/shared-links/spec.md` at `2029af1663`:

> ### Requirement: Link settings by room type
>
> The system SHALL offer link settings according to the section or room type that contains the entity:
> access rights, the "who has access" choice ("Anyone with the link" or "DocSpace users only"), a password,
> the deny-download flag and a lifetime (expiration). Where a setting is not offered its value is fixed, as
> the matrix scenario and the fixed-value scenarios state.
>
> #### Scenario: Link settings matrix by location
>
> - **WHEN** a link is created or edited on an entity in the given location
> - **THEN** the settings that can be chosen follow this table
>
> | Location            | Access rights | Who has access | Password | Deny download | Lifetime |
> |---------------------|:-------------:|:--------------:|:--------:|:-------------:|:--------:|
> | My documents        | ✔ | ✔ | ✔ | ✔ | ✔ |
> | Public room         | ✔ | ✗ | ✔ | ✔ | additional links only |
> | Form filling room   | ✗ | ✔ | ✔ | ✔ | room link ✗ |
> | Collaboration room  | ✔ | ✔ | ✔ | ✔ | ✔ |
> | Virtual Data Room   | ✔ | ✗ | ✔ | ✔ | ✔ |
> | Custom room         | ✔ | ✔ | ✔ | ✔ | ✔ |
>
> #### Scenario: Fixed audience of Public room and VDR links
>
> - **WHEN** a link exists in a Public room or in a Virtual Data Room
> - **THEN** a Public room link is available to anyone with the link and a VDR link to DocSpace users only, whatever the request asked for
>
> #### Scenario: Form filling room link with the editing right
>
> - **WHEN** a link in a Form filling room is created or edited with the editing, review or comment right
> - **THEN** the system rejects the request (403, "The role is not available for this user type"); the room link grants reading or form filling
>
> #### Scenario: Form link inside a Form filling room with the read right
>
> - **WHEN** a link to a form or folder inside a Form filling room is requested with the read right
> - **THEN** the system rejects the request (403); such a link grants form filling only
>
> #### Scenario: Primary room link does not expire
>
> - **WHEN** an expiration date is set on the primary link of a Public room or a Form filling room
> - **THEN** the request succeeds and the stored link has no expiration
>
> #### Scenario: Additional link of a Public room expires
>
> - **WHEN** an expiration date is set on an additional link of a Public room, or on a link of a file or folder inside a Public or Form filling room
> - **THEN** the expiration date is saved
>
> #### Scenario: Password and download restriction everywhere
>
> - **WHEN** a password or the deny-download flag is set on a link in any section or room type
> - **THEN** the setting is saved

> ### Requirement: Deny-download on links combines with the VDR room setting
>
> The deny-download flag of a link SHALL be stored as requested in every room type. The effective rule for a
> link visitor in a Virtual Data Room SHALL be the stricter of the room-wide restriction on downloading and
> copying content and the link's own flag. The room's owner, Room managers, Content creators and Editors are
> not restricted.
>
> #### Scenario: Room-wide restriction is on
>
> - **WHEN** a VDR has the room-wide restriction turned on and a visitor downloads a file through a link whose deny-download flag is off
> - **THEN** the download is refused (403)
>
> #### Scenario: Room-wide restriction is off
>
> - **WHEN** a VDR has the room-wide restriction turned off, one file link has deny-download on and another has it off
> - **THEN** the download through the first link is refused and through the second succeeds

### Requirement: Link settings by room type

The system SHALL offer link settings according to the section or room type that contains the entity:
access rights, the "who has access" choice ("Anyone with the link" or "DocSpace users only"), a password,
the deny-download flag and a lifetime (expiration). Where a setting is not offered its value is fixed, as
the scenarios state. A link of a private item follows its room type's matrix except as `private-rooms`
states: read only, `passwordRequired` instead of a password, no deny-download.

#### Scenario: Link settings matrix by location

- **WHEN** a link is created or edited on an entity in the given location
- **THEN** the settings that can be chosen follow this table

| Location            | Access rights | Who has access | Password | Deny download | Lifetime |
|---------------------|:-------------:|:--------------:|:--------:|:-------------:|:--------:|
| My documents        | ✔ | ✔ | ✔ | ✔ | ✔ |
| Public room         | ✔ | ✗ | ✔ | ✔ | additional links only |
| Form filling room   | ✗ | ✔ | ✔ | ✔ | room link ✗ |
| Collaboration room  | ✔ | ✔ | ✔ | ✔ | ✔ |
| Virtual Data Room   | ✔ | ✗ | ✔ | ✔ | ✔ |
| Custom room         | ✔ | ✔ | ✔ | ✔ | ✔ |
| Private Collaboration, VDR or Custom room | read only | as its room type | `passwordRequired` only | ✗ | ✔ |

#### Scenario: Fixed audience of Public room and VDR links

- **WHEN** a link exists in a Public room or in a Virtual Data Room
- **THEN** a Public room link is available to anyone with the link and a VDR link to DocSpace users only, whatever the request asked for

#### Scenario: Form filling room link with the editing right

- **WHEN** a link in a Form filling room is created or edited with the editing, review or comment right
- **THEN** the system rejects the request (403, "The role is not available for this user type"); the room link grants reading or form filling

#### Scenario: Form link inside a Form filling room with the read right

- **WHEN** a link to a form or folder inside a Form filling room is requested with the read right
- **THEN** the system rejects the request (403); such a link grants form filling only

#### Scenario: Primary room link does not expire

- **WHEN** an expiration date is set on the primary link of a Public room or a Form filling room
- **THEN** the request succeeds and the stored link has no expiration

#### Scenario: Additional link of a Public room expires

- **WHEN** an expiration date is set on an additional link of a Public room, or on a link of a file or folder inside a Public or Form filling room
- **THEN** the expiration date is saved

#### Scenario: Password and download restriction everywhere

- **WHEN** a password or the deny-download flag is set on a link in any section or room type, an item of a private room excepted
- **THEN** the setting is saved

#### Scenario: Password and download restriction on a private item

- **WHEN** `password` or `denyDownload:true` is sent for a link of a private item
- **THEN** the request is refused (400 for the password, 403 for the flag) and nothing is stored

### Requirement: Deny-download on links combines with the VDR room setting

The deny-download flag of a link SHALL be stored as requested in every room type except on a private item. The effective
rule for a link visitor in a Virtual Data Room SHALL be the stricter of the room-wide restriction on downloading and copying
content and the link's own flag. In a private Virtual Data Room the restriction instead stops its links, as `private-rooms`
states. The room's owner, Room managers, Content creators and Editors are not restricted.

#### Scenario: Room-wide restriction is on

- **WHEN** a VDR has the room-wide restriction turned on and a visitor downloads a file through a link whose deny-download flag is off
- **THEN** the download is refused (403)

#### Scenario: Room-wide restriction is off

- **WHEN** a VDR has the room-wide restriction turned off, one file link has deny-download on and another has it off
- **THEN** the download through the first link is refused and through the second succeeds

#### Scenario: Private VDR with the restriction on

- **WHEN** a private VDR has the room-wide restriction turned on and holds a link
- **THEN** a new link is refused (403) and the existing one is served as an invalid link
