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
/// The general configuration of the current portal, as the client shell needs it before and after sign-in.
/// </summary>
public class SettingsDto
{
    /// <summary>
    /// The portal time zone as an IANA identifier, which is the zone every date this API returns in "portal time"
    /// is expressed in. Filled in for a signed-in caller only.
    /// </summary>
    /// <example>UTC</example>
    public string Timezone { get; set; }

    /// <summary>
    /// The mail domains a new member may register or be invited from without confirming the address. It is filled
    /// in for a signed-in caller, and for an anonymous one only while `enabledJoin` is `true`; it is empty
    /// whenever `trustedDomainsType` is not `Custom`.
    /// </summary>
    /// <example>["mydomain.com", "mydomain1.com"]</example>
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// How the mail domains above are applied: no domain trusted, every domain trusted, or only the listed ones.
    /// Filled in under the same conditions as `trustedDomains`.
    /// </summary>
    /// <example>Custom</example>
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// The default language of the portal as a culture name, which is what unauthenticated pages are rendered in.
    /// A signed-in member may have a language of their own, and that one is not reported here.
    /// </summary>
    /// <example>en-US</example>
    public required string Culture { get; set; }

    /// <summary>
    /// The portal's offset from UTC as a time span, positive east of UTC. Filled in for a signed-in caller only,
    /// and taken at the moment of the call, so it already reflects daylight saving time.
    /// </summary>
    /// <example>-08:30:00</example>
    public TimeSpan UtcOffset { get; set; }

    /// <summary>
    /// The same offset in hours, fractional for a zone that is not on a whole hour. It is there so a client does
    /// not have to parse `utcOffset`.
    /// </summary>
    /// <example>-8.5</example>
    public double UtcHoursOffset { get; set; }

    /// <summary>
    /// The portal title shown on the login page and in letters. It falls back to the product name in the portal
    /// language while the portal has been given no title of its own.
    /// </summary>
    /// <example>Web Office Applications</example>
    public string GreetingSettings { get; set; }

    /// <summary>
    /// The portal owner, the one account that cannot be removed or demoted. Filled in for a signed-in caller
    /// only, and the empty GUID for an anonymous one.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The naming scheme the portal uses for its own vocabulary - what a member, a group or a room is called in
    /// the interface. `GET api/2.0/settings/customschemas/{id}` spells that vocabulary out. Filled in for a
    /// signed-in caller only.
    /// </summary>
    /// <example>default</example>
    public string NameSchemaId { get; set; }

    /// <summary>
    /// Whether someone who is not invited may still register, which is the case when the portal trusts every mail
    /// domain or a list of them. It is computed for an anonymous caller only and left out entirely for a
    /// signed-in one, so a missing value is not a `false`.
    /// </summary>
    /// <example>true</example>
    public bool? EnabledJoin { get; set; }

    /// <summary>
    /// Whether the login page may offer the form for writing to the portal administrators. It is also `true`
    /// while the portal's payment has lapsed, whatever the setting says, so it can be set on a portal where an
    /// administrator switched the form off.
    /// </summary>
    /// <example>true</example>
    public bool? EnableAdmMess { get; set; }

    /// <summary>
    /// Whether the login page may offer sign-in through an external identity provider. It is computed for an
    /// anonymous caller only; `GET api/2.0/capabilities` reports the same thing with the list of providers.
    /// </summary>
    /// <example>true</example>
    public bool? ThirdpartyEnable { get; set; }

    /// <summary>
    /// Always `true` in this product. It exists so a client that also talks to older ONLYOFFICE portals can tell
    /// them apart, and is not a feature switch.
    /// </summary>
    /// <example>true</example>
    public bool DocSpace { get; set; }

    /// <summary>
    /// Whether this is a server installation someone administers themselves rather than a portal in the cloud.
    /// Several fields below and a number of operations behave differently in the two, so a client that has to
    /// branch on the deployment reads it here.
    /// </summary>
    /// <example>true</example>
    public bool Standalone { get; set; }

    /// <summary>
    /// Whether the installation runs from an Amazon machine image, which is a server installation that can read
    /// its own instance metadata. It is `false` on every cloud portal.
    /// </summary>
    /// <example>true</example>
    public bool IsAmi { get; set; }

    /// <summary>
    /// The domain new portals of this installation are created under, which is what a portal name is checked
    /// against and appended to. It is empty on an installation that serves a single portal on a fixed address.
    /// </summary>
    /// <example>example.com</example>
    public required string BaseDomain { get; set; }

    /// <summary>
    /// The token that authorizes the first-run setup wizard. It is handed out to anonymous callers only, and only
    /// while the wizard has not been completed; once it has, the field stays empty for good.
    /// </summary>
    /// <example>dGhpc2lzYXRva2Vu...</example>
    public string WizardToken { get; set; }

    /// <summary>
    /// The parameters for hashing a password in the client before it is sent - the salt, the iteration count and
    /// the hash size. It is filled in for an anonymous caller and, for a signed-in one, only when
    /// `withPassword=true` is asked for. Hash with exactly these parameters and send the result as
    /// `passwordHash`, since the portal cannot reproduce the hash from a different set.
    /// </summary>
    /// <example>{ "size": 256, "iterations": 100000, "salt": "base64string" }</example>
    public PasswordHasher PasswordHash { get; set; }

    /// <summary>
    /// The Firebase project a mobile or web client sends push registrations to. Filled in for a signed-in caller
    /// only, and its own fields are empty strings on an installation that configures no Firebase project.
    /// </summary>
    /// <example>{ "apiKey": "AIza...", "projectId": "myapp-12345" }</example>
    public FirebaseDto Firebase { get; set; }

    /// <summary>
    /// The product version of the portal, empty when the installation does not publish one. It is the version of
    /// the server, not of this API, whose own version is fixed at 2.0.
    /// </summary>
    /// <example>12.5.0</example>
    public string Version { get; set; }

    /// <summary>
    /// Which CAPTCHA the login form has to render, decided by the installation's configuration. Computed for an
    /// anonymous caller only.
    /// </summary>
    /// <example>Google</example>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The site key for the CAPTCHA named by `recaptchaType`, safe to embed in a page. It is empty when the
    /// installation configures no CAPTCHA, in which case the login form asks for none.
    /// </summary>
    /// <example>abc123def456</example>
    public string RecaptchaPublicKey { get; set; }

    /// <summary>
    /// Whether the client may collect and send diagnostic information. Filled in for a signed-in caller only, and
    /// `false` unless the installation switched it on.
    /// </summary>
    /// <example>true</example>
    public bool DebugInfo { get; set; }

    /// <summary>
    /// The address of the socket service that pushes live updates to a client. It is filled in for a signed-in
    /// caller and for an anonymous one who arrives with an external sharing link, and is empty when the
    /// installation runs no socket service - a client then has to poll.
    /// </summary>
    /// <example>https://example.com</example>
    public string SocketUrl { get; set; }

    /// <summary>
    /// The lifecycle state of the portal. Anything other than active means most operations are refused for the
    /// moment, because the portal is being transferred, restored, encrypted or removed.
    /// </summary>
    /// <example>Active</example>
    public TenantStatus TenantStatus { get; set; }

    /// <summary>
    /// The portal's own name within the installation, which together with `baseDomain` forms the address it is
    /// reached at. `PUT api/2.0/portal/portalrename` changes it.
    /// </summary>
    /// <example>mycompany</example>
    public string TenantAlias { get; set; }

    /// <summary>
    /// Whether the interface may show the About page. A cloud portal always may; a server installation may unless
    /// its plan includes branding and the vendor details hide the page.
    /// </summary>
    /// <example>true</example>
    public bool DisplayAbout { get; set; }

    /// <summary>
    /// The rules a portal name is checked against - its length limits and the pattern it has to match - so a
    /// client can validate a rename before sending it. Filled in for a signed-in caller only.
    /// </summary>
    /// <example>{ "minLength": 3, "maxLength": 63 }</example>
    public TenantDomainValidator DomainValidator { get; set; }

    /// <summary>
    /// The key that lets the client open the vendor's support chat, empty when the installation configures none.
    /// Filled in for a signed-in caller only.
    /// </summary>
    /// <example>abc123def456</example>
    public string ZendeskKey { get; set; }

    /// <summary>
    /// The Google Tag Manager container the client should load, empty when the installation configures none.
    /// Filled in for a signed-in caller only.
    /// </summary>
    /// <example>GTM-XXXXXX</example>
    public string TagManagerId { get; set; }

    /// <summary>
    /// Whether the portal limits how long an authentication session stays valid. The limit itself is read with
    /// `GET api/2.0/settings/cookiesettings`; while this is `false` a session is honoured for a year.
    /// </summary>
    /// <example>true</example>
    public required bool CookieSettingsEnabled { get; set; }

    /// <summary>
    /// Whether the space-management section is restricted to the portal owner. Filled in for a signed-in caller
    /// only.
    /// </summary>
    /// <example>true</example>
    public bool LimitedAccessSpace { get; set; }

    /// <summary>
    /// Whether the Developer Tools section is hidden from members who are not administrators. Filled in for a
    /// signed-in caller only.
    /// </summary>
    /// <example>true</example>
    public bool LimitedAccessDevToolsForUsers { get; set; }

    /// <summary>
    /// Whether the interface may show the vendor's promotional banners. A cloud portal always reports `true`; on
    /// a server installation it follows the banner setting. Filled in for a signed-in caller only.
    /// </summary>
    /// <example>true</example>
    public bool DisplayBanners { get; set; }

    /// <summary>
    /// Whether the AI features - chat, agents and vectorisation - may be used on this portal. While it is
    /// `false` the AI Agents folder is hidden and the AI operations are refused. Filled in for a signed-in caller
    /// only.
    /// </summary>
    /// <example>true</example>
    public bool AiEnabled { get; set; }

    /// <summary>
    /// Whether the portal wallet has already dropped below its low-balance threshold, so a client can warn about
    /// AI operations being cut off. It is reported to DocSpace administrators only and left empty for everyone
    /// else, which is not the same as a healthy balance.
    /// </summary>
    /// <example>false</example>
    public bool? WalletLowBalance { get; set; }

    /// <summary>
    /// The pattern a member's first and last name has to match, so a client can validate a name before sending
    /// it. It is a .NET regular expression and is applied to each name part separately.
    /// </summary>
    /// <example>^[a-zA-Z0-9_]{3,20}$</example>
    public string UserNameRegex { get; set; }

    /// <summary>
    /// How many invitations the portal may still send in the current window. Filled in for a signed-in caller
    /// only, and set to the maximum value of a 32-bit integer on an installation that limits nothing.
    /// </summary>
    /// <example>10</example>
    public int? InvitationLimit { get; set; }

    /// <summary>
    /// What the installation allows to be done with web plugins. Filled in for a signed-in caller only, with all
    /// three flags `false` unless the installation switched plugins on.
    /// </summary>
    /// <example>{ "enabled": true, "upload": true, "delete": true }</example>
    public PluginsDto Plugins { get; set; }

    /// <summary>
    /// What a mobile client needs to hand a document link over to the installed application instead of opening it
    /// in the browser. Its fields are empty strings when the installation configures no application.
    /// </summary>
    /// <example>{ "androidPackageName": "com.example.app", "url": "https://example.com/deeplink" }</example>
    public required DeepLinkDto DeepLink { get; set; }

    /// <summary>
    /// Where the ready-made form templates are served from and which extension they carry. Filled in for a
    /// signed-in caller only.
    /// </summary>
    /// <example>{ "path": "/forms/templates", "domain": "https://forms.example.com" }</example>
    public FormGalleryDto FormGallery { get; set; }

    /// <summary>
    /// The largest image the portal accepts as a logo or an avatar, in bytes. Filled in for a signed-in caller
    /// only, and a larger upload is refused rather than resized.
    /// </summary>
    /// <example>10485760</example>
    public long MaxImageUploadSize { get; set; }

    /// <summary>
    /// The wordmark to print next to the portal logo. It falls back to the built-in one while the portal has
    /// stored no text of its own, so it is never empty.
    /// </summary>
    /// <example>Company Name</example>
    public string LogoText { get; set; }

    /// <summary>
    /// The addresses of the vendor's help, support, forum and video resources, already picked for the portal
    /// language. An entry is missing when the installation configures no address for it or the resource is
    /// switched off, which `GET api/2.0/settings/rebranding/additional` reports flag by flag.
    /// </summary>
    /// <example>{ "helpLink": "https://help.example.com", "feedbackLink": "https://feedback.example.com" }</example>
    public CultureSpecificExternalResources ExternalResources { get; set; }

    /// <summary>
    /// The section the client should open after sign-in, which is the caller's own preference rather than a
    /// portal-wide one. Filled in for a signed-in caller only.
    /// </summary>
    /// <example>DEFAULT</example>
    public FolderType DefaultFolderType { get; set; }

    /// <summary>
    /// Whether the installation has an external database wired up for form results, without which the operations
    /// that write form results there are refused. Filled in for a signed-in caller only.
    /// </summary>
    /// <example>true</example>
    public bool ExternalDbEnabled { get; set; }
}