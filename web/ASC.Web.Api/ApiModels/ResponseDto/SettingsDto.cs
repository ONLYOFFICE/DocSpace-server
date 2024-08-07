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
    [SwaggerSchemaCustomString("Time zone")]
    public string Timezone { get; set; }

    [SwaggerSchemaCustomString("List of trusted domains", Example = "mydomain.com")]
    public List<string> TrustedDomains { get; set; }

    [SwaggerSchemaCustomString("Trusted domains type", Example = "None")]
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    [SwaggerSchemaCustomString("Language", Example = "en-US")]
    public string Culture { get; set; }

    [SwaggerSchemaCustom<TimeSpan>("UTC offset")]
    public TimeSpan UtcOffset { get; set; }

    [SwaggerSchemaCustomDouble("UTC hours offset")]
    public double UtcHoursOffset { get; set; }

    [SwaggerSchemaCustomString("Greeting settings", Example = "Web Office Applications")]
    public string GreetingSettings { get; set; }

    [SwaggerSchemaCustomGuid("Owner ID")]
    public Guid OwnerId { get; set; }

    [SwaggerSchemaCustomString("Team template ID")]
    public string NameSchemaId { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if a user can join to the portal or not", Nullable = true)]
    public bool? EnabledJoin { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if a user can send a message to the administrator or not", Nullable = true)]
    public bool? EnableAdmMess { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if a user can connect third-party providers or not", Nullable = true)]
    public bool? ThirdpartyEnable { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if this is a DocSpace portal or not")]
    public bool DocSpace { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if this is a standalone portal or not")]
    public bool Standalone { get; set; }

    [SwaggerSchemaCustomString("Base domain")]
    public string BaseDomain { get; set; }

    [SwaggerSchemaCustomString("Wizard token")]
    public string WizardToken { get; set; }

    [SwaggerSchemaCustom<PasswordHasher>("Password hash")]
    public PasswordHasher PasswordHash { get; set; }

    [SwaggerSchemaCustom<FirebaseDto>("Firebase parameters")]
    public FirebaseDto Firebase { get; set; }

    [SwaggerSchemaCustomString("Version")]
    public string Version { get; set; }

    [SwaggerSchemaCustomString("Type of captcha", Example = "Default")]
    public RecaptchaType RecaptchaType { get; set; }

    [SwaggerSchemaCustomString("ReCAPTCHA public key")]
    public string RecaptchaPublicKey { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if the debug information will be sent or not")]
    public bool DebugInfo { get; set; }

    [SwaggerSchemaCustomString("Socket URL")]
    public string SocketUrl { get; set; }

    [SwaggerSchemaCustomString("Tenant status", Example = "Active")]
    public TenantStatus TenantStatus { get; set; }

    [SwaggerSchemaCustomString("Tenant alias")]
    public string TenantAlias { get; set; }

    [SwaggerSchemaCustomString("Link to the help")]
    public string HelpLink { get; set; }

    [SwaggerSchemaCustomString("Link to the forum")]
    public string ForumLink { get; set; }

    [SwaggerSchemaCustomString("API documentation link")]
    public string ApiDocsLink { get; set; }

    [SwaggerSchemaCustom<TenantDomainValidator>("Domain validator")]
    public TenantDomainValidator DomainValidator { get; set; }

    [SwaggerSchemaCustomString("Zendesk key")]
    public string ZendeskKey { get; set; }

    [SwaggerSchemaCustomString("Tag manager ID")]
    public string TagManagerId { get; set; }

    [SwaggerSchemaCustomString("Email for training booking")]
    public string BookTrainingEmail { get; set; }

    [SwaggerSchemaCustomString("Documentation email")]
    public string DocumentationEmail { get; set; }

    [SwaggerSchemaCustomString("Legal terms")]
    public string LegalTerms { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies whether the cookie settings are enabled")]
    public bool CookieSettingsEnabled { get; set; }

    [SwaggerSchemaCustomBoolean("Limited access space")]
    public bool LimitedAccessSpace { get; set; }

    [SwaggerSchemaCustomString("User name validation regex")]
    public string UserNameRegex { get; set; }

    [SwaggerSchemaCustomInt("Invitation limit", Format = "int32", Nullable = true)]
    public int? InvitationLimit { get; set; }

    [SwaggerSchemaCustom<PluginsDto>("Plugins")]
    public PluginsDto Plugins { get; set; }

    [SwaggerSchemaCustom<DeepLinkDto>("Deep link")]
    public DeepLinkDto DeepLink { get; set; }

    [SwaggerSchemaCustom<FormGalleryDto>("Form gallery")]
    public FormGalleryDto FormGallery { get; set; }

    [SwaggerSchemaCustomLong("Max image upload size", Format = "int64")]
    public long MaxImageUploadSize { get; set; }

    public static SettingsDto GetSample()
    {
        return new SettingsDto
        {
            Culture = "en-US",
            Timezone = TimeZoneInfo.Utc.ToString(),
            TrustedDomains = ["mydomain.com"],
            UtcHoursOffset = -8.5,
            UtcOffset = TimeSpan.FromHours(-8.5),
            GreetingSettings = "Web Office Applications",
            OwnerId = new Guid()
        };
    }
}