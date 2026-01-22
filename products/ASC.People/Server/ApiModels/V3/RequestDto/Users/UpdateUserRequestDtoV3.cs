// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.People.ApiModels.V3.RequestDto.Users;

/// <summary>
/// Request model for updating an existing user's profile information.
/// </summary>
/// <remarks>
/// This DTO supports both full updates (PUT) and partial updates (PATCH):
/// - PUT /api/3.0/users/{id}: Full replacement - all fields should be provided
/// - PATCH /api/3.0/users/{id}: Partial update - only provided fields are modified
///
/// Business Rules:
/// - User ID is provided in the route parameter, not in the request body
/// - Cannot change user's email directly (use dedicated email change endpoint)
/// - Cannot change user's ID, creation date, or encrypted identifiers
/// - Changing user type may trigger quota recalculation and permission updates
/// - Department changes immediately affect access control and group memberships
///
/// Permissions Required:
/// - Users can update their own limited profile fields (name, contacts, preferences)
/// - Room administrators can update users within their managed rooms
/// - DocSpace administrators can update any user's profile
/// - Some fields (type, isActive) require administrative permissions
///
/// Side Effects and Cascading Changes:
/// - Changing user type recalculates quota and may affect billing
/// - Adding/removing departments updates group memberships and access permissions
/// - Deactivating users (isActive: false) may trigger data reassignment prompts
/// - Profile changes invalidate cached user data across the system
/// - Real-time notifications sent to connected clients
/// - Webhook events fired for external integrations
///
/// Idempotency:
/// PUT operations are idempotent - sending the same request multiple times
/// produces the same result. PATCH operations are also idempotent.
///
/// Usage Scenarios:
/// - Updating employee information after role changes or promotions
/// - Correcting typos or outdated information in user profiles
/// - Reassigning users to different departments during reorganization
/// - Temporarily disabling user accounts without deletion
/// - Bulk profile updates via API automation
///
/// Performance Considerations:
/// - Batch update endpoints available for bulk operations (better performance)
/// - Consider using PATCH for updating single fields to reduce payload size
/// - Profile photo updates should use dedicated photo upload endpoint
/// </remarks>
/// <example>
/// {
///   "firstName": "John",
///   "lastName": "Smith",
///   "title": "Lead Developer",
///   "department": ["550e8400-e29b-41d4-a716-446655440000"],
///   "location": "San Francisco Office",
///   "isActive": true
/// }
/// </example>
public class UpdateUserRequestDtoV3
{
    /// <summary>
    /// The user's first name (given name).
    /// Updates how the user is identified and addressed in the system.
    /// </summary>
    /// <example>John</example>
    [StringLength(255, ErrorMessage = "First name cannot exceed 255 characters")]
    public string FirstName { get; set; }

    /// <summary>
    /// The user's last name (family name).
    /// Updates how the user is identified and addressed in the system.
    /// </summary>
    /// <example>Smith</example>
    [StringLength(255, ErrorMessage = "Last name cannot exceed 255 characters")]
    public string LastName { get; set; }

    /// <summary>
    /// The user's job title or position in the organization.
    /// Visible on user profiles and collaboration interfaces.
    /// </summary>
    /// <example>Lead Developer</example>
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; }

    /// <summary>
    /// List of department (group) IDs the user should belong to.
    /// Replaces existing department memberships with this new list.
    /// Each ID must reference an existing group.
    /// </summary>
    /// <example>["550e8400-e29b-41d4-a716-446655440000"]</example>
    public Guid[] Department { get; set; }

    /// <summary>
    /// The user's physical location, office, or work site.
    /// Free-text field for organizational reference.
    /// </summary>
    /// <example>San Francisco Office</example>
    [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
    public string Location { get; set; }

    /// <summary>
    /// The user's biological sex for profile purposes.
    /// Valid values: Male, Female
    /// </summary>
    /// <example>Male</example>
    public SexEnum? Sex { get; set; }

    /// <summary>
    /// The user's date of birth in ISO 8601 format.
    /// Used for birthday notifications and age-related features.
    /// </summary>
    /// <example>1990-05-15</example>
    public ApiDateTime Birthday { get; set; }

    /// <summary>
    /// The user's employment start date with the organization.
    /// Used for anniversary tracking and tenure calculations.
    /// </summary>
    /// <example>2024-01-01</example>
    public ApiDateTime WorksFrom { get; set; }

    /// <summary>
    /// Optional administrative notes or comments about the user.
    /// Not visible to the user themselves, only to administrators.
    /// </summary>
    /// <example>Promoted to Lead Developer in Q2 2024</example>
    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string Comment { get; set; }

    /// <summary>
    /// List of contact methods for the user (phones, social media, messengers).
    /// Replaces existing contacts with this new list.
    /// </summary>
    public IEnumerable<Contact> Contacts { get; set; }

    /// <summary>
    /// Whether the user has opted in to receive marketing communications.
    /// Changes to this field respect GDPR and privacy regulations.
    /// </summary>
    /// <example>false</example>
    public bool? AllowMarketing { get; set; }

    /// <summary>
    /// Whether the user account is active and can access the system.
    /// Set to false to disable the account temporarily without deletion.
    /// Disabled users cannot log in but their data is preserved.
    /// Requires administrative permissions to modify.
    /// </summary>
    /// <example>true</example>
    public bool? IsActive { get; set; }

    /// <summary>
    /// The user type determining access level and permissions.
    /// Valid values: User, Guest, DocSpaceAdmin, RoomAdmin
    /// Changing this may trigger quota recalculation and affect billing.
    /// Requires administrative permissions to modify.
    /// </summary>
    /// <example>User</example>
    public EmployeeType? Type { get; set; }

    /// <summary>
    /// The user's preferred culture/language code (e.g., en-US, de-DE).
    /// Changes the language of UI and notifications for this user.
    /// </summary>
    /// <example>en-US</example>
    [StringLength(10, ErrorMessage = "Culture name cannot exceed 10 characters")]
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$", ErrorMessage = "Invalid culture format. Use format: en-US")]
    public string CultureName { get; set; }
}
