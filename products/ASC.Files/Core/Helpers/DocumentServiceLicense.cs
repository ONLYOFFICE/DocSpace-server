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

namespace ASC.Files.Core.Helpers;

[Scope]
public class DocumentServiceLicense(ICache cache,
    ResiliencePipelineProvider<string> resiliencePipelineProvider,
    CoreBaseSettings coreBaseSettings,
    FilesLinkUtility filesLinkUtility,
    IHttpClientFactory clientFactory)
{
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);


    private async Task<CommandResponse> GetDocumentServiceLicenseAsync(bool useCache)
    {
        if (!coreBaseSettings.Standalone)
        {
            return null;
        }

        if (string.IsNullOrEmpty(filesLinkUtility.DocServiceCommandUrl))
        {
            return null;
        }

        var cacheKey = "DocumentServiceLicense";
        var commandResponse = useCache ? cache.Get<CommandResponse>(cacheKey) : null;
        if (commandResponse == null)
        {
            commandResponse = await CommandRequestAsync(
                   filesLinkUtility.DocServiceCommandUrl,
                   CommandMethod.License,
                   null,
                   null,
                   null,
                   null,
                   filesLinkUtility.DocServiceSignatureSecret,
                   filesLinkUtility.DocServiceSignatureHeader,
                   await filesLinkUtility.GetDocServiceSslVerificationAsync(),
                   clientFactory
                   );

            if (useCache)
            {
                cache.Insert(cacheKey, commandResponse, DateTime.UtcNow.Add(_cacheExpiration));
            }
        }

        return commandResponse;
    }

    public async Task<LicenseValidationResult> ValidateLicense(License license)
    {
        var pipeline = resiliencePipelineProvider.GetPipeline<LicenseValidationResult>(LicenseResiliencePipelineName);

        var errorMsg = string.Empty;

        var response = await pipeline.ExecuteAsync(async _ =>
        {
            var commandResponse = await GetDocumentServiceLicenseAsync(false);

            if (commandResponse == null)
            {
                return new LicenseValidationResult(true, null);
            }

            if (commandResponse.Error == ErrorTypes.ParseError)
            {
                errorMsg = commandResponse.ErrorString;
                return null; // Possible DocumentService behavior without a license when the response contains "end_date":null
            }

            if (commandResponse.Error != ErrorTypes.NoError)
            {
                return new LicenseValidationResult(false, commandResponse.ErrorString);
            }

            if (commandResponse.License.ResourceKey == license.ResourceKey ||
                commandResponse.License.CustomerId == license.CustomerId)
            {
                if (commandResponse.Server == null)
                {
                    return new LicenseValidationResult(false, "Server is null");
                }

                return commandResponse.Server.ResultType is CommandResponse.ServerInfo.ResultTypes.Success or CommandResponse.ServerInfo.ResultTypes.SuccessLimit
                    ? new LicenseValidationResult(true, null)
                    : new LicenseValidationResult(false, $"ResultType is {commandResponse.Server.ResultType}");
            }

            return null;
        });

        return response ?? new LicenseValidationResult(false, $"Failure after several attempts. {errorMsg}");
    }

    public async Task<(Dictionary<string, DateTime>, License)> GetLicenseQuotaAsync(bool useCache = true)
    {
        var commandResponse = await GetDocumentServiceLicenseAsync(useCache);
        return commandResponse == null ?
            (null, null) :
            (commandResponse.Quota?.Users?.ToDictionary(r => r.UserId, r => r.Expire), commandResponse.License);
    }
}