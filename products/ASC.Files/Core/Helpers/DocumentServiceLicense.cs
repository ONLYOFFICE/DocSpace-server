// (c) Copyright Ascensio System SIA 2009-2025
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

using ASC.Core.Billing;

namespace ASC.Files.Core.Helpers;

[Scope]
public class DocumentServiceLicense(ICache cache,
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
        var builder = new ResiliencePipelineBuilder<LicenseValidationResult>();

        var pipeline = builder.AddRetry(new RetryStrategyOptions<LicenseValidationResult>()
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder<LicenseValidationResult>().HandleResult(result => result == null)
        }).Build();

        var response = await pipeline.ExecuteAsync(async (_) =>
        {
            var commandResponse = await GetDocumentServiceLicenseAsync(false);

            if (commandResponse == null)
            {
                return new LicenseValidationResult(true, null);
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

                return commandResponse.Server.ResultType == CommandResponse.ServerInfo.ResultTypes.Success ||
                    commandResponse.Server.ResultType == CommandResponse.ServerInfo.ResultTypes.SuccessLimit
                    ? new LicenseValidationResult(true, null)
                    : new LicenseValidationResult(false, $"ResultType is {commandResponse.Server.ResultType}");
            }

            return null;
        });

        return response ?? new LicenseValidationResult(false, "Failure after several attempts");
    }

    public async Task<(Dictionary<string, DateTime>, License)> GetLicenseQuotaAsync()
    {
        var commandResponse = await GetDocumentServiceLicenseAsync(true);
        return commandResponse == null ? 
            (null, null) : 
            (commandResponse.Quota?.Users?.ToDictionary(r=> r.UserId, r=> r.Expire), commandResponse.License);
    }
}
