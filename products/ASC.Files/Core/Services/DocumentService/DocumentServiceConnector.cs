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

using Microsoft.Net.Http.Headers;

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
                                      Options options,
                                      bool isAsync,
                                      bool toForm)
    {
        logger.DebugDocServiceConvert(fromExtension, toExtension, documentUri, filesLinkUtility.DocServiceConverterUrl);
        try
        {
            return await ASC.Files.Core.Helpers.DocumentService.GetConvertedUriAsync(
                filesLinkUtility.DocServiceConverterUrl,
                documentUri,
                fromExtension,
                toExtension,
                GenerateRevisionId(documentRevisionId),
                password,
                region,
                thumbnail,
                spreadsheetLayout,
                options,
                isAsync,
                filesLinkUtility.DocServiceSignatureSecret,
                filesLinkUtility.DocServiceSignatureHeader,
                await filesLinkUtility.GetDocServiceSslVerificationAsync(),
                clientFactory,
                toForm);
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
        logger.DebugDocServiceCommand(method.ToStringFast(), fileId.ToString(), docKeyForTrack, callbackUrl, users != null ? string.Join(", ", users) : "null", JsonSerializer.Serialize(meta));
        
        try
        {
            var commandResponse = await CommandRequestAsync(
                filesLinkUtility.DocServiceCommandUrl,
                method,
                GenerateRevisionId(docKeyForTrack),
                callbackUrl,
                users,
                meta,
                filesLinkUtility.DocServiceSignatureSecret,
                filesLinkUtility.DocServiceSignatureHeader,
                await filesLinkUtility.GetDocServiceSslVerificationAsync(),
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
            if (System.IO.File.Exists(inputScript))
            {
                await using (var stream = System.IO.File.OpenRead(inputScript))
                {
                    scriptUrl = await pathProvider.GetTempUrlAsync(stream, ".docbuilder");
                }
            }
            else
            {
                using (var stream = new MemoryStream())
                await using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(inputScript);
                    await writer.FlushAsync();
                    stream.Position = 0;
                    scriptUrl = await pathProvider.GetTempUrlAsync(stream, ".docbuilder");
                }
            }
            scriptUrl = ReplaceCommunityAddress(scriptUrl);
            requestKey = scriptUrl;
        }

        logger.DebugDocServiceBuilderRequestKey(requestKey, isAsync);
        try
        {
            return await ASC.Files.Core.Helpers.DocumentService.DocbuilderRequestAsync(
                filesLinkUtility.DocServiceDocbuilderUrl,
                GenerateRevisionId(requestKey),
                scriptUrl,
                isAsync,
                filesLinkUtility.DocServiceSignatureSecret,
                filesLinkUtility.DocServiceSignatureHeader,
                await filesLinkUtility.GetDocServiceSslVerificationAsync(),
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
                filesLinkUtility.DocServiceCommandUrl,
                CommandMethod.Version,
                null,
                null,
                null,
                null,
                filesLinkUtility.DocServiceSignatureSecret,
                filesLinkUtility.DocServiceSignatureHeader,
                await filesLinkUtility.GetDocServiceSslVerificationAsync(),
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
        if (!string.IsNullOrEmpty(filesLinkUtility.DocServiceApiUrl))
        {
            try
            {
                var requestUri = Uri.IsWellFormedUriString(filesLinkUtility.DocServiceApiUrl, UriKind.Absolute)
                    ? filesLinkUtility.DocServiceApiUrl
                    : baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceApiUrl);

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(requestUri),
                    Method = HttpMethod.Head
                };

                using var httpClient = await filesLinkUtility.GetDocServiceSslVerificationAsync()
                    ? clientFactory.CreateClient()
                    : clientFactory.CreateClient(CustomSslVerificationClient);

                using var response = await httpClient.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Api url is not available");
                }
            }
            catch (Exception ex)
            {
                logger.ErrorDocumentDocServiceCheckError(ex);

                throw new Exception("Api url: " + ex.Message);
            }
        }

        var docServiceHealthcheckUrl = filesLinkUtility.DocServiceHealthcheckUrl;
        if (!string.IsNullOrEmpty(docServiceHealthcheckUrl))
        {
            try
            {
                if (!await HealthcheckRequestAsync(docServiceHealthcheckUrl, clientFactory))
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

                var fileUri = ReplaceCommunityAddress(url);

                var key = GenerateRevisionId(Guid.NewGuid().ToString());

                var uriTuple = await ASC.Files.Core.Helpers.DocumentService.GetConvertedUriAsync(
                    filesLinkUtility.DocServiceConverterUrl,
                    fileUri,
                    fileExtension,
                    toExtension,
                    key,
                    null,
                    null,
                    null,
                    null,
                    null,
                    false,
                    filesLinkUtility.DocServiceSignatureSecret,
                    filesLinkUtility.DocServiceSignatureHeader,
                    await filesLinkUtility.GetDocServiceSslVerificationAsync(),
                    clientFactory,
                    false);

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

                await CommandRequestAsync(
                    filesLinkUtility.DocServiceCommandUrl,
                    CommandMethod.Version,
                    key,
                    null,
                    null,
                    null,
                    filesLinkUtility.DocServiceSignatureSecret,
                    filesLinkUtility.DocServiceSignatureHeader,
                    await filesLinkUtility.GetDocServiceSslVerificationAsync(),
                    clientFactory);
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
                scriptUrl = ReplaceCommunityAddress(scriptUrl);

                await ASC.Files.Core.Helpers.DocumentService.DocbuilderRequestAsync(
                    filesLinkUtility.DocServiceDocbuilderUrl,
                    null,
                    scriptUrl,
                    false,
                    filesLinkUtility.DocServiceSignatureSecret,
                    filesLinkUtility.DocServiceSignatureHeader,
                    await filesLinkUtility.GetDocServiceSslVerificationAsync(),
                    clientFactory);
            }
            catch (Exception ex)
            {
                logger.ErrorDocServiceCheck(ex);

                throw new Exception("Docbuilder url: " + ex.Message);
            }
        }
    }

    public string ReplaceCommunityAddress(string url)
    {
        var docServicePortalUrl = filesLinkUtility.GetDocServicePortalUrl();

        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        if (string.IsNullOrEmpty(docServicePortalUrl))
        {
            var tenant = tenantManager.GetCurrentTenant();
            if (!tenantExtra.Saas
                || string.IsNullOrEmpty(tenant.MappedDomain)
                || !url.StartsWith("https://" + tenant.MappedDomain))
            {
                return url;
            }

            docServicePortalUrl = "https://" + tenant.GetTenantDomain(coreSettings, false);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return url;
        }

        var uri = new UriBuilder(absoluteUri);
        if (new UriBuilder(baseCommonLinkUtility.ServerRootPath).Host != uri.Host)
        {
            return url;
        }

        var query = HttpUtility.ParseQueryString(uri.Query);
        query[HeaderNames.Origin.ToLower()] = uri.Scheme + Uri.SchemeDelimiter + uri.Host + ":" + uri.Port;
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
        var externalUri = new UriBuilder(baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetDocServiceUrl())).ToString();
        var internalUri = new UriBuilder(baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetDocServiceUrlInternal())).ToString();
        if (uri.StartsWith(internalUri, true, CultureInfo.InvariantCulture) || !uri.StartsWith(externalUri, true, CultureInfo.InvariantCulture))
        {
            return url;
        }

        uri = uri.Replace(externalUri, filesLinkUtility.GetDocServiceUrlInternal());

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
