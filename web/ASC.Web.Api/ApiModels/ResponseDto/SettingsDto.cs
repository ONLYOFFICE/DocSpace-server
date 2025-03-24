// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The settings information.
/// </summary>
public class SettingsDto
{
    /// <summary>
    /// The time zone of the settings.
    /// </summary>
    [SwaggerSchemaCustom(Example = "UTC")]
    public string Timezone { get; set; }

    /// <summary>
    /// The settings list of trusted domains.
    /// </summary>
    [SwaggerSchemaCustom(Example = "mydomain.com")]
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// The trusted domains type of the settings.
    /// </summary>
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// The language of the settings.
    /// </summary>
    [SwaggerSchemaCustom(Example = "en-US")]
    public string Culture { get; set; }

    /// <summary>
    /// The settings UTC offset.
    /// </summary>
    [SwaggerSchemaCustom(Example = "-8.5")]
    public TimeSpan UtcOffset { get; set; }

    /// <summary>
    /// The settings UTC hours offset.
    /// </summary>
    public double UtcHoursOffset { get; set; }

    /// <summary>
    /// The greeting settings.
    /// </summary>
    [SwaggerSchemaCustom(Example = "Web Office Applications")]
    public string GreetingSettings { get; set; }

    /// <summary>
    /// The owner ID of the settings.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The team template ID of the settings.
    /// </summary>
    public string NameSchemaId { get; set; }

    /// <summary>
    /// Specifies if a user can join to the portal or not.
    /// </summary>
    public bool? EnabledJoin { get; set; }

    /// <summary>
    /// Specifies if a user can send a message to the administrator or not.
    /// </summary>
    public bool? EnableAdmMess { get; set; }

    /// <summary>
    /// Specifies if a user can connect third-party providers or not.
    /// </summary>
    public bool? ThirdpartyEnable { get; set; }

    /// <summary>
    /// Specifies if this is a DocSpace portal or not.
    /// </summary>
    public bool DocSpace { get; set; }

    /// <summary>
    /// Specifies if this is a standalone portal or not.
    /// </summary>
    public bool Standalone { get; set; }

    /// <summary>
    /// Specifies if this is a AMI instance or not.
    /// </summary>
    /// <type>System.Boolean, System</type>
    public bool IsAmi { get; set; }

    /// <summary>
    /// The base domain of the settings.
    /// </summary>
    public string BaseDomain { get; set; }

    /// <summary>
    /// The wizard token of the settings.
    /// </summary>
    public string WizardToken { get; set; }

    /// <summary>
    /// The password hash of the settings.
    /// </summary>
    public PasswordHasher PasswordHash { get; set; }

    /// <summary>
    /// The firebase parameters of the settings.
    /// </summary>
    public FirebaseDto Firebase { get; set; }

    /// <summary>
    /// The version of the settings.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// The type of captcha of the settings.
    /// </summary>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The ReCAPTCHA public key of the settings.
    /// </summary>
    public string RecaptchaPublicKey { get; set; }

    /// <summary>
    /// Specifies if the debug information will be sent or not.
    /// </summary>
    public bool DebugInfo { get; set; }

    /// <summary>
    /// The socket URL of the settings.
    /// </summary>
    public string SocketUrl { get; set; }

    /// <summary>
    /// The tenant status of the settings.
    /// </summary>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// The tenant alias of the settings.
    /// </summary>
    public string TenantAlias { get; set; }

    /// <summary>
    /// Specifies whether to display the "About" section.
    /// </summary>
    public bool DisplayAbout { get; set; }

    /// <summary>
    /// The domain validator of the settings.
    /// </summary>
    public TenantDomainValidator DomainValidator { get; set; }

    /// <summary>
    /// The zendesk key of the settings.
    /// </summary>
    public string ZendeskKey { get; set; }

    /// <summary>
    /// The tag manager ID of the settings.
    /// </summary>
    public string TagManagerId { get; set; }

    /// <summary>
    /// Specifies whether the cookie settings are enabled.
    /// </summary>
    public bool CookieSettingsEnabled { get; set; }

    /// <summary>
    /// The limited access to Space Management.
    /// </summary>
    public bool LimitedAccessSpace { get; set; }

    /// <summary>
    /// The limited access to Developer Tools for users.
    /// </summary>
    public bool LimitedAccessDevToolsForUsers { get; set; }

    /// <summary>
    /// The user name validation regex of the settings.
    /// </summary>
    public string UserNameRegex { get; set; }

    /// <summary>
    /// The invitation limit of the settings.
    /// </summary>
    public int? InvitationLimit { get; set; }

    /// <summary>
    /// The plugins of the settings.
    /// </summary>
    public PluginsDto Plugins { get; set; }

    /// <summary>
    /// The deep link of the settings.
    /// </summary>
    public DeepLinkDto DeepLink { get; set; }

    /// <summary>
    /// The form gallery of the settings.
    /// </summary>
    public FormGalleryDto FormGallery { get; set; }

    /// <summary>
    /// The max image upload size of the settings.
    /// </summary>
    public long MaxImageUploadSize { get; set; }

    /// <summary>
    /// The white label logo text of the settings.
    /// </summary>
    public string LogoText { get; set; }

    /// <summary>
    /// The external resources of the settings.
    /// </summary>
    public CultureSpecificExternalResources ExternalResources { get; set; }
}