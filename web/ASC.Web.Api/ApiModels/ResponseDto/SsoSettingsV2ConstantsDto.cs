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
/// The SSO settings constants: every value the settings accept, by name.
/// </summary>
/// <remarks>
/// The groups below restate the <c>const</c> holders of ASC.Web.Core. A constant is static and
/// System.Text.Json writes instance members only, so returning the holders themselves put six
/// empty objects on the wire; an instance property per constant is what carries the values.
/// </remarks>
public class SsoSettingsV2ConstantsDto
{
    /// <summary>
    /// The values the `nameIdFormat` of the identity provider settings accepts. The built-in configuration uses
    /// the SAML 2.0 transient format.
    /// </summary>
    public SsoNameIdFormatTypeDto SsoNameIdFormatType { get; set; } = new();

    /// <summary>
    /// The values the `ssoBinding` and `sloBinding` of the identity provider settings accept - how the portal
    /// sends its sign-in and sign-out requests. The built-in configuration uses HTTP POST for both.
    /// </summary>
    public SsoBindingTypeDto SsoBindingType { get; set; } = new();

    /// <summary>
    /// The values the `signingAlgorithm` of the service provider certificate and the `verifyAlgorithm` of the
    /// identity provider certificate accept. The built-in configuration uses RSA-SHA1 for both.
    /// </summary>
    public SsoSigningAlgorithmTypeDto SsoSigningAlgorithmType { get; set; } = new();

    /// <summary>
    /// The values the `encryptAlgorithm` and `decryptAlgorithm` of the certificate settings accept. The built-in
    /// configuration uses AES-128 everywhere.
    /// </summary>
    public SsoEncryptAlgorithmTypeDto SsoEncryptAlgorithmType { get; set; } = new();

    /// <summary>
    /// The values the `action` of a service provider certificate accepts, which is what the portal's own key
    /// pair may be used for.
    /// </summary>
    public SsoSpCertificateActionTypeDto SsoSpCertificateActionType { get; set; } = new();

    /// <summary>
    /// The values the `action` of an identity provider certificate accepts, which is what the provider's
    /// certificate may be used for - the mirror image of the service provider actions.
    /// </summary>
    public SsoIdpCertificateActionTypeDto SsoIdpCertificateActionType { get; set; } = new();
}

/// <summary>
/// The SAML name ID formats the SSO settings accept.
/// </summary>
public class SsoNameIdFormatTypeDto
{
    /// <summary>
    /// The SAML 1.1 unspecified name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified</example>
    public string Saml11Unspecified => SsoNameIdFormatType.Saml11Unspecified;

    /// <summary>
    /// The SAML 1.1 email address name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress</example>
    public string Saml11EmailAddress => SsoNameIdFormatType.Saml11EmailAddress;

    /// <summary>
    /// The SAML 2.0 entity name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:nameid-format:entity</example>
    public string Saml20Entity => SsoNameIdFormatType.Saml20Entity;

    /// <summary>
    /// The SAML 2.0 transient name ID format, whose identifier differs from one session to the next. It is what
    /// the built-in configuration uses.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:nameid-format:transient</example>
    public string Saml20Transient => SsoNameIdFormatType.Saml20Transient;

    /// <summary>
    /// The SAML 2.0 persistent name ID format, whose identifier stays the same for one person across sessions.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:nameid-format:persistent</example>
    public string Saml20Persistent => SsoNameIdFormatType.Saml20Persistent;

    /// <summary>
    /// The SAML 2.0 encrypted name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:nameid-format:encrypted</example>
    public string Saml20Encrypted => SsoNameIdFormatType.Saml20Encrypted;

    /// <summary>
    /// The SAML 2.0 unspecified name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:nameid-format:unspecified</example>
    public string Saml20Unspecified => SsoNameIdFormatType.Saml20Unspecified;

    /// <summary>
    /// The SAML 1.1 X.509 subject name name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:1.1:nameid-format:X509SubjectName</example>
    public string Saml11X509SubjectName => SsoNameIdFormatType.Saml11X509SubjectName;

    /// <summary>
    /// The SAML 1.1 Windows domain qualified name name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:1.1:nameid-format:WindowsDomainQualifiedName</example>
    public string Saml11WindowsDomainQualifiedName => SsoNameIdFormatType.Saml11WindowsDomainQualifiedName;

    /// <summary>
    /// The SAML 2.0 Kerberos name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:nameid-format:kerberos</example>
    public string Saml20Kerberos => SsoNameIdFormatType.Saml20Kerberos;
}

/// <summary>
/// The SAML bindings the SSO settings accept.
/// </summary>
public class SsoBindingTypeDto
{
    /// <summary>
    /// The SAML 2.0 HTTP POST binding, which carries the request in a self-submitting form. It is what the
    /// built-in configuration uses and the one to pick when requests are signed, since it has no length limit.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST</example>
    public string Saml20HttpPost => SsoBindingType.Saml20HttpPost;

    /// <summary>
    /// The SAML 2.0 HTTP redirect binding, which carries the request in the query string and is therefore bound
    /// by the length a URL may have.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect</example>
    public string Saml20HttpRedirect => SsoBindingType.Saml20HttpRedirect;
}

/// <summary>
/// The signing algorithms the SSO settings accept.
/// </summary>
public class SsoSigningAlgorithmTypeDto
{
    /// <summary>
    /// The RSA-SHA1 signing algorithm, which the built-in configuration uses. SHA-1 is the weakest of the three
    /// and some identity providers no longer accept it.
    /// </summary>
    /// <example>http://www.w3.org/2000/09/xmldsig#rsa-sha1</example>
    public string RsaSha1 => SsoSigningAlgorithmType.RSA_SHA1;

    /// <summary>
    /// The RSA-SHA256 signing algorithm.
    /// </summary>
    /// <example>http://www.w3.org/2001/04/xmldsig-more#rsa-sha256</example>
    public string RsaSha256 => SsoSigningAlgorithmType.RSA_SHA256;

    /// <summary>
    /// The RSA-SHA512 signing algorithm.
    /// </summary>
    /// <example>http://www.w3.org/2001/04/xmldsig-more#rsa-sha512</example>
    public string RsaSha512 => SsoSigningAlgorithmType.RSA_SHA512;
}

/// <summary>
/// The encryption algorithms the SSO settings accept.
/// </summary>
public class SsoEncryptAlgorithmTypeDto
{
    /// <summary>
    /// The AES-128-CBC encryption algorithm, which the built-in configuration uses.
    /// </summary>
    /// <example>http://www.w3.org/2001/04/xmlenc#aes128-cbc</example>
    public string Aes128 => SsoEncryptAlgorithmType.AES_128;

    /// <summary>
    /// The AES-256-CBC encryption algorithm, the strongest of the three.
    /// </summary>
    /// <example>http://www.w3.org/2001/04/xmlenc#aes256-cbc</example>
    public string Aes256 => SsoEncryptAlgorithmType.AES_256;

    /// <summary>
    /// The Triple DES CBC encryption algorithm, kept for identity providers that support nothing newer.
    /// </summary>
    /// <example>http://www.w3.org/2001/04/xmlenc#tripledes-cbc</example>
    public string TriDec => SsoEncryptAlgorithmType.TRI_DEC;
}

/// <summary>
/// What the portal's own key pair may be used for, as the `action` of a service provider certificate.
/// </summary>
public class SsoSpCertificateActionTypeDto
{
    /// <summary>
    /// The key pair signs the requests the portal sends and nothing else.
    /// </summary>
    /// <example>signing</example>
    public string Signing => SsoSpCertificateActionType.Signing;

    /// <summary>
    /// The key pair encrypts what the portal sends and decrypts what comes back, but signs nothing.
    /// </summary>
    /// <example>encrypt</example>
    public string Encrypt => SsoSpCertificateActionType.Encrypt;

    /// <summary>
    /// The key pair does both, which is what one pair configured on its own has to be set to.
    /// </summary>
    /// <example>signing and encrypt</example>
    public string SigningAndEncrypt => SsoSpCertificateActionType.SigningAndEncrypt;
}

/// <summary>
/// What the identity provider's certificate may be used for, as the `action` of an identity provider certificate.
/// </summary>
public class SsoIdpCertificateActionTypeDto
{
    /// <summary>
    /// The certificate verifies the signatures on what the provider sends, and nothing else - the counterpart of
    /// the service provider's signing action.
    /// </summary>
    /// <example>verification</example>
    public string Verification => SsoIdpCertificateActionType.Verification;

    /// <summary>
    /// The certificate is used to decrypt what the provider sends, but verifies no signature.
    /// </summary>
    /// <example>decrypt</example>
    public string Decrypt => SsoIdpCertificateActionType.Decrypt;

    /// <summary>
    /// The certificate does both, which is what a single provider certificate has to be set to.
    /// </summary>
    /// <example>verification and decrypt</example>
    public string VerificationAndDecrypt => SsoIdpCertificateActionType.VerificationAndDecrypt;
}
