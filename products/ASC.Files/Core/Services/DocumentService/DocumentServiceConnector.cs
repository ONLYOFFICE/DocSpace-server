// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Web.Files.Services.DocumentService;

[Scope]
public class DocumentServiceConnector(ILogger<DocumentServiceConnector> logger,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    PathProvider pathProvider,
    GlobalStore globalStore,
    BaseCommonLinkUtility baseCommonLinkUtility,
    TenantManager tenantManager,
    TenantExtra tenantExtra,
    CoreSettings coreSettings,
    IHttpClientFactory clientFactory)
{
    public static string GenerateRevisionId(string expectedKey)
    {
        return ASC.Files.Core.Helpers.DocumentService.GenerateRevisionId(expectedKey);
    }

    public async Task<(int ResultPercent, string ConvertedDocumentUri, string convertedFileType)> GetConvertedUriAsync(string documentUri,
                                      string fromExtension,
                                      string toExtension,
                                      string documentRevisionId,
                                      string password,
                                      string region,
                                      ThumbnailData thumbnail,
                                      SpreadsheetLayout spreadsheetLayout,
                                      bool isAsync)
    {
        logger.DebugDocServiceConvert(fromExtension, toExtension, documentUri, filesLinkUtility.DocServiceConverterUrl);
        try
        {
            return await ASC.Files.Core.Helpers.DocumentService.GetConvertedUriAsync(
                fileUtility,
                filesLinkUtility.DocServiceConverterUrl,
                documentUri,
                fromExtension,
                toExtension,
                GenerateRevisionId(documentRevisionId),
                password,
                region,
                thumbnail,
                spreadsheetLayout,
                isAsync,
                fileUtility.SignatureSecret,
                clientFactory);
        }
        catch (Exception ex)
        {
            throw CustomizeError(ex);
        }
    }

    public async Task<bool> CommandAsync(CommandMethod method,
                               string docKeyForTrack,
                               object fileId = null,
                               string callbackUrl = null,
                               string[] users = null,
                               MetaData meta = null)
    {
        logger.DebugDocServiceCommand(method.ToStringFast(), fileId.ToString(), docKeyForTrack, callbackUrl, users != null ? string.Join(", ", users) : "null", JsonConvert.SerializeObject(meta));
        
        try
        {
            var commandResponse = await CommandRequestAsync(
                fileUtility,
                filesLinkUtility.DocServiceCommandUrl,
                method,
                GenerateRevisionId(docKeyForTrack),
                callbackUrl,
                users,
                meta,
                fileUtility.SignatureSecret,
                clientFactory);

            if (commandResponse.Error == ErrorTypes.NoError)
            {
                return true;
            }

            logger.ErrorDocServiceCommandResponse(commandResponse.Error, commandResponse.ErrorString);
        }
        catch (Exception e)
        {
            logger.ErrorDocServiceCommandError(e);
        }

        return false;
    }

    public async Task<(string BuilderKey, Dictionary<string, string> Urls)> DocbuilderRequestAsync(string requestKey,
                                           string inputScript,
                                           bool isAsync)
    {
        string scriptUrl = null;
        if (!string.IsNullOrEmpty(inputScript))
        {
            using (var stream = new MemoryStream())
            await using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(inputScript);
                await writer.FlushAsync();
                stream.Position = 0;
                scriptUrl = await pathProvider.GetTempUrlAsync(stream, ".docbuilder");
            }
            scriptUrl = await ReplaceCommunityAddressAsync(scriptUrl);
            requestKey = null;
        }

        logger.DebugDocServiceBuilderRequestKey(requestKey, isAsync);
        try
        {
            return await ASC.Files.Core.Helpers.DocumentService.DocbuilderRequestAsync(
                fileUtility,
                filesLinkUtility.DocServiceDocbuilderUrl,
                GenerateRevisionId(requestKey),
                scriptUrl,
                isAsync,
                fileUtility.SignatureSecret,
                clientFactory);
        }
        catch (Exception ex)
        {
            throw CustomizeError(ex);
        }
    }

    public async Task<string> GetVersionAsync()
    {
        logger.DebugDocServiceRequestVersion();
        try
        {
            var commandResponse = await CommandRequestAsync(
                fileUtility,
                filesLinkUtility.DocServiceCommandUrl,
                CommandMethod.Version,
                GenerateRevisionId(null),
                null,
                null,
                null,
                fileUtility.SignatureSecret,
                clientFactory);

            var version = commandResponse.Version;
            if (string.IsNullOrEmpty(version))
            {
                version = "0";
            }

            if (commandResponse.Error == ErrorTypes.NoError)
            {
                return version;
            }

            logger.ErrorDocServiceCommandResponse(commandResponse.Error, commandResponse.ErrorString);
        }
        catch (Exception e)
        {
            logger.ErrorDocServiceCommandError(e);
        }

        return "4.1.5.1";
    }

    public async Task CheckDocServiceUrlAsync()
    {
        if (!string.IsNullOrEmpty(filesLinkUtility.DocServiceHealthcheckUrl))
        {
            try
            {
                if (!await HealthcheckRequestAsync(filesLinkUtility.DocServiceHealthcheckUrl, clientFactory))
                {
                    throw new Exception("bad status");
                }
            }
            catch (Exception ex)
            {
                logger.ErrorDocServiceHealthcheck(ex);

                throw new Exception("Healthcheck url: " + ex.Message);
            }
        }

        if (!string.IsNullOrEmpty(filesLinkUtility.DocServiceConverterUrl))
        {
            string convertedFileUri;
            try
            {
                const string fileExtension = ".docx";
                var toExtension = fileUtility.GetInternalExtension(fileExtension);
                var url = pathProvider.GetEmptyFileUrl(fileExtension);

                var fileUri = await ReplaceCommunityAddressAsync(url);

                var key = GenerateRevisionId(Guid.NewGuid().ToString());
                var uriTuple = await ASC.Files.Core.Helpers.DocumentService.GetConvertedUriAsync(fileUtility, filesLinkUtility.DocServiceConverterUrl, fileUri, fileExtension, toExtension, key, null, null, null, null, false, fileUtility.SignatureSecret, clientFactory);
                convertedFileUri = uriTuple.ConvertedDocumentUri;
            }
            catch (Exception ex)
            {
                logger.ErrorConverterDocServiceCheckError(ex);

                throw new Exception("Converter url: " + ex.Message);
            }

            try
            {
                var request1 = new HttpRequestMessage
                {
                    RequestUri = new Uri(convertedFileUri)
                };

                using var httpClient = clientFactory.CreateClient();
                using var response = await httpClient.SendAsync(request1);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Converted url is not available");
                }
            }
            catch (Exception ex)
            {
                logger.ErrorDocumentDocServiceCheckError(ex);

                throw new Exception("Document server: " + ex.Message);
            }
        }

        if (!string.IsNullOrEmpty(filesLinkUtility.DocServiceCommandUrl))
        {
            try
            {
                var key = GenerateRevisionId(Guid.NewGuid().ToString());
                await CommandRequestAsync(fileUtility, filesLinkUtility.DocServiceCommandUrl, CommandMethod.Version, key, null, null, null, fileUtility.SignatureSecret, clientFactory);
            }
            catch (Exception ex)
            {
                logger.ErrorCommandDocServiceCheckError(ex);

                throw new Exception("Command url: " + ex.Message);
            }
        }

        if (!string.IsNullOrEmpty(filesLinkUtility.DocServiceDocbuilderUrl))
        {
            try
            {
                var storeTemplate = await globalStore.GetStoreTemplateAsync();
                var scriptUri = await storeTemplate.GetUriAsync("", "test.docbuilder");
                var scriptUrl = baseCommonLinkUtility.GetFullAbsolutePath(scriptUri.ToString());
                scriptUrl = await ReplaceCommunityAddressAsync(scriptUrl);

                await ASC.Files.Core.Helpers.DocumentService.DocbuilderRequestAsync(fileUtility, filesLinkUtility.DocServiceDocbuilderUrl, null, scriptUrl, false, fileUtility.SignatureSecret, clientFactory);
            }
            catch (Exception ex)
            {
                logger.ErrorDocServiceCheck(ex);

                throw new Exception("Docbuilder url: " + ex.Message);
            }
        }
    }

    public async Task<string> ReplaceCommunityAddressAsync(string url)
    {
        var docServicePortalUrl = filesLinkUtility.DocServicePortalUrl;

        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        if (string.IsNullOrEmpty(docServicePortalUrl))
        {
            var tenant = await tenantManager.GetCurrentTenantAsync();
            if (!tenantExtra.Saas
                || string.IsNullOrEmpty(tenant.MappedDomain)
                || !url.StartsWith("https://" + tenant.MappedDomain))
            {
                return url;
            }

            docServicePortalUrl = "https://" + tenant.GetTenantDomain(coreSettings, false);
        }

        var uri = new UriBuilder(url);
        if (new UriBuilder(baseCommonLinkUtility.ServerRootPath).Host != uri.Host)
        {
            return url;
        }

        var query = HttpUtility.ParseQueryString(uri.Query);
        //query[HttpRequestExtensions.UrlRewriterHeader] = urlRewriterQuery;
        uri.Query = query.ToString();

        var communityUrl = new UriBuilder(docServicePortalUrl);
        uri.Scheme = communityUrl.Scheme;
        uri.Host = communityUrl.Host;
        uri.Port = communityUrl.Port;

        return uri.ToString();
    }

    public string ReplaceDocumentAddress(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        var uri = new UriBuilder(url).ToString();
        var externalUri = new UriBuilder(baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceUrl)).ToString();
        var internalUri = new UriBuilder(baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceUrlInternal)).ToString();
        if (uri.StartsWith(internalUri, true, CultureInfo.InvariantCulture) || !uri.StartsWith(externalUri, true, CultureInfo.InvariantCulture))
        {
            return url;
        }

        uri = uri.Replace(externalUri, filesLinkUtility.DocServiceUrlInternal);

        return uri;
    }

    private Exception CustomizeError(Exception ex)
    {
        var error = FilesCommonResource.ErrorMessage_DocServiceException;
        if (!string.IsNullOrEmpty(ex.Message))
        {
            error += $" ({ex.Message})";
        }

        logger.ErrorDocServiceError(ex);

        return new Exception(error, ex);
    }
}
