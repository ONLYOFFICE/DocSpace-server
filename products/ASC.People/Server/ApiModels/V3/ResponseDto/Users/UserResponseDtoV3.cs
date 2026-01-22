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
// the GNU AGPL at: http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.People.ApiModels.V3.ResponseDto.Users;

/// <summary>
/// Response model containing complete user profile information.
/// </summary>
/// <remarks>
/// This DTO represents a full user profile as returned by the v3 API.
/// Includes profile data, system metadata, and hypermedia links for navigation.
///
/// Data Categories:
/// 1. Identity: ID, email, names, display name
/// 2. Employment: Type, status, activation status, title, departments
/// 3. Personal: Birthday, sex, location, contacts
/// 4. System: Creation date, last modified, culture settings
/// 5. Navigation: HATEOAS links to related resources
///
/// HATEOAS Links (Hypermedia as the Engine of Application State):
/// The links dictionary provides discoverability and navigation:
/// - self: Link to this specific user resource
/// - photo: Link to the user's profile photo endpoint
/// - contacts: Link to manage the user's contact information
/// - groups: Link to view the user's group memberships
///
/// These links allow API clients and AI systems to:
/// - Navigate the API without hardcoding URLs
/// - Discover available operations on this resource
/// - Build adaptive user interfaces
/// - Implement robust API clients that don't break with URL changes
///
/// Privacy and Security:
/// - Some fields may be hidden based on user privacy settings
/// - Visibility of sensitive information depends on the caller's permissions
/// - Terminated users show limited information for security
/// - Contact information visibility is permission-based
///
/// Usage Scenarios:
/// - Displaying user profiles in web and mobile applications
/// - User search results with complete profile data
/// - Administrative user management interfaces
/// - API integrations that need full user information
/// - AI-powered user discovery and recommendation systems
/// </remarks>
/// <example>
/// {
///   "id": "550e8400-e29b-41d4-a716-446655440000",
///   "email": "john.doe@company.com",
///   "firstName": "John",
///   "lastName": "Doe",
///   "displayName": "John Doe",
///   "type": "User",
///   "status": "Active",
///   "activationStatus": "Activated",
///   "title": "Senior Developer",
///   "location": "New York Office",
///   "departments": [
///     {
///       "id": "660e8400-e29b-41d4-a716-446655440001",
///       "name": "Engineering"
///     }
///   ],
///   "createdAt": "2024-01-01T10:00:00Z",
///   "lastModified": "2024-06-15T14:30:00Z",
///   "links": {
///     "self": "/api/3.0/users/550e8400-e29b-41d4-a716-446655440000",
///     "photo": "/api/3.0/users/550e8400-e29b-41d4-a716-446655440000/photo",
///     "contacts": "/api/3.0/users/550e8400-e29b-41d4-a716-446655440000/contacts",
///     "groups": "/api/3.0/users/550e8400-e29b-41d4-a716-446655440000/groups"
///   }
/// }
/// </example>
public class UserResponseDtoV3
{
    /// <summary>
    /// The unique identifier (GUID) for the user.
    /// This ID is immutable and remains constant throughout the user's lifecycle.
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The user's email address used for authentication and communication.
    /// Guaranteed to be unique within the tenant.
    /// </summary>
    /// <example>john.doe@company.com</example>
    public string Email { get; set; }

    /// <summary>
    /// The user's first name (given name).
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; set; }

    /// <summary>
    /// The user's last name (family name).
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; set; }

    /// <summary>
    /// The user's formatted display name combining first and last names.
    /// Format may vary based on user's culture settings (e.g., "John Doe" vs "Doe, John").
    /// </summary>
    /// <example>John Doe</example>
    public string DisplayName { get; set; }

    /// <summary>
    /// The user type indicating access level and feature availability.
    /// Possible values: User, Guest, DocSpaceAdmin, RoomAdmin
    /// </summary>
    /// <example>User</example>
    public string Type { get; set; }

    /// <summary>
    /// The current employment status of the user.
    /// Possible values: Active (currently employed), Terminated (no longer employed), Disabled (temporarily suspended)
    /// </summary>
    /// <example>Active</example>
    public string Status { get; set; }

    /// <summary>
    /// The activation status of the user account.
    /// Possible values:
    /// - Activated: User has completed registration and can access the system
    /// - Pending: User invited but hasn't completed registration
    /// - NotActivated: User created but not yet activated
    /// </summary>
    /// <example>Activated</example>
    public string ActivationStatus { get; set; }

    /// <summary>
    /// The user's job title or position in the organization.
    /// </summary>
    /// <example>Senior Developer</example>
    public string Title { get; set; }

    /// <summary>
    /// List of departments (groups) the user belongs to.
    /// Each department includes ID and name for easy display.
    /// </summary>
    public IEnumerable<GroupSummaryDtoV3> Departments { get; set; }

    /// <summary>
    /// The user's physical location, office, or work site.
    /// </summary>
    /// <example>New York Office</example>
    public string Location { get; set; }

    /// <summary>
    /// The user's biological sex for profile purposes.
    /// Possible values: Male, Female, null (not specified)
    /// </summary>
    /// <example>Male</example>
    public string Sex { get; set; }

    /// <summary>
    /// The user's date of birth in ISO 8601 format.
    /// May be null if not provided or hidden for privacy.
    /// </summary>
    /// <example>1990-05-15</example>
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// The user's employment start date with the organization.
    /// Used for calculating tenure and anniversary dates.
    /// </summary>
    /// <example>2024-01-01</example>
    public DateTime WorksFrom { get; set; }

    /// <summary>
    /// Administrative notes or comments about the user.
    /// Only visible to administrators, not to the user themselves.
    /// </summary>
    /// <example>Transferred from London office in Q1 2024</example>
    public string Comment { get; set; }

    /// <summary>
    /// List of contact methods for the user (phone numbers, social media, messaging apps).
    /// Visibility may be restricted based on privacy settings and caller permissions.
    /// </summary>
    public IEnumerable<ContactDtoV3> Contacts { get; set; }

    /// <summary>
    /// URL to the user's profile photo.
    /// Returns the photo endpoint where the image can be retrieved.
    /// </summary>
    /// <example>/api/3.0/users/550e8400-e29b-41d4-a716-446655440000/photo</example>
    public string PhotoUrl { get; set; }

    /// <summary>
    /// The user's preferred culture/language code in IETF format (e.g., en-US, de-DE).
    /// Determines the language of the UI and notifications for this user.
    /// </summary>
    /// <example>en-US</example>
    public string CultureName { get; set; }

    /// <summary>
    /// Whether the user has opted in to receive marketing communications and promotional emails.
    /// Complies with GDPR and privacy regulations.
    /// </summary>
    /// <example>false</example>
    public bool AllowMarketing { get; set; }

    /// <summary>
    /// The timestamp when the user account was created (UTC).
    /// Immutable value set at user creation time.
    /// </summary>
    /// <example>2024-01-01T10:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the user profile was last modified (UTC).
    /// Updated whenever any profile field is changed.
    /// </summary>
    /// <example>2024-06-15T14:30:00Z</example>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Hypermedia links to related resources following HATEOAS principles.
    /// Provides discoverability and navigation without hardcoding URLs.
    ///
    /// Common links:
    /// - self: This user's resource URL
    /// - photo: User photo management endpoint
    /// - contacts: User contacts management endpoint
    /// - groups: User's group memberships endpoint
    /// </summary>
    /// <example>
    /// {
    ///   "self": "/api/3.0/users/550e8400-e29b-41d4-a716-446655440000",
    ///   "photo": "/api/3.0/users/550e8400-e29b-41d4-a716-446655440000/photo"
    /// }
    /// </example>
    public Dictionary<string, string> Links { get; set; }
}

/// <summary>
/// Summary information about a group/department for inclusion in user profiles.
/// </summary>
/// <remarks>
/// This lightweight DTO avoids circular references and reduces payload size.
/// Full group details can be retrieved via the groups API endpoint.
/// </remarks>
public class GroupSummaryDtoV3
{
    /// <summary>
    /// The unique identifier for the group.
    /// </summary>
    /// <example>660e8400-e29b-41d4-a716-446655440001</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The display name of the group/department.
    /// </summary>
    /// <example>Engineering</example>
    public string Name { get; set; }
}

/// <summary>
/// Contact information entry for a user (phone, email, social media, etc.).
/// </summary>
/// <remarks>
/// Represents a single contact method for reaching the user.
/// Multiple contacts of the same type can exist (e.g., work phone and mobile phone).
/// </remarks>
public class ContactDtoV3
{
    /// <summary>
    /// The type of contact method.
    /// Common values: phone, mobile, email, telegram, skype, linkedin, twitter, etc.
    /// </summary>
    /// <example>phone</example>
    public string Type { get; set; }

    /// <summary>
    /// The contact value (phone number, username, email address, etc.).
    /// Format depends on the contact type.
    /// </summary>
    /// <example>+1-555-123-4567</example>
    public string Value { get; set; }
}
