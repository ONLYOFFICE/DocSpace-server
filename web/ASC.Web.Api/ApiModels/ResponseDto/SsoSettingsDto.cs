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
namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The SAML single sign-on configuration of the portal.
/// </summary>
public class SsoSettingsDto
{
    /// <summary>
    /// The timestamp indicating when the settings were last modified.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime LastModified { get; init; }

    /// <summary>
    /// Specifies if the SSO settings are enabled or not.
    /// </summary>
    /// <example>false</example>
    public bool? EnableSso { get; init; }

    /// <summary>
    /// The SSO IdP settings.
    /// </summary>
    /// <example>{"entityId": "", "ssoUrl": "", "ssoBinding": "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST", "sloUrl": "", "sloBinding": "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST", "nameIdFormat": "urn:oasis:names:tc:SAML:2.0:nameid-format:transient"}</example>
    public SsoIdpSettingsDto IdpSettings { get; init; }

    /// <summary>
    /// The list of the IdP certificates.
    /// </summary>
    /// <example>[{"crt": "base64-cert-data", "key": "base64-key-data"}]</example>
    public List<SsoCertificateDto> IdpCertificates { get; init; }

    /// <summary>
    /// The IdP advanced certificate.
    /// </summary>
    /// <example>{"verifyAlgorithm": "RSA_SHA1", "verifyAuthResponsesSign": false, "verifyLogoutRequestsSign": false, "verifyLogoutResponsesSign": false, "decryptAlgorithm": "AES_128", "decryptAssertions": false}</example>
    public SsoIdpCertificateAdvancedDto IdpCertificateAdvanced { get; init; }

    /// <summary>
    /// The SP login label.
    /// </summary>
    /// <example>Single Sign-on</example>
    public string SpLoginLabel { get; init; }

    /// <summary>
    /// The list of the SP certificates.
    /// </summary>
    /// <example>[{"crt": "base64-cert-data", "key": "base64-key-data"}]</example>
    public List<SsoCertificateDto> SpCertificates { get; init; }

    /// <summary>
    /// The SP advanced certificate.
    /// </summary>
    /// <example>{"signingAlgorithm": "RSA_SHA1", "signAuthRequests": false, "signLogoutRequests": false, "signLogoutResponses": false, "encryptAlgorithm": "AES_128", "encryptAssertions": false, "decryptAlgorithm": "AES_128"}</example>
    public SsoSpCertificateAdvancedDto SpCertificateAdvanced { get; init; }

    /// <summary>
    /// The SSO field mapping.
    /// </summary>
    /// <example>{"firstName": "givenName", "lastName": "sn", "email": "mail", "title": "title", "location": "l", "phone": "mobile"}</example>
    public SsoFieldMappingDto FieldMapping { get; init; }

    /// <summary>
    /// Specifies if the authentication page will be hidden or not.
    /// </summary>
    /// <example>false</example>
    public bool HideAuthPage { get; init; }

    /// <summary>
    /// The user type.
    /// </summary>
    /// <example>1</example>
    public int UsersType { get; init; }

    /// <summary>
    /// Specifies if the email verification is disabled or not.
    /// </summary>
    /// <example>false</example>
    public bool DisableEmailVerification { get; init; }
}

/// <summary>
/// The identity provider the portal trusts: its entity id, endpoints and bindings.
/// </summary>
public class SsoIdpSettingsDto
{
    /// <summary>
    /// The entity ID.
    /// </summary>
    /// <example>https://idp.company.com/saml</example>
    public string EntityId { get; init; }

    /// <summary>
    /// The SSO URL.
    /// </summary>
    /// <example>https://idp.example.com/sso</example>
    public string SsoUrl { get; init; }

    /// <summary>
    /// The SSO binding.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect</example>
    public string SsoBinding { get; init; }

    /// <summary>
    /// The SLO URL.
    /// </summary>
    /// <example>https://idp.example.com/slo</example>
    public string SloUrl { get; init; }

    /// <summary>
    /// The SLO binding.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect</example>
    public string SloBinding { get; init; }

    /// <summary>
    /// The name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress</example>
    public string NameIdFormat { get; init; }
}

/// <summary>
/// A certificate used to sign or encrypt SAML messages, with its validity period.
/// </summary>
public class SsoCertificateDto
{
    /// <summary>
    /// Specifies if a certificate is self-signed or not.
    /// </summary>
    /// <example>false</example>
    public bool SelfSigned { get; init; }

    /// <summary>
    /// The CRT certificate file.
    /// </summary>
    /// <example>crt file</example>
    public string Crt { get; init; }

    /// <summary>
    /// The certificate key.
    /// </summary>
    /// <example>key</example>
    public string Key { get; init; }

    /// <summary>
    /// The certificate action.
    /// </summary>
    /// <example>validate</example>
    public string Action { get; init; }

    /// <summary>
    /// The certificate domain name.
    /// </summary>
    /// <example>example.com</example>
    public string DomainName { get; init; }

    /// <summary>
    /// The certificate start date.
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// The certificate expiration date.
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime ExpiredDate { get; init; }
}

/// <summary>
/// How the portal checks and decrypts what the identity provider sends.
/// </summary>
public class SsoIdpCertificateAdvancedDto
{
    /// <summary>
    /// The certificate verification algorithm.
    /// </summary>
    /// <example>rsa-sha256</example>
    public string VerifyAlgorithm { get; init; }

    /// <summary>
    /// Specifies if the signatures of the SAML authentication responses sent to SP will be verified or not.
    /// </summary>
    /// <example>true</example>
    public bool VerifyAuthResponsesSign { get; init; }

    /// <summary>
    /// Specifies if the signatures of the SAML logout requests sent to SP will be verified or not.
    /// </summary>
    /// <example>true</example>
    public bool VerifyLogoutRequestsSign { get; init; }

    /// <summary>
    /// Specifies if the signatures of the SAML logout responses sent to SP will be verified or not.
    /// </summary>
    /// <example>true</example>
    public bool VerifyLogoutResponsesSign { get; init; }

    /// <summary>
    /// The certificate decryption algorithm.
    /// </summary>
    /// <example>aes256-cbc</example>
    public string DecryptAlgorithm { get; init; }

    /// <summary>
    /// Specifies if the assertions will be decrypted or not.
    /// </summary>
    /// <example>true</example>
    public bool DecryptAssertions { get; init; }
}

/// <summary>
/// How the portal signs and encrypts what it sends to the identity provider.
/// </summary>
public class SsoSpCertificateAdvancedDto
{
    /// <summary>
    /// The certificate signing algorithm.
    /// </summary>
    /// <example>rsa-sha256</example>
    public string SigningAlgorithm { get; init; }

    /// <summary>
    /// Specifies if SP will sign the SAML authentication requests sent to IdP or not.
    /// </summary>
    /// <example>true</example>
    public bool SignAuthRequests { get; init; }

    /// <summary>
    /// Specifies if SP will sign the SAML logout requests sent to IdP or not.
    /// </summary>
    /// <example>true</example>
    public bool SignLogoutRequests { get; init; }

    /// <summary>
    /// Specifies if SP will sign the SAML logout responses sent to IdP or not.
    /// </summary>
    /// <example>true</example>
    public bool SignLogoutResponses { get; init; }

    /// <summary>
    /// The certificate encryption algorithm.
    /// </summary>
    /// <example>aes256-cbc</example>
    public string EncryptAlgorithm { get; init; }

    /// <summary>
    /// The certificate decryption algorithm.
    /// </summary>
    /// <example>aes256-cbc</example>
    public string DecryptAlgorithm { get; init; }

    /// <summary>
    /// Specifies if the assertions will be encrypted or not.
    /// </summary>
    /// <example>true</example>
    public bool EncryptAssertions { get; init; }
}

/// <summary>
/// Which SAML attributes fill the profile fields of a user who signs in through SSO.
/// </summary>
public class SsoFieldMappingDto
{
    /// <summary>
    /// The first name.
    /// </summary>
    /// <example>givenName</example>
    public string FirstName { get; init; }

    /// <summary>
    /// The last name.
    /// </summary>
    /// <example>sn</example>
    public string LastName { get; init; }

    /// <summary>
    /// The email address.
    /// </summary>
    /// <example>sn@example.com</example>
    [EmailAddress]
    public string Email { get; init; }

    /// <summary>
    /// The title.
    /// </summary>
    /// <example>SN</example>
    public string Title { get; init; }

    /// <summary>
    /// The location.
    /// </summary>
    /// <example>Location</example>
    public string Location { get; init; }

    /// <summary>
    /// The phone number.
    /// </summary>
    /// <example>+14155552671</example>
    public string Phone { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class SsoSettingsDtoMapper
{
    public static partial SsoSettingsDto Map(this SsoSettingsV2 source);
}
