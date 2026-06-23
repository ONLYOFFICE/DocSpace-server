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

namespace ASC.Core.Common.Notify;

internal class FirebaseApiKey(IConfiguration configuration)
{
    public string Type => "service_account";

    [JsonPropertyName("project_id")]
    public string ProjectId => configuration["firebase-mobile:projectId"] ?? "";

    [JsonPropertyName("private_key_id")]
    public string PrivateKeyId => configuration["firebase-mobile:privateKeyId"] ?? "";

    [JsonPropertyName("private_key")]
    public string PrivateKey => configuration["firebase-mobile:privateKey"] ?? "";

    [JsonPropertyName("client_email")]
    public string ClientEmail => configuration["firebase-mobile:clientEmail"] ?? "";

    [JsonPropertyName("client_id")]
    public string ClientId => configuration["firebase-mobile:clientId"] ?? "";

    [JsonPropertyName("auth_uri")]
    public string AuthUri => "https://accounts.google.com/o/oauth2/auth";

    [JsonPropertyName("token_uri")]
    public string TokenUri => "https://oauth2.googleapis.com/token";

    [JsonPropertyName("auth_provider_x509_cert_url")]
    public string AuthProviderX509CertUrl => "https://www.googleapis.com/oauth2/v1/certs";

    [JsonPropertyName("client_x509_cert_url")]
    public string ClientX509CertUrl => configuration["firebase-mobile:x509CertUrl"] ?? "";
}