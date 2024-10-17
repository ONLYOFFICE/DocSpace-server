﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Core.Common.Notify;

class FirebaseApiKey(IConfiguration configuration)
{
    public string Type
    {
        get
        {
            return "service_account";
        }
    }

    [JsonPropertyName("project_id")]
    public string ProjectId
    {
        get
        {
            return configuration["firebase-mobile:projectId"] ?? "";
        }
    }
    
    [JsonPropertyName("private_key_id")]
    public string PrivateKeyId
    {
        get
        {
            return configuration["firebase-mobile:privateKeyId"] ?? "";
        }
    }
    
    [JsonPropertyName("private_key")]
    public string PrivateKey
    {
        get
        {
            return configuration["firebase-mobile:privateKey"] ?? "";
        }
    }
    
    [JsonPropertyName("client_email")]
    public string ClientEmail
    {
        get
        {
            return configuration["firebase-mobile:clientEmail"] ?? "";
        }
    }
    
    [JsonPropertyName("client_id")]
    public string ClientId
    {
        get
        {
            return configuration["firebase-mobile:clientId"] ?? "";
        }
    }
    
    [JsonPropertyName("auth_uri")]
    public string AuthUri
    {
        get
        {
            return "https://accounts.google.com/o/oauth2/auth";
        }
    }
    
    [JsonPropertyName("token_uri")]
    public string TokenUri
    {
        get
        {
            return "https://oauth2.googleapis.com/token";
        }
    }
    
    [JsonPropertyName("auth_provider_x509_cert_url")]
    public string AuthProviderX509CertUrl
    {
        get
        {
            return "https://www.googleapis.com/oauth2/v1/certs";
        }
    }
    
    [JsonPropertyName("client_x509_cert_url")]
    public string ClientX509CertUrl
    {
        get
        {
            return configuration["firebase-mobile:x509CertUrl"] ?? "";
        }
    }
}
