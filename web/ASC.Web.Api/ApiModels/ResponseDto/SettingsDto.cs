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

public class SettingsDto
{
    /// <summary>
    /// Time zone
    /// </summary>
    [SwaggerSchemaCustom(Example = "UTC")]
    public string Timezone { get; set; }

    /// <summary>
    /// List of trusted domains
    /// </summary>
    [SwaggerSchemaCustom(Example = "mydomain.com")]
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// Trusted domains type
    /// </summary>
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// Language
    /// </summary>
    [SwaggerSchemaCustom(Example = "en-US")]
    public string Culture { get; set; }

    /// <summary>
    /// UTC offset
    /// </summary>
    [SwaggerSchemaCustom(Example = "-8.5")]
    public TimeSpan UtcOffset { get; set; }

    /// <summary>
    /// UTC hours offset
    /// </summary>
    public double UtcHoursOffset { get; set; }

    /// <summary>
    /// Greeting settings
    /// </summary>
    [SwaggerSchemaCustom(Example = "Web Office Applications")]
    public string GreetingSettings { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Team template ID
    /// </summary>
    public string NameSchemaId { get; set; }

    /// <summary>
    /// Specifies if a user can join to the portal or not
    /// </summary>
    public bool? EnabledJoin { get; set; }

    /// <summary>
    /// Specifies if a user can send a message to the administrator or not
    /// </summary>
    public bool? EnableAdmMess { get; set; }

    /// <summary>
    /// Specifies if a user can connect third-party providers or not
    /// </summary>
    public bool? ThirdpartyEnable { get; set; }

    /// <summary>
    /// Specifies if this is a DocSpace portal or not
    /// </summary>
    public bool DocSpace { get; set; }

    /// <summary>
    /// Specifies if this is a standalone portal or not
    /// </summary>
    public bool Standalone { get; set; }

    /// <summary>
    /// Base domain
    /// </summary>
    public string BaseDomain { get; set; }

    /// <summary>
    /// Wizard token
    /// </summary>
    public string WizardToken { get; set; }

    /// <summary>
    /// Password hash
    /// </summary>
    public PasswordHasher PasswordHash { get; set; }

    /// <summary>
    /// Firebase parameters
    /// </summary>
    public FirebaseDto Firebase { get; set; }

    /// <summary>
    /// Version
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Type of captcha
    /// </summary>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// ReCAPTCHA public key
    /// </summary>
    public string RecaptchaPublicKey { get; set; }

    /// <summary>
    /// Specifies if the debug information will be sent or not
    /// </summary>
    public bool DebugInfo { get; set; }

    /// <summary>
    /// Socket URL
    /// </summary>
    public string SocketUrl { get; set; }

    /// <summary>
    /// Tenant status
    /// </summary>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// Tenant alias
    /// </summary>
    public string TenantAlias { get; set; }

    /// <summary>
    /// Link to the help
    /// </summary>
    public string HelpLink { get; set; }

    /// <summary>
    /// Link to the forum
    /// </summary>
    public string ForumLink { get; set; }

    /// <summary>
    /// Specifies whether to display the About section
    /// </summary>
    public bool DisplayAbout { get; set; }

    /// <summary>
    /// API documentation link
    /// </summary>
    public string ApiDocsLink { get; set; }

    /// <summary>
    /// Domain validator
    /// </summary>
    public TenantDomainValidator DomainValidator { get; set; }

    /// <summary>
    /// Zendesk key
    /// </summary>
    public string ZendeskKey { get; set; }

    /// <summary>
    /// Tag manager ID
    /// </summary>
    public string TagManagerId { get; set; }

    /// <summary>
    /// Email for training booking
    /// </summary>
    public string BookTrainingEmail { get; set; }

    /// <summary>
    /// Documentation email
    /// </summary>
    public string DocumentationEmail { get; set; }

    /// <summary>
    /// Legal terms
    /// </summary>
    public string LegalTerms { get; set; }

    /// <summary>
    /// License url
    /// </summary>
    public string LicenseUrl { get; set; }

    /// <summary>
    /// Specifies whether the cookie settings are enabled
    /// </summary>
    public bool CookieSettingsEnabled { get; set; }

    /// <summary>
    /// Limited access space
    /// </summary>
    public bool LimitedAccessSpace { get; set; }

    /// <summary>
    /// User name validation regex
    /// </summary>
    public string UserNameRegex { get; set; }

    /// <summary>
    /// Invitation limit
    /// </summary>
    public int? InvitationLimit { get; set; }

    /// <summary>
    /// Plugins
    /// </summary>
    public PluginsDto Plugins { get; set; }

    /// <summary>
    /// Deep link
    /// </summary>
    public DeepLinkDto DeepLink { get; set; }

    /// <summary>
    /// Form gallery
    /// </summary>
    public FormGalleryDto FormGallery { get; set; }

    /// <summary>
    /// Max image upload size
    /// </summary>
    public long MaxImageUploadSize { get; set; }
}