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
    [OpenApiDescription("Time zone", Example = "UTC")]
    public string Timezone { get; set; }

    /// <summary>
    /// List of trusted domains
    /// </summary>
    [OpenApiDescription("List of trusted domains", Example = "mydomain.com")]
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// Trusted domains type
    /// </summary>
    [OpenApiDescription("Trusted domains type")]
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// Language
    /// </summary>
    [OpenApiDescription("Language", Example = "en-US")]
    public string Culture { get; set; }

    /// <summary>
    /// UTC offset
    /// </summary>
    [OpenApiDescription("UTC offset", Example = "-8.5")]
    public TimeSpan UtcOffset { get; set; }

    /// <summary>
    /// UTC hours offset
    /// </summary>
    [OpenApiDescription("UTC hours offset")]
    public double UtcHoursOffset { get; set; }

    /// <summary>
    /// Greeting settings
    /// </summary>
    [OpenApiDescription("Greeting settings", Example = "Web Office Applications")]
    public string GreetingSettings { get; set; }

    /// <summary>
    /// Owner ID
    /// </summary>
    [OpenApiDescription("Owner ID")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Team template ID
    /// </summary>
    [OpenApiDescription("Team template ID")]
    public string NameSchemaId { get; set; }

    /// <summary>
    /// Specifies if a user can join to the portal or not
    /// </summary>
    [OpenApiDescription("Specifies if a user can join to the portal or not")]
    public bool? EnabledJoin { get; set; }

    /// <summary>
    /// Specifies if a user can send a message to the administrator or not
    /// </summary>
    [OpenApiDescription("Specifies if a user can send a message to the administrator or not")]
    public bool? EnableAdmMess { get; set; }

    /// <summary>
    /// Specifies if a user can connect third-party providers or not
    /// </summary>
    [OpenApiDescription("Specifies if a user can connect third-party providers or not")]
    public bool? ThirdpartyEnable { get; set; }

    /// <summary>
    /// Specifies if this is a DocSpace portal or not
    /// </summary>
    [OpenApiDescription("Specifies if this is a DocSpace portal or not")]
    public bool DocSpace { get; set; }

    /// <summary>
    /// Specifies if this is a standalone portal or not
    /// </summary>
    [OpenApiDescription("Specifies if this is a standalone portal or not")]
    public bool Standalone { get; set; }

    /// <summary>Specifies if this is a AMI instance or not</summary>
    /// <type>System.Boolean, System</type>
    [OpenApiDescription("Specifies if this is a AMI instance or not")]
    public bool IsAmi { get; set; }

    /// <summary>
    /// Base domain
    /// </summary>
    [OpenApiDescription("Base domain")]
    public string BaseDomain { get; set; }

    /// <summary>
    /// Wizard token
    /// </summary>
    [OpenApiDescription("Wizard token")]
    public string WizardToken { get; set; }

    /// <summary>
    /// Password hash
    /// </summary>
    [OpenApiDescription("Password hash")]
    public PasswordHasher PasswordHash { get; set; }

    /// <summary>
    /// Firebase parameters
    /// </summary>
    [OpenApiDescription("Firebase parameters")]
    public FirebaseDto Firebase { get; set; }

    /// <summary>
    /// Version
    /// </summary>
    [OpenApiDescription("Version")]
    public string Version { get; set; }

    /// <summary>
    /// Type of captcha
    /// </summary>
    [OpenApiDescription("Type of captcha")]
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// ReCAPTCHA public key
    /// </summary>
    [OpenApiDescription("ReCAPTCHA public key")]
    public string RecaptchaPublicKey { get; set; }

    /// <summary>
    /// Specifies if the debug information will be sent or not
    /// </summary>
    [OpenApiDescription("Specifies if the debug information will be sent or not")]
    public bool DebugInfo { get; set; }

    /// <summary>
    /// Socket URL
    /// </summary>
    [OpenApiDescription("Socket URL")]
    public string SocketUrl { get; set; }

    /// <summary>
    /// Tenant status
    /// </summary>
    [OpenApiDescription("Tenant status")]
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// Tenant alias
    /// </summary>
    [OpenApiDescription("Tenant alias")]
    public string TenantAlias { get; set; }

    /// <summary>
    /// Link to the help
    /// </summary>
    [OpenApiDescription("Link to the help")]
    public string HelpLink { get; set; }
    
    /// <summary>
    /// Link to the feedback and support
    /// </summary>
    [OpenApiDescription("Link to the feedback and support")]
    public string FeedbackAndSupportLink { get; set; }

    /// <summary>
    /// Link to the forum
    /// </summary>
    [OpenApiDescription("Link to the forum")]
    public string ForumLink { get; set; }

    /// <summary>
    /// Specifies whether to display the About section
    /// </summary>
    [OpenApiDescription("Specifies whether to display the About section")]
    public bool DisplayAbout { get; set; }

    /// <summary>
    /// API documentation link
    /// </summary>
    [OpenApiDescription("API documentation link")]
    public string ApiDocsLink { get; set; }

    /// <summary>
    /// Domain validator
    /// </summary>
    [OpenApiDescription("Domain validator")]
    public TenantDomainValidator DomainValidator { get; set; }

    /// <summary>
    /// Zendesk key
    /// </summary>
    [OpenApiDescription("Zendesk key")]
    public string ZendeskKey { get; set; }

    /// <summary>
    /// Tag manager ID
    /// </summary>
    [OpenApiDescription("Tag manager ID")]
    public string TagManagerId { get; set; }

    /// <summary>
    /// Email for training booking
    /// </summary>
    [OpenApiDescription("Email for training booking")]
    public string BookTrainingEmail { get; set; }

    /// <summary>
    /// Documentation email
    /// </summary>
    [OpenApiDescription("Documentation email")]
    public string DocumentationEmail { get; set; }

    /// <summary>
    /// Legal terms
    /// </summary>
    [OpenApiDescription("Legal terms")]
    public string LegalTerms { get; set; }

    /// <summary>
    /// License url
    /// </summary>
    [OpenApiDescription("License url")]
    public string LicenseUrl { get; set; }

    /// <summary>
    /// Specifies whether the cookie settings are enabled
    /// </summary>
    [OpenApiDescription("Specifies whether the cookie settings are enabled")]
    public bool CookieSettingsEnabled { get; set; }

    /// <summary>
    /// Limited access space
    /// </summary>
    [OpenApiDescription("Limited access space")]
    public bool LimitedAccessSpace { get; set; }

    /// <summary>
    /// User name validation regex
    /// </summary>
    [OpenApiDescription("User name validation regex")]
    public string UserNameRegex { get; set; }

    /// <summary>
    /// Invitation limit
    /// </summary>
    [OpenApiDescription("Invitation limit")]
    public int? InvitationLimit { get; set; }

    /// <summary>
    /// Plugins
    /// </summary>
    [OpenApiDescription("Plugins")]
    public PluginsDto Plugins { get; set; }

    /// <summary>
    /// Deep link
    /// </summary>
    [OpenApiDescription("Deep link")]
    public DeepLinkDto DeepLink { get; set; }

    /// <summary>
    /// Form gallery
    /// </summary>
    [OpenApiDescription("Form gallery")]
    public FormGalleryDto FormGallery { get; set; }

    /// <summary>
    /// Max image upload size
    /// </summary>
    [OpenApiDescription("Max image upload size")]
    public long MaxImageUploadSize { get; set; }

    /// <summary>
    /// White label logo text
    /// </summary>
    [OpenApiDescription("White label logo text")]
    public string LogoText { get; set; }
}