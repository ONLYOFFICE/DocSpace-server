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

using ASC.Files.Core;

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The settings information.
/// </summary>
public class SettingsDto
{
    /// <summary>
    /// The time zone.
    /// </summary>
    /// <example>UTC</example>
    public string Timezone { get; set; }

    /// <summary>
    /// The list of the trusted domains.
    /// </summary>
    /// <example>["mydomain.com", "mydomain1.com"]</example>
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// The type of the trusted domains.
    /// </summary>
    /// <example>Custom</example>
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// The language.
    /// </summary>
    /// <example>en-US</example>
    public required string Culture { get; set; }

    /// <summary>
    /// The UTC offset in the TimeSpan format.
    /// </summary>
    /// <example>-08:30:00</example>
    public TimeSpan UtcOffset { get; set; }

    /// <summary>
    /// The UTC offset in hours.
    /// </summary>
    /// <example>-8.5</example>
    public double UtcHoursOffset { get; set; }

    /// <summary>
    /// The greeting settings.
    /// </summary>
    /// <example>Web Office Applications</example>
    public string GreetingSettings { get; set; }

    /// <summary>
    /// The owner ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The team template ID.
    /// </summary>
    /// <example>default</example>
    public string NameSchemaId { get; set; }

    /// <summary>
    /// Specifies if a user can join the portal or not.
    /// </summary>
    /// <example>true</example>
    public bool? EnabledJoin { get; set; }

    /// <summary>
    /// Specifies if a user can send a message to the administrator when accessing the DocSpace portal or not.
    /// </summary>
    /// <example>true</example>
    public bool? EnableAdmMess { get; set; }

    /// <summary>
    /// Specifies if a user can connect third-party providers to the portal or not.
    /// </summary>
    /// <example>true</example>
    public bool? ThirdpartyEnable { get; set; }

    /// <summary>
    /// Specifies if this portal is a DocSpace portal or not.
    /// </summary>
    /// <example>true</example>
    public bool DocSpace { get; set; }

    /// <summary>
    /// Indicates whether the system is running in standalone mode.
    /// </summary>
    /// <example>true</example>
    public bool Standalone { get; set; }

    /// <summary>
    /// Specifies if this portal is the AMI instance or not.
    /// </summary>
    /// <example>true</example>
    public bool IsAmi { get; set; }

    /// <summary>
    /// The base domain.
    /// </summary>
    /// <example>example.com</example>
    public required string BaseDomain { get; set; }

    /// <summary>
    /// The wizard token.
    /// </summary>
    /// <example>dGhpc2lzYXRva2Vu...</example>
    public string WizardToken { get; set; }

    /// <summary>
    /// The password hash.
    /// </summary>
    /// <example>{ "size": 256, "iterations": 100000, "salt": "base64string" }</example>
    public PasswordHasher PasswordHash { get; set; }

    /// <summary>
    /// The Firebase parameters.
    /// </summary>
    /// <example>{ "apiKey": "AIza...", "projectId": "myapp-12345" }</example>
    public FirebaseDto Firebase { get; set; }

    /// <summary>
    /// The portal version.
    /// </summary>
    /// <example>12.5.0</example>
    public string Version { get; set; }

    /// <summary>
    /// The type of CAPTCHA validation used.
    /// </summary>
    /// <example>Google</example>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The ReCAPTCHA public key.
    /// </summary>
    /// <example>abc123def456</example>
    public string RecaptchaPublicKey { get; set; }

    /// <summary>
    /// Specifies if the debug information will be sent or not.
    /// </summary>
    /// <example>true</example>
    public bool DebugInfo { get; set; }

    /// <summary>
    /// The socket URL.
    /// </summary>
    /// <example>https://example.com</example>
    public string SocketUrl { get; set; }

    /// <summary>
    /// The tenant status.
    /// </summary>
    /// <example>Active</example>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// The tenant alias.
    /// </summary>
    /// <example>mycompany</example>
    public string TenantAlias { get; set; }

    /// <summary>
    /// Specifies whether to display the "About" portal section.
    /// </summary>
    /// <example>true</example>
    public bool DisplayAbout { get; set; }

    /// <summary>
    /// The domain validator.
    /// </summary>
    /// <example>{ "minLength": 3, "maxLength": 63 }</example>
    public TenantDomainValidator DomainValidator { get; set; }

    /// <summary>
    /// The Zendesk key.
    /// </summary>
    /// <example>abc123def456</example>
    public string ZendeskKey { get; set; }

    /// <summary>
    /// The tag manager ID.
    /// </summary>
    /// <example>GTM-XXXXXX</example>
    public string TagManagerId { get; set; }

    /// <summary>
    /// Specifies whether the cookie settings are enabled.
    /// </summary>
    /// <example>true</example>
    public required bool CookieSettingsEnabled { get; set; }

    /// <summary>
    /// Specifies whether the access to the space management is limited or not.
    /// </summary>
    /// <example>true</example>
    public bool LimitedAccessSpace { get; set; }

    /// <summary>
    /// Specifies whether the access to the Developer Tools is limited for users or not.
    /// </summary>
    /// <example>true</example>
    public bool LimitedAccessDevToolsForUsers { get; set; }

    /// <summary>
    /// Specifies whether to display the promotional banners.
    /// </summary>
    /// <example>true</example>
    public bool DisplayBanners { get; set; }

    /// <summary>
    /// Specifies whether AI functionality (chat, agents, vectorization) is enabled for the current tenant.
    /// When <c>false</c>, all AI features are disabled and the AI Agents folder is hidden.
    /// </summary>
    /// <example>true</example>
    public bool AiEnabled { get; set; }

    /// <summary>
    /// The user name validation regex.
    /// </summary>
    /// <example>^[a-zA-Z0-9_]{3,20}$</example>
    public string UserNameRegex { get; set; }

    /// <summary>
    /// The maximum number of invitations to the portal.
    /// </summary>
    /// <example>10</example>
    public int? InvitationLimit { get; set; }

    /// <summary>
    /// The plugins settings.
    /// </summary>
    /// <example>{ "enabled": true, "allow": ["plugin1", "plugin2"] }</example>
    public PluginsDto Plugins { get; set; }

    /// <summary>
    /// The deep link settings.
    /// </summary>
    /// <example>{ "androidPackageName": "com.example.app", "url": "https://example.com/deeplink" }</example>
    public required DeepLinkDto DeepLink { get; set; }

    /// <summary>
    /// The form gallery settings.
    /// </summary>
    /// <example>{ "path": "/forms/templates", "domain": "https://forms.example.com" }</example>
    public FormGalleryDto FormGallery { get; set; }

    /// <summary>
    /// The maximum image upload size.
    /// </summary>
    /// <example>10485760</example>
    public long MaxImageUploadSize { get; set; }

    /// <summary>
    /// The white label logo text.
    /// </summary>
    /// <example>Company Name</example>
    public string LogoText { get; set; }

    /// <summary>
    /// The external resources settings.
    /// </summary>
    /// <example>{ "helpLink": "https://help.example.com", "feedbackLink": "https://feedback.example.com" }</example>
    public CultureSpecificExternalResources ExternalResources { get; set; }

    /// <summary>
    /// Specifies the default folder type for the current settings.
    /// </summary>
    /// <example>DEFAULT</example>
    public FolderType DefaultFolderType { get; set; }

    /// <summary>
    /// Specifies if an external database is connected for storing form results.
    /// </summary>
    /// <example>true</example>
    public bool ExternalDbEnabled { get; set; }
}