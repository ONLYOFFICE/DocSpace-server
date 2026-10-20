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

using DocSpace.API.SDK.Api.Authentication;

namespace ASC.Data.Stress.Core;

public static class ApiHelper
{
    /// <summary>The OpenResty router of the local Aspire stack (README: "OpenResty (reverse proxy) | 8092").</summary>
    public const string DefaultBasePath = "http://localhost:8092";

    public static async Task<Configuration> GetConfigurationAsync(string userName, string password, string basePath = DefaultBasePath)
    {
        using var authorizationApi = new AuthenticationApi(new Configuration { BasePath = basePath });
        var response = (await authorizationApi.AuthenticateMeAsync(new AuthRequestDto(userName, password))).Response;

        return GetConfiguration(response.Token, basePath);
    }

    /// <summary>Configuration for a ready token: an API key or a token from <c>POST api/2.0/authentication</c>.</summary>
    public static Configuration GetConfiguration(string token, string basePath = DefaultBasePath, TimeSpan? timeout = null)
    {
        var configuration = new Configuration
        {
            BasePath = basePath,
            DefaultHeaders =
            {
                ["Authorization"] = $"Bearer {token}"
            }
        };

        if (timeout is not null)
        {
            configuration.Timeout = timeout.Value;
        }

        return configuration;
    }
}
