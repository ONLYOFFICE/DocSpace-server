// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.Helpers;

[Scope]
public class DocumentServiceLicense(ICache cache,
    CoreBaseSettings coreBaseSettings,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
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
                   fileUtility,
                   filesLinkUtility.DocServiceCommandUrl,
                   CommandMethod.License,
                   null,
                   null,
                   null,
                   null,
                   fileUtility.SignatureSecret,
                   clientFactory
                   );

            if (useCache)
            {
                cache.Insert(cacheKey, commandResponse, DateTime.UtcNow.Add(_cacheExpiration));
            }
        }

        return commandResponse;
    }

    public async Task<bool> ValidateLicense(License license)
    {
        var attempt = 0;

        while (attempt < 3)
        {
            var commandResponse = await GetDocumentServiceLicenseAsync(false);

            if (commandResponse == null)
            {
                return true;
            }

            if (commandResponse.Error != ErrorTypes.NoError)
            {
                return false;
            }

            if (commandResponse.License.ResourceKey == license.ResourceKey || commandResponse.License.CustomerId == license.CustomerId)
            {
                return commandResponse.Server is { ResultType: CommandResponse.ServerInfo.ResultTypes.Success or CommandResponse.ServerInfo.ResultTypes.SuccessLimit };
            }
            else
            {
                await Task.Delay(1000);
                attempt += 1;
            }
        }

        return false;
    }

    public async Task<(Dictionary<string, DateTime>, License)> GetLicenseQuotaAsync()
    {
        var commandResponse = await GetDocumentServiceLicenseAsync(true);
        return commandResponse == null ? 
            (null, null) : 
            (commandResponse.Quota?.Users?.ToDictionary(r=> r.UserId, r=> r.Expire), commandResponse.License);
    }
}
