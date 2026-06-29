// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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

    /// <summary>
    /// Specifies if the tariff service is configured.
    /// </summary>
    /// <example>true</example>
    public bool TariffServiceConfigured { get; set; }
}
