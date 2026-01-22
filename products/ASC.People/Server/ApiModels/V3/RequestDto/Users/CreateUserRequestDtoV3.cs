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
/// Request model for creating a new user in the DocSpace system.
/// </summary>
/// <remarks>
/// This DTO is used to create a new activated user with full profile information.
/// The user will be immediately active in the system upon creation.
///
/// Business Rules:
/// - Email must be unique within the tenant (organization)
/// - Password must meet the tenant's password policy requirements (minimum length, complexity)
/// - User type determines access permissions, features available, and quota consumption
/// - Department IDs must reference existing groups in the system
/// - At least first name and last name are required for user identification
///
/// User Types and Their Meanings:
/// - User: Standard user with full collaboration features
/// - Guest: Limited user for external collaboration
/// - DocSpaceAdmin: Administrator with full system access
/// - RoomAdmin: Administrator for specific rooms/workspaces
///
/// Quota Impact:
/// - Creating paid user types (DocSpaceAdmin, RoomAdmin) consumes paid user quota
/// - Creating free types (Guest, User) may consume different quota pools
/// - Creation will fail if quota limits would be exceeded
///
/// Post-Creation Actions:
/// - Welcome email is sent to the user's email address
/// - User receives login credentials (if password provided)
/// - User is automatically added to specified departments
/// - Audit log entry is created for compliance tracking
///
/// Usage Scenarios:
/// - Employee onboarding during HR processes
/// - Adding external collaborators (guests) to projects
/// - Bulk user imports via API integration
/// - Self-service user registration (with appropriate permissions)
///
/// Alternative Approaches:
/// - For invitation-based onboarding where users set their own passwords:
///   Use POST /api/3.0/users/invitations instead
/// - For importing large numbers of users:
///   Consider batch import endpoints for better performance
/// </remarks>
/// <example>
/// {
///   "email": "john.doe@company.com",
///   "firstName": "John",
///   "lastName": "Doe",
///   "password": "SecurePass123!",
///   "type": "User",
///   "title": "Senior Developer",
///   "department": ["550e8400-e29b-41d4-a716-446655440000"],
///   "location": "New York Office",
///   "birthday": "1990-05-15",
///   "worksFrom": "2024-01-01",
///   "cultureName": "en-US"
/// }
/// </example>
public class CreateUserRequestDtoV3
{
    /// <summary>
    /// The user's email address. Must be unique within the tenant.
    /// This will be used for authentication and system notifications.
    /// </summary>
    /// <example>john.doe@company.com</example>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
    public string Email { get; set; }

    /// <summary>
    /// The user's password for authentication. Must meet password policy requirements.
    /// Mutually exclusive with passwordHash - provide only one.
    /// </summary>
    /// <example>SecurePass123!</example>
    [StringLength(512, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 512 characters")]
    public string Password { get; set; }

    /// <summary>
    /// The user's password as a pre-hashed value. Alternative to providing plain password.
    /// Use this when integrating with external systems that already have hashed passwords.
    /// Mutually exclusive with password - provide only one.
    /// </summary>
    /// <example>5f4dcc3b5aa765d61d8327deb882cf99</example>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The user's first name (given name).
    /// Used for display purposes and user identification.
    /// </summary>
    /// <example>John</example>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(255, ErrorMessage = "First name cannot exceed 255 characters")]
    public string FirstName { get; set; }

    /// <summary>
    /// The user's last name (family name).
    /// Used for display purposes and user identification.
    /// </summary>
    /// <example>Doe</example>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(255, ErrorMessage = "Last name cannot exceed 255 characters")]
    public string LastName { get; set; }

    /// <summary>
    /// The user type determining access level, permissions, and feature availability.
    /// Valid values: User, Guest, DocSpaceAdmin, RoomAdmin
    /// This affects quota consumption and available functionality.
    /// </summary>
    /// <example>User</example>
    [Required(ErrorMessage = "User type is required")]
    public EmployeeType Type { get; set; }

    /// <summary>
    /// The user's job title or position in the organization.
    /// Displayed on user profiles and in collaboration interfaces.
    /// </summary>
    /// <example>Senior Developer</example>
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string Title { get; set; }

    /// <summary>
    /// List of department (group) IDs the user belongs to.
    /// Each ID must reference an existing group in the system.
    /// Users can belong to multiple departments for flexible organization.
    /// </summary>
    /// <example>["550e8400-e29b-41d4-a716-446655440000", "660e8400-e29b-41d4-a716-446655440001"]</example>
    public Guid[] Department { get; set; }

    /// <summary>
    /// The user's physical location, office, or work site.
    /// Free-text field for organizational reference.
    /// </summary>
    /// <example>New York Office</example>
    [StringLength(255, ErrorMessage = "Location cannot exceed 255 characters")]
    public string Location { get; set; }

    /// <summary>
    /// The user's biological sex for profile purposes.
    /// Valid values: Male, Female
    /// Optional field used for personalization and display.
    /// </summary>
    /// <example>Male</example>
    public SexEnum? Sex { get; set; }

    /// <summary>
    /// The user's date of birth in ISO 8601 format (YYYY-MM-DD).
    /// Used for birthday notifications and age-related features.
    /// </summary>
    /// <example>1990-05-15</example>
    public ApiDateTime Birthday { get; set; }

    /// <summary>
    /// The user's employment start date with the organization.
    /// Defaults to the current date if not specified.
    /// Used for anniversary tracking and tenure calculations.
    /// </summary>
    /// <example>2024-01-01</example>
    public ApiDateTime WorksFrom { get; set; }

    /// <summary>
    /// Optional notes or comments about the user for administrative purposes.
    /// Not displayed to the user themselves, only visible to administrators.
    /// </summary>
    /// <example>Transferred from London office in Q1 2024</example>
    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    public string Comment { get; set; }

    /// <summary>
    /// List of contact methods (phone numbers, social media handles, messengers) for the user.
    /// Each contact includes a type (e.g., "phone", "telegram") and a value.
    /// </summary>
    public IEnumerable<Contact> Contacts { get; set; }

    /// <summary>
    /// URL or file path for the user's avatar photo.
    /// Can be a publicly accessible URL or a reference to an uploaded file.
    /// Photo will be resized and optimized automatically.
    /// </summary>
    /// <example>https://cdn.example.com/photos/johndoe.jpg</example>
    [Url(ErrorMessage = "Invalid URL format for avatar")]
    public string AvatarUrl { get; set; }

    /// <summary>
    /// The user's preferred culture/language code in IETF format (e.g., en-US, de-DE, fr-FR).
    /// Determines the language of the user interface and email notifications.
    /// Defaults to the tenant's default culture if not specified.
    /// </summary>
    /// <example>en-US</example>
    [StringLength(10, ErrorMessage = "Culture name cannot exceed 10 characters")]
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$", ErrorMessage = "Invalid culture format. Use format: en-US")]
    public string CultureName { get; set; }

    /// <summary>
    /// Whether the user has opted in to receive marketing emails and promotional updates.
    /// Defaults to false if not specified (user must explicitly opt in).
    /// Respects GDPR and other privacy regulations.
    /// </summary>
    /// <example>false</example>
    public bool? AllowMarketing { get; set; }
}
