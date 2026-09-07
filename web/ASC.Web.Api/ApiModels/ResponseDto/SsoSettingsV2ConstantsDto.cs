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
    /// The SAML name ID formats the SSO settings accept.
    /// </summary>
    public SsoNameIdFormatTypeDto SsoNameIdFormatType { get; set; } = new();

    /// <summary>
    /// The SAML bindings the SSO settings accept.
    /// </summary>
    public SsoBindingTypeDto SsoBindingType { get; set; } = new();

    /// <summary>
    /// The signing algorithms the SSO settings accept.
    /// </summary>
    public SsoSigningAlgorithmTypeDto SsoSigningAlgorithmType { get; set; } = new();

    /// <summary>
    /// The encryption algorithms the SSO settings accept.
    /// </summary>
    public SsoEncryptAlgorithmTypeDto SsoEncryptAlgorithmType { get; set; } = new();

    /// <summary>
    /// What an SP certificate can be used for.
    /// </summary>
    public SsoSpCertificateActionTypeDto SsoSpCertificateActionType { get; set; } = new();

    /// <summary>
    /// What an IDP certificate can be used for.
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
    /// The SAML 2.0 transient name ID format.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:nameid-format:transient</example>
    public string Saml20Transient => SsoNameIdFormatType.Saml20Transient;

    /// <summary>
    /// The SAML 2.0 persistent name ID format.
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
    /// The SAML 2.0 HTTP POST binding.
    /// </summary>
    /// <example>urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST</example>
    public string Saml20HttpPost => SsoBindingType.Saml20HttpPost;

    /// <summary>
    /// The SAML 2.0 HTTP redirect binding.
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
    /// The RSA-SHA1 signing algorithm.
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
    /// The AES-128-CBC encryption algorithm.
    /// </summary>
    /// <example>http://www.w3.org/2001/04/xmlenc#aes128-cbc</example>
    public string Aes128 => SsoEncryptAlgorithmType.AES_128;

    /// <summary>
    /// The AES-256-CBC encryption algorithm.
    /// </summary>
    /// <example>http://www.w3.org/2001/04/xmlenc#aes256-cbc</example>
    public string Aes256 => SsoEncryptAlgorithmType.AES_256;

    /// <summary>
    /// The Triple DES CBC encryption algorithm.
    /// </summary>
    /// <example>http://www.w3.org/2001/04/xmlenc#tripledes-cbc</example>
    public string TriDec => SsoEncryptAlgorithmType.TRI_DEC;
}

/// <summary>
/// What an SP certificate can be used for.
/// </summary>
public class SsoSpCertificateActionTypeDto
{
    /// <summary>
    /// Signing only.
    /// </summary>
    /// <example>signing</example>
    public string Signing => SsoSpCertificateActionType.Signing;

    /// <summary>
    /// Encryption only.
    /// </summary>
    /// <example>encrypt</example>
    public string Encrypt => SsoSpCertificateActionType.Encrypt;

    /// <summary>
    /// Both signing and encryption.
    /// </summary>
    /// <example>signing and encrypt</example>
    public string SigningAndEncrypt => SsoSpCertificateActionType.SigningAndEncrypt;
}

/// <summary>
/// What an IDP certificate can be used for.
/// </summary>
public class SsoIdpCertificateActionTypeDto
{
    /// <summary>
    /// Verification only.
    /// </summary>
    /// <example>verification</example>
    public string Verification => SsoIdpCertificateActionType.Verification;

    /// <summary>
    /// Decryption only.
    /// </summary>
    /// <example>decrypt</example>
    public string Decrypt => SsoIdpCertificateActionType.Decrypt;

    /// <summary>
    /// Both verification and decryption.
    /// </summary>
    /// <example>verification and decrypt</example>
    public string VerificationAndDecrypt => SsoIdpCertificateActionType.VerificationAndDecrypt;
}
