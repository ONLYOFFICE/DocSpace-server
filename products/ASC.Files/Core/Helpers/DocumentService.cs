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

/// <summary>
/// The document service parameters.
/// </summary>
public static class DocumentService
{
    private const int Timeout = 120000;

    /// <summary>
    /// The custom SSL verification client.
    /// </summary>
    public const string CustomSslVerificationClient = "CustomSSLVerificationClient";

    /// <summary>
    /// The document service resilience pipeline name.
    /// </summary>
    public const string ResiliencePipelineName = "DocumentServiceResiliencePipeline";

    /// <summary>
    /// The document service license resilience pipeline name.
    /// </summary>
    public const string LicenseResiliencePipelineName = "DocumentServiceLicenseResiliencePipeline";

    /// <summary>
    /// Gets the HTTP client name.
    /// </summary>
    public static string GetHttpClientName(bool sslVerification) => nameof(DocumentService) + (sslVerification ? string.Empty : CustomSslVerificationClient);

    private static readonly JsonSerializerOptions _bodySettings = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// The document service takes the parameters of an uploaded source from inside the token, and the claim below
    /// names the endpoint a token may be spent on, so that one cannot be replayed against another. The "from-file"
    /// endpoints want the name of the plain endpoint: the document service also knows "converter-from-file" and
    /// "docbuilder-from-file", but rejects both here, and a token with no operation at all comes back as error -8.
    /// </summary>
    private const string OperationClaim = "operation";
    private const string ConverterOperation = "converter";
    private const string DocbuilderOperation = "docbuilder";

    private static readonly JsonSerializerOptions _tokenSettings = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions _commonSettings = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Translation key to a supported form.
    /// </summary>
    /// <param name="expectedKey">Expected key</param>
    /// <returns>Supported key</returns>
    public static string GenerateRevisionId(string expectedKey)
    {
        expectedKey ??= "";
        const int maxLength = 128;
        if (expectedKey.Length > maxLength)
        {
            expectedKey = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey)));
        }

        var key = Regex.Replace(expectedKey, "[^0-9a-zA-Z_]", "_");
        return key[^Math.Min(key.Length, maxLength)..];
    }

    /// <summary>
    /// The method converts the file to the required format.
    /// </summary>
    /// <param name="documentConverterUrl">Url to the service of conversion</param>
    /// <param name="documentUri">Uri for the document to convert</param>
    /// <param name="fromExtension">Document extension</param>
    /// <param name="toExtension">Extension to which to convert</param>
    /// <param name="documentRevisionId">Key for caching on service</param>
    /// <param name="password">Password</param>
    /// <param name="region"></param>
    /// <param name="thumbnail">Thumbnail settings</param>
    /// <param name="spreadsheetLayout"></param>
    /// <param name="options"></param>
    /// <param name="isAsync">Perform conversions asynchronously</param>
    /// <param name="signatureSecret">Secret key to generate the token</param>
    /// <param name="signatureHeader">Header to transfer the token</param>
    /// <param name="sslVerification">Enable SSL verification</param>
    /// <param name="clientFactory"></param>
    /// <param name="toForm"></param>
    /// <returns>The percentage of completion of conversion</returns>
    /// <example>
    /// string convertedDocumentUri;
    /// GetConvertedUri("http://helpcenter.teamlab.com/content/GettingStarted.pdf", ".pdf", ".docx", "469971047", false, out convertedDocumentUri);
    /// </example>
    /// <exception>
    /// </exception>

    public static Task<(int ResultPercent, string ConvertedDocumentUri, string convertedFileType)> GetConvertedUriAsync(
        string documentConverterUrl,
        string documentUri,
        string fromExtension,
        string toExtension,
        string documentRevisionId,
        string password,
        string region,
        ThumbnailData thumbnail,
        SpreadsheetLayout spreadsheetLayout,
        Options options,
        bool isAsync,
        string signatureSecret,
        string signatureHeader,
        bool sslVerification,
       IHttpClientFactory clientFactory,
       bool toForm)
    {
        fromExtension = string.IsNullOrEmpty(fromExtension) ? Path.GetExtension(documentUri) : fromExtension;
        if (string.IsNullOrEmpty(fromExtension))
        {
            throw new ArgumentNullException(nameof(fromExtension), "Document's extension for conversion is not known");
        }

        if (string.IsNullOrEmpty(toExtension))
        {
            throw new ArgumentNullException(nameof(toExtension), "Extension for conversion is not known");
        }

        return InternalGetConvertedUriAsync(documentConverterUrl, documentUri, fromExtension, toExtension, documentRevisionId, password, region, thumbnail, spreadsheetLayout, options, isAsync, signatureSecret, signatureHeader, sslVerification, clientFactory, toForm);
    }

    private static async Task<(int ResultPercent, string ConvertedDocumentUri, string convertedFileType)> InternalGetConvertedUriAsync(
       string documentConverterUrl,
       string documentUri,
       string fromExtension,
       string toExtension,
       string documentRevisionId,
       string password,
       string region,
       ThumbnailData thumbnail,
       SpreadsheetLayout spreadsheetLayout,
       Options options,
       bool isAsync,
       string signatureSecret,
       string signatureHeader,
       bool sslVerification,
       IHttpClientFactory clientFactory,
       bool toForm)
    {
        var title = Path.GetFileName(documentUri ?? "");
        title = string.IsNullOrEmpty(title) || title.Contains('?') ? Guid.NewGuid().ToString() : title;

        documentRevisionId = string.IsNullOrEmpty(documentRevisionId)
                                 ? documentUri
                                 : documentRevisionId;

        documentRevisionId = GenerateRevisionId(documentRevisionId);

        documentConverterUrl = FilesLinkUtility.AddQueryString(documentConverterUrl, new Dictionary<string, string> {
            { FilesLinkUtility.ShardKey, documentRevisionId }
        });


        using var request = new HttpRequestMessage(HttpMethod.Post, documentConverterUrl);
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var httpClient = clientFactory.CreateClient(GetHttpClientName(sslVerification));

        var body = new ConvertionBody
        {
            Async = isAsync,
            FileType = fromExtension.Trim('.'),
            Key = documentRevisionId,
            OutputType = toExtension.Trim('.'),
            Title = title,
            Thumbnail = thumbnail,
            SpreadsheetLayout = spreadsheetLayout,
            Watermark = options?.WatermarkOnDraw,
            Url = documentUri,
            Region = region
        };
        if (toForm)
        {
            body.Pdf = new PdfData { Form = true };
        }

        if (!string.IsNullOrEmpty(password))
        {
            body.Password = password;
        }

        if (!string.IsNullOrEmpty(signatureSecret))
        {
            var token = JsonWebToken.Encode(new { payload = body }, signatureSecret);
            //todo: remove old scheme
            request.Headers.Add(signatureHeader, "Bearer " + token);

            token = JsonWebToken.Encode(body, signatureSecret);
            body.Token = token;
        }

        var bodyString = JsonSerializer.Serialize(body, _bodySettings);

        request.Content = new StringContent(bodyString, Encoding.UTF8, "application/json");
        string dataResponse;

        using (var response = await httpClient.SendAsync(request))
        {
            dataResponse = await response.Content.ReadAsStringAsync();
        }

        return GetResponseUri(dataResponse);
    }

    /// <summary>
    /// Converts a document that is uploaded with the request instead of being fetched from an address, which is what
    /// lets a portal behind a private network hand the document service a file it could not reach on its own. The
    /// answer carries the converted document itself rather than an address to download it from.
    /// </summary>
    /// <param name="documentConverterUrl">Url to the service of conversion that accepts an uploaded document</param>
    /// <param name="file">The document content</param>
    /// <param name="fileName">The name the document is uploaded under</param>
    /// <param name="body">The conversion parameters, as the document service expects them</param>
    /// <param name="signatureSecret">Secret key to generate the token</param>
    /// <param name="sslVerification">Enable SSL verification</param>
    /// <param name="clientFactory"></param>
    /// <returns>The converted document</returns>
    public static Task<Stream> GetConvertedFileAsync(
        string documentConverterUrl,
        Stream file,
        string fileName,
        ConvertFromFileBody body,
        string signatureSecret,
        bool sslVerification,
        IHttpClientFactory clientFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentConverterUrl);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(body);

        if (string.IsNullOrEmpty(body.OutputType))
        {
            throw new ArgumentException("Extension for conversion is not known", nameof(body));
        }

        return InternalGetConvertedFileAsync(documentConverterUrl, file, fileName, body, signatureSecret, sslVerification, clientFactory);
    }

    private static async Task<Stream> InternalGetConvertedFileAsync(
        string documentConverterUrl,
        Stream file,
        string fileName,
        ConvertFromFileBody body,
        string signatureSecret,
        bool sslVerification,
        IHttpClientFactory clientFactory)
    {
        body.FileType = body.FileType?.Trim('.');
        body.OutputType = body.OutputType.Trim('.');
        body.Key = GenerateRevisionId(body.Key);

        documentConverterUrl = FilesLinkUtility.AddQueryString(documentConverterUrl, new Dictionary<string, string> {
            { FilesLinkUtility.ShardKey, body.Key }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, documentConverterUrl);

        var httpClient = clientFactory.CreateClient(GetHttpClientName(sslVerification));

        using var content = BuildFromFileContent(body, ConverterOperation, signatureSecret, file, fileName);
        request.Content = content;

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        try
        {
            // A document service that is down answers through its own proxy, with an html error page that would
            // otherwise be saved as the converted document.
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"answered {(int)response.StatusCode} {response.StatusCode}");
            }

            // A failure comes back as the ordinary json error answer, a success as the document itself.
            if (IsJson(response))
            {
                ThrowResponseError(await response.Content.ReadAsStringAsync());
            }

            return await ResponseStream.FromMessageAsync(response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Inintiates the request to the document Server with command.
    /// </summary>
    /// <param name="documentTrackerUrl">Url to the command service</param>
    /// <param name="method">Name of method</param>
    /// <param name="documentRevisionId">Key for caching on service, whose used in editor</param>
    /// <param name="callbackUrl">Url to the callback handler</param>
    /// <param name="users">users id for drop</param>
    /// <param name="meta">file meta data for update</param>
    /// <param name="signatureSecret">Secret key to generate the token</param>
    /// <param name="signatureHeader">Header to transfer the token</param>
    /// <param name="sslVerification">Enable SSL verification</param>
    /// <param name="clientFactory"></param>
    /// <returns>Response</returns>

    public static async Task<CommandResponse> CommandRequestAsync(
        string documentTrackerUrl,
        CommandMethod method,
        string documentRevisionId,
        string callbackUrl,
        List<string> users,
        MetaData meta,
        string signatureSecret,
        string signatureHeader,
        bool sslVerification,
        IHttpClientFactory clientFactory)
    {
        documentTrackerUrl = FilesLinkUtility.AddQueryString(documentTrackerUrl, new Dictionary<string, string> {
            { FilesLinkUtility.ShardKey, documentRevisionId }
        });

        var commandTimeout = Timeout;

        if (method == CommandMethod.Version)
        {
            commandTimeout = 5000;
        }

        using var cancellationTokenSource = new CancellationTokenSource(commandTimeout);
        using var request = new HttpRequestMessage(HttpMethod.Post, documentTrackerUrl);

        var httpClient = clientFactory.CreateClient(GetHttpClientName(sslVerification));

        var body = new CommandBody
        {
            Command = method,
            Key = documentRevisionId
        };

        if (!string.IsNullOrEmpty(callbackUrl))
        {
            body.Callback = callbackUrl;
        }

        if (users is { Count: > 0 })
        {
            body.Users = users;
        }

        if (meta != null)
        {
            body.Meta = meta;
        }

        if (!string.IsNullOrEmpty(signatureSecret))
        {
            var token = JsonWebToken.Encode(new { payload = body }, signatureSecret);

            //todo: remove old scheme
            request.Headers.Add(signatureHeader, "Bearer " + token);

            token = JsonWebToken.Encode(body, signatureSecret);
            body.Token = token;
        }

        var bodyString = JsonSerializer.Serialize(body, _bodySettings);

        request.Content = new StringContent(bodyString, Encoding.UTF8, "application/json");
        string dataResponse;
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationTokenSource.Token);
            dataResponse = await response.Content.ReadAsStringAsync(cancellationTokenSource.Token);
        }
        catch (HttpRequestException e) when (e.HttpRequestError == HttpRequestError.NameResolutionError)
        {
            return new CommandResponse
            {
                Error = ErrorTypes.UnknownError,
                ErrorString = e.Message
            };
        }

        try
        {
            var commandResponse = JsonSerializer.Deserialize<CommandResponse>(dataResponse, _commonSettings);
            return commandResponse;
        }
        catch (Exception ex)
        {
            return new CommandResponse
            {
                Error = ErrorTypes.ParseError,
                ErrorString = $"{ex.Message} Content: {dataResponse}"
            };
        }
    }

    /// <summary>
    /// Inintiates the the document builder request.
    /// </summary>
    public static Task<(string DocBuilderKey, Dictionary<string, string> Urls)> DocbuilderRequestAsync(
        string docbuilderUrl,
        string requestKey,
        string scriptUrl,
        bool isAsync,
        string signatureSecret,
        string signatureHeader,
        bool sslVerification,
       IHttpClientFactory clientFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(docbuilderUrl);

        if (string.IsNullOrEmpty(requestKey) && string.IsNullOrEmpty(scriptUrl))
        {
            throw new ArgumentException("requestKey or inputScript is empty");
        }

        return InternalDocbuilderRequestAsync(docbuilderUrl, requestKey, scriptUrl, isAsync, signatureSecret, signatureHeader, sslVerification, clientFactory);
    }

    private static async Task<(string DocBuilderKey, Dictionary<string, string> Urls)> InternalDocbuilderRequestAsync(
       string docbuilderUrl,
       string requestKey,
       string scriptUrl,
       bool isAsync,
       string signatureSecret,
       string signatureHeader,
       bool sslVerification,
       IHttpClientFactory clientFactory)
    {
        docbuilderUrl = FilesLinkUtility.AddQueryString(docbuilderUrl, new Dictionary<string, string> {
            { FilesLinkUtility.ShardKey, requestKey }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, docbuilderUrl);
        var httpClient = clientFactory.CreateClient(GetHttpClientName(sslVerification));

        var body = new BuilderBody
        {
            Async = isAsync,
            Key = requestKey,
            Url = scriptUrl
        };

        if (!string.IsNullOrEmpty(signatureSecret))
        {
            var token = JsonWebToken.Encode(new { payload = body }, signatureSecret);
            //todo: remove old scheme
            request.Headers.Add(signatureHeader, "Bearer " + token);

            token = JsonWebToken.Encode(body, signatureSecret);
            body.Token = token;
        }

        var bodyString = JsonSerializer.Serialize(body, _bodySettings);

        request.Content = new StringContent(bodyString, Encoding.UTF8, "application/json");

        string dataResponse;

        using (var response = await httpClient.SendAsync(request))
        {
            dataResponse = await response.Content.ReadAsStringAsync();
        }

        return ParseDocbuilderResponse(dataResponse);
    }

    /// <summary>
    /// Runs a document builder script that is uploaded with the request instead of being fetched from an address,
    /// which is what lets a portal behind a private network hand the document service a script it could not reach on
    /// its own.
    /// </summary>
    /// <param name="docbuilderUrl">Url to the document builder service that accepts an uploaded script</param>
    /// <param name="script">The script content</param>
    /// <param name="scriptFileName">The name the script is uploaded under</param>
    /// <param name="body">The request parameters, as the document service expects them</param>
    /// <param name="signatureSecret">Secret key to generate the token</param>
    /// <param name="sslVerification">Enable SSL verification</param>
    /// <param name="clientFactory"></param>
    public static Task<(string DocBuilderKey, Dictionary<string, string> Urls)> DocbuilderRequestFromFileAsync(
        string docbuilderUrl,
        Stream script,
        string scriptFileName,
        BuilderFromFileBody body,
        string signatureSecret,
        bool sslVerification,
        IHttpClientFactory clientFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(docbuilderUrl);
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(body);

        return InternalDocbuilderRequestFromFileAsync(docbuilderUrl, script, scriptFileName, body, signatureSecret, sslVerification, clientFactory);
    }

    private static async Task<(string DocBuilderKey, Dictionary<string, string> Urls)> InternalDocbuilderRequestFromFileAsync(
        string docbuilderUrl,
        Stream script,
        string scriptFileName,
        BuilderFromFileBody body,
        string signatureSecret,
        bool sslVerification,
        IHttpClientFactory clientFactory)
    {
        if (!string.IsNullOrEmpty(body.Key))
        {
            docbuilderUrl = FilesLinkUtility.AddQueryString(docbuilderUrl, new Dictionary<string, string> {
                { FilesLinkUtility.ShardKey, body.Key }
            });
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, docbuilderUrl);
        var httpClient = clientFactory.CreateClient(GetHttpClientName(sslVerification));

        using var content = BuildFromFileContent(body, DocbuilderOperation, signatureSecret, script, scriptFileName);
        request.Content = content;

        string dataResponse;

        using (var response = await httpClient.SendAsync(request))
        {
            // A document service that is down answers through its own proxy, with an html error page that would
            // otherwise reach the json reader and be reported as a stray character.
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"answered {(int)response.StatusCode} {response.StatusCode}");
            }

            dataResponse = await response.Content.ReadAsStringAsync();
        }

        return ParseDocbuilderResponse(dataResponse);
    }

    /// <summary>
    /// Reads the answer of the document builder service: reports the error it names, and hands back the request key
    /// together with the produced files once the build has ended. The urls stay null while it is still running, which
    /// is what a caller polls on.
    /// </summary>
    private static (string DocBuilderKey, Dictionary<string, string> Urls) ParseDocbuilderResponse(string dataResponse)
    {
        if (string.IsNullOrEmpty(dataResponse))
        {
            throw new Exception("Invalid response");
        }

        var responseFromService = JObject.Parse(dataResponse);
        if (responseFromService == null)
        {
            throw new Exception("Invalid answer format");
        }

        var errorElement = responseFromService.Value<string>("error");
        if (!string.IsNullOrEmpty(errorElement))
        {
            DocumentServiceException.ProcessResponseError(errorElement);
        }

        var isEnd = responseFromService.Value<bool>("end");

        Dictionary<string, string> urls = null;
        if (isEnd)
        {
            IDictionary<string, JToken> rates = (JObject)responseFromService["urls"];

            urls = rates.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
        }

        return (responseFromService.Value<string>("key"), urls);
    }

    /// <summary>
    /// Lays out the multipart body the "from-file" endpoints expect. The parameters travel signed, inside the token
    /// part, because the document service is configured to take them from there; a portal with no signature secret
    /// sends them unsigned as the json "params" part instead. The document itself is always a separate part, so the
    /// document service never has to fetch the source on its own.
    /// </summary>
    private static MultipartFormDataContent BuildFromFileContent(FromFileBody body, string operation, string signatureSecret, Stream file, string fileName)
    {
        var content = new MultipartFormDataContent();

        if (string.IsNullOrEmpty(signatureSecret))
        {
            content.Add(new StringContent(JsonSerializer.Serialize(body, _bodySettings), Encoding.UTF8, "application/json"), "params");
        }
        else
        {
            content.Add(new StringContent(EncodeFromFileToken(body, operation, signatureSecret)), "token");
        }

        content.Add(new StreamContent(file), "file", fileName);

        return content;
    }

    /// <summary>
    /// Signs the parameters as the document service wants them for an uploaded source: the payload is the parameters
    /// themselves, with the claim that says which endpoint the token may be spent on.
    /// </summary>
    private static string EncodeFromFileToken(FromFileBody body, string operation, string signatureSecret)
    {
        var payload = JsonSerializer.SerializeToNode(body, _tokenSettings).AsObject();

        payload[OperationClaim] = operation;

        return JsonWebToken.Encode(payload, signatureSecret);
    }

    private static bool IsJson(HttpResponseMessage response)
    {
        return response.Content.Headers.ContentType?.MediaType == "application/json";
    }

    /// <summary>
    /// Reports the failure a "from-file" endpoint answered with. Success there is the document itself, so any json
    /// answer is an error answer, and one without an error code is reported as an unknown failure.
    /// </summary>
    private static void ThrowResponseError(string dataResponse)
    {
        var errorElement = JObject.Parse(dataResponse).Value<string>("error");

        DocumentServiceException.ProcessResponseError(errorElement ?? nameof(DocumentServiceException.ErrorCode.Unknown));
    }

    public static Task<bool> HealthcheckRequestAsync(string healthcheckUrl, IHttpClientFactory clientFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(healthcheckUrl);

        return InternalHealthcheckRequestAsync(healthcheckUrl, clientFactory);
    }

    private static async Task<bool> InternalHealthcheckRequestAsync(string healthcheckUrl, IHttpClientFactory clientFactory)
    {
        using var request = new HttpRequestMessage();
        request.RequestUri = new Uri(healthcheckUrl);

        var httpClient = clientFactory.CreateClient("customHttpClient");
        httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout);

        using var response = await httpClient.SendAsync(request);
        var dataResponse = await response.Content.ReadAsStringAsync();
        return dataResponse.Equals("true", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// The command method.
    /// </summary>
    [EnumExtensions]
    public enum CommandMethod
    {
        Info,
        Drop,
        Saved, //not used
        Version,
        ForceSave, //not used
        Meta,
        License
    }

    /// <summary>
    /// The command response parameters.
    /// </summary>
    [DebuggerDisplay("{Key}")]
    public class CommandResponse
    {
        /// <summary>
        /// The command response error type.
        /// </summary>
        public ErrorTypes Error { get; set; }

        /// <summary>
        /// The command response error message.
        /// </summary>
        public string ErrorString { get; set; }

        /// <summary>
        /// The document identifier used to unambiguously identify the document file.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The document license information.
        /// </summary>
        public License License { get; set; }

        /// <summary>
        /// The server characteristics.
        /// </summary>
        public ServerInfo Server { get; set; }

        /// <summary>
        /// The user quota value.
        /// </summary>
        public QuotaInfo Quota { get; set; }

        /// <summary>
        /// The ONLYOFFICE Docs version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The command response error type.
        /// </summary>
        public enum ErrorTypes
        {
            NoError = 0,
            DocumentIdError = 1,
            ParseError = 2,
            UnknownError = 3,
            NotModify = 4,
            UnknownCommand = 5,
            Token = 6,
            TokenExpire = 7
        }

        /// <summary>
        /// The server characteristics.
        /// </summary>
        [DebuggerDisplay("{BuildVersion}")]
        public class ServerInfo
        {
            /// <summary>
            /// The server build date.
            /// </summary>
            public DateTime BuildDate { get; set; }

            /// <summary>
            /// The server build number.
            /// </summary>
            public int BuildNumber { get; set; }

            /// <summary>
            /// The server build version.
            /// </summary>
            public string BuildVersion { get; set; }

            /// <summary>
            /// The server product version.
            /// </summary>
            public PackageTypes PackageType { get; set; }

            /// <summary>
            /// The license status.
            /// </summary>
            public ResultTypes ResultType { get; set; }

            /// <summary>
            /// The number of server workers.
            /// </summary>
            public int WorkersCount { get; set; }

            /// <summary>
            /// The server product version.
            /// </summary>
            public enum PackageTypes
            {
                OpenSource = 0,
                IntegrationEdition = 1,
                DeveloperEdition = 2
            }

            /// <summary>
            /// The license status.
            /// </summary>
            public enum ResultTypes
            {
                Error = 1,
                Expired = 2,
                Success = 3,
                UnknownUser = 4,
                Connections = 5,
                ExpiredTrial = 6,
                SuccessLimit = 7,
                UsersCount = 8,
                ConnectionsOS = 9,
                UsersCountOS = 10,
                ExpiredLimited = 11
            }
        }

        /// <summary>
        /// The user quota value.
        /// </summary>
        public class QuotaInfo
        {
            /// <summary>
            /// The list of user quotas for the user license.
            /// </summary>
            public List<User> Users { get; set; }

            /// <summary>
            /// The user quota information.
            /// </summary>
            [DebuggerDisplay("{UserId} ({Expire})")]
            public class User
            {
                /// <summary>
                /// The ID of the user who opened the editor.
                /// </summary>
                [JsonPropertyName("userid")]
                public string UserId { get; set; }

                /// <summary>
                /// The date of license expiration for this user.
                /// </summary>
                public DateTime Expire { get; set; }
            }
        }
    }

    /// <summary>
    /// The command body.
    /// </summary>
    [DebuggerDisplay("{Command} ({Key})")]
    private class CommandBody
    {
        /// <summary>
        /// The command method.
        /// </summary>
        [JsonIgnore]
        public CommandMethod Command { get; init; }

        /// <summary>
        /// The command type.
        /// </summary>
        public string C => Command.ToString().ToLower(CultureInfo.InvariantCulture);

        /// <summary>
        /// The command callback.
        /// </summary>
        public string Callback { get; set; }

        /// <summary>
        /// The document identifier used to unambiguously identify the document file.
        /// </summary>
        public string Key { get; init; }

        /// <summary>
        /// The new meta information of the document.
        /// </summary>
        public MetaData Meta { get; set; }

        /// <summary>
        /// The list of the user identifiers.
        /// </summary>
        public List<string> Users { get; set; }

        /// <summary>
        /// The encrypted signature added to the config in the form of a token.
        /// </summary>
        public string Token { get; set; }

        //not used
        /// <summary>
        /// Some custom identifier which will help distinguish the specific request in case there were more than one.
        /// </summary>
        [JsonPropertyName("userdata")]
        public string UserData { get; set; }
    }

    /// <summary>
    /// The PDF data.
    /// </summary>
    public class PdfData
    {
        /// <summary>
        /// Specifies if the PDF document is a PDF form or not.
        /// </summary>
        /// <example>true</example>
        public bool Form { get; set; }
    }

    /// <summary>
    /// The layout of forms printed as pdf documents or images.
    /// </summary>
    public class DocumentLayout
    {
        /// <summary>
        /// Whether placeholders are drawn or not.
        /// </summary>
        /// <example>true</example>
        public bool? DrawPlaceHolders { get; set; }

        /// <summary>
        /// Whether forms are highlighted or not.
        /// </summary>
        /// <example>true</example>
        public bool? DrawFormHighlight { get; set; }

        /// <summary>
        /// Whether the print mode is turned on. It only applies to a docx converted into pdf: with the print mode off
        /// the highlight flag does nothing and the placeholder flag saves the forms in the pdf.
        /// </summary>
        /// <example>false</example>
        public bool? IsPrint { get; set; }
    }

    /// <summary>
    /// How the text of a pdf, xps or oxps source is read back when converting from it.
    /// </summary>
    public class DocumentRenderer
    {
        /// <summary>
        /// The rendering mode: blockChar reads the text by single characters, blockLine by separate lines, plainLine as
        /// plain text with a paragraph per line, plainParagraph as plain text with lines combined into paragraphs.
        /// </summary>
        /// <example>plainLine</example>
        public string TextAssociation { get; set; }
    }

    /// <summary>
    /// The new meta information of the document.
    /// </summary>
    [DebuggerDisplay("{Title}")]
    public class MetaData
    {
        /// <summary>
        /// The new document name.
        /// </summary>
        public string Title { get; set; }
    }

    /// <summary>
    /// The thumbnail data.
    /// </summary>
    [DebuggerDisplay("{Height}x{Width}")]
    public class ThumbnailData
    {
        /// <summary>
        /// The mode to fit the image to the height and width specified:
        /// 0 - stretch file to fit height and width;
        /// 1 - keep the aspect for the image;
        /// 2 - convert the metric size of the page into pixels at 96 dpi.
        /// </summary>
        /// <example>1</example>
        public int Aspect { get; set; }

        /// <summary>
        /// Specifies if the thumbnails should be generated for the first page only or for all the document pages.
        /// </summary>
        /// <example>true</example>
        public bool First { get; set; }

        /// <summary>
        /// The thumbnail height in pixels.
        /// </summary>
        /// <example>100</example>
        public int Height { get; set; }

        /// <summary>
        /// The thumbnail width in pixels.
        /// </summary>
        /// <example>100</example>
        public int Width { get; set; }
    }

    /// <summary>
    /// The settings for converting the spreadsheet to pdf.
    /// </summary>
    [DebuggerDisplay("SpreadsheetLayout {IgnorePrintArea} {Orientation} {FitToHeight} {FitToWidth} {Headings} {GridLines}")]
    public class SpreadsheetLayout
    {
        /// <summary>
        /// Specifies whether to ignore the print area chosen for the spreadsheet file or not.
        /// </summary>
        /// <example>false</example>
        public bool IgnorePrintArea { get; set; }

        /// <summary>
        /// The orientation of the output PDF file.
        /// </summary>
        /// <example>landscape</example>
        public string Orientation { get; set; }

        /// <summary>
        /// The height of the converted area, measured in the number of pages.
        /// </summary>
        /// <example>0</example>
        public int FitToHeight { get; set; }

        /// <summary>
        /// Allows to set the scale of the output PDF file.
        /// </summary>
        /// <example>100</example>
        public int? Scale { get; set; }

        /// <summary>
        /// The width of the converted area, measured in the number of pages.
        /// </summary>
        /// <example>1</example>
        public int FitToWidth { get; set; }

        /// <summary>
        /// Specifies whether to include the headings to the output PDF file or not.
        /// </summary>
        /// <example>false</example>
        public bool Headings { get; set; }

        /// <summary>
        /// Specifies whether to include grid lines to the output PDF file or not.
        /// </summary>
        /// <example>false</example>
        public bool GridLines { get; set; }

        /// <summary>
        /// The margins of the output PDF file.
        /// </summary>
        public LayoutMargins Margins { get; set; }

        /// <summary>
        /// The page size of the output PDF file.
        /// </summary>
        public LayoutPageSize PageSize { get; set; }

        /// <summary>
        /// The margins of the output PDF file.
        /// </summary>
        [DebuggerDisplay("Margins {Top} {Right} {Bottom} {Left}")]
        public class LayoutMargins
        {
            /// <summary>
            /// The left margin of the output PDF file.
            /// </summary>
            /// <example>0.7in</example>
            public string Left { get; set; }

            /// <summary>
            /// The right margin of the output PDF file.
            /// </summary>
            /// <example>0.7in</example>
            public string Right { get; set; }

            /// <summary>
            /// The top margin of the output PDF file.
            /// </summary>
            /// <example>0.75in</example>
            public string Top { get; set; }

            /// <summary>
            /// The bottom margin of the output PDF file.
            /// </summary>
            /// <example>0.75in</example>
            public string Bottom { get; set; }
        }

        /// <summary>
        /// The page size of the output PDF file.
        /// </summary>
        [DebuggerDisplay("PageSize {Width} {Height}")]
        public class LayoutPageSize
        {
            /// <summary>
            /// The page height of the output PDF file.
            /// </summary>
            /// <example>11.69in</example>
            public string Height { get; set; }

            /// <summary>
            /// The page width of the output PDF file.
            /// </summary>
            /// <example>8.27in</example>
            public string Width { get; set; }
        }
    }

    /// <summary>
    /// The parameters shared by every request that uploads its source with the request: whether the document service
    /// answers straight away or is polled, the key that identifies the request, and the signature.
    /// </summary>
    public abstract class FromFileBody
    {
        /// <summary>
        /// Specifies whether the request to the document service is asynchronous or not.
        /// </summary>
        public bool Async { get; set; }

        /// <summary>
        /// The request identifier used to unambiguously identify the request.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The encrypted signature added to the config in the form of a token.
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>
    /// The document builder parameters without the address of the script: the shape of the "params" part of a
    /// multipart document builder request, where the script travels as a separate part.
    /// </summary>
    public class BuilderFromFileBody : FromFileBody;

    /// <summary>
    /// The conversion parameters without the address of the source document: the shape of the "params"
    /// part of a multipart conversion request, where the document travels as a separate part.
    /// </summary>
    [DebuggerDisplay("{Title} from {FileType} to {OutputType} ({Key})")]
    public class ConvertFromFileBody : FromFileBody
    {
        /// <summary>
        /// The type of the document file to be converted.
        /// </summary>
        [JsonPropertyName("filetype")]
        public string FileType { get; set; }

        /// <summary>
        /// The encoding of a csv or txt source, as a code page number. Without it such a source is read in the
        /// encoding the document service guesses, which garbles any non-latin text.
        /// </summary>
        public int? CodePage { get; set; }

        /// <summary>
        /// The character separating the values of a csv source: 0 - none, 1 - tab, 2 - semicolon, 3 - colon,
        /// 4 - comma, 5 - space.
        /// </summary>
        public int? Delimiter { get; set; }

        /// <summary>
        /// The layout of forms printed as pdf documents or images.
        /// </summary>
        public DocumentLayout DocumentLayout { get; set; }

        /// <summary>
        /// How the text of a pdf, xps or oxps source is read back.
        /// </summary>
        public DocumentRenderer DocumentRenderer { get; set; }

        /// <summary>
        /// The resulting converted document type.
        /// </summary>
        [JsonPropertyName("outputtype")]
        public string OutputType { get; set; }

        /// <summary>
        /// The password for the document file if it is protected with a password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The converted file name.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The thunmbnail settings.
        /// </summary>
        public ThumbnailData Thumbnail { get; set; }

        /// <summary>
        /// The settings for converting the spreadsheet to pdf.
        /// </summary>
        public SpreadsheetLayout SpreadsheetLayout { get; set; }

        /// <summary>
        /// The default display format for currency and date and time when converting from spreadsheet format to PDF.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// The properties of a watermark which is inserted into the PDF and image files during conversion.
        /// </summary>
        public WatermarkOnDraw Watermark { get; set; }

        /// <summary>
        /// The settings for converting document files to PDF.
        /// </summary>
        public PdfData Pdf { get; set; }
    }

    /// <summary>
    /// The conversion  body.
    /// </summary>
    private sealed class ConvertionBody : ConvertFromFileBody
    {
        /// <summary>
        /// The the absolute URL to the document to be converted.
        /// </summary>
        public required string Url { get; set; }
    }

    /// <summary>
    /// The Document Builder request body.
    /// </summary>
    [DebuggerDisplay("{Key}")]
    private sealed class BuilderBody
    {
        /// <summary>
        /// Specifies if the request to the document builder service is asynchronous or not.
        /// </summary>
        public bool Async { get; set; }

        /// <summary>
        /// The request identifier used to unambiguously identify the request.
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        /// The absolute URL to the .docbuilder file.
        /// </summary>
        public required string Url { get; set; }

        /// <summary>
        /// The encrypted signature added to the config in the form of a token.
        /// </summary>
        public string Token { get; set; }
    }

    /// <summary>
    /// The address the content of a file is fetched from, together with the signature that authorises the fetch, as
    /// the document service is handed it.
    /// </summary>
    public class FileLink
    {
        /// <summary>
        /// The format the stored content is in, lower-cased and with the leading dot, which is how the document
        /// service learns how to read the bytes behind the address. It stays empty when the file title carries no
        /// extension at all.
        /// </summary>
        /// <example>.docx</example>
        [JsonPropertyName("filetype")]
        public required string FileType { get; set; }

        /// <summary>
        /// Signs the address and the format above so that the document service can trust them. It stays empty on a
        /// portal that has no signature secret configured for the document service, and the address is then meant
        /// to be fetched unsigned.
        /// </summary>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public string Token { get; set; }

        /// <summary>
        /// Where the content is fetched from: the portal download handler, pinned to the revision the file was at
        /// when the address was issued and carrying an authorisation key of limited validity. It is addressed to
        /// the host the document service can reach, which on a deployment with a private editor network is not the
        /// address a browser should follow.
        /// </summary>
        /// <example>https://portal.example.com/filehandler.ashx?action=stream&amp;fileid=512&amp;version=3</example>
        [Url]
        public required string Url { get; set; }
    }

    public class DocumentServiceException(DocumentServiceException.ErrorCode errorCode, string message)
        : Exception(message)
    {
        public ErrorCode Code { get; set; } = errorCode;

        public static void ProcessResponseError(string errorCode)
        {
            if (!ErrorCodeExtensions.TryParse(errorCode, true, out var code) && CultureInfo.CurrentCulture.Name == "ar-SA" && !Enum.TryParse(errorCode, out code))
            {
                code = ErrorCode.Unknown;
            }
            var errorMessage = code switch
            {
                ErrorCode.SizeLimit => "size limit exceeded",
                ErrorCode.OutputType => "output format not defined",
                ErrorCode.Vkey => "document signature",
                ErrorCode.TaskQueue => "database",
                ErrorCode.ConvertPassword => "password",
                ErrorCode.ConvertDownload => "download",
                ErrorCode.Convert => "convertation",
                ErrorCode.ConvertTimeout => "convertation timeout",
                ErrorCode.Unknown => "unknown error",
                _ => "errorCode = " + errorCode
            };
            throw new DocumentServiceException(code, errorMessage);
        }

        [EnumExtensions]
        public enum ErrorCode
        {
            SizeLimit = -10,
            OutputType = -9,
            Vkey = -8,
            TaskQueue = -6,
            ConvertPassword = -5,
            ConvertDownload = -4,
            Convert = -3,
            ConvertTimeout = -2,
            Unknown = -1
        }
    }

    /// <summary>
    /// Processing the document received from the editing service.
    /// </summary>
    /// <param name="jsonDocumentResponse">The resulting json from editing service</param>
    /// <returns>The percentage of completion of conversion and Uri to the converted document</returns>
    private static (int ResultPercent, string responseuri, string convertedFileType) GetResponseUri(string jsonDocumentResponse)
    {
        if (string.IsNullOrEmpty(jsonDocumentResponse))
        {
            throw new ArgumentException("Invalid param", nameof(jsonDocumentResponse));
        }

        var responseFromService = JObject.Parse(jsonDocumentResponse);
        if (responseFromService == null)
        {
            throw new WebException("Invalid answer format");
        }

        var errorElement = responseFromService.Value<string>("error");
        if (!string.IsNullOrEmpty(errorElement))
        {
            DocumentServiceException.ProcessResponseError(errorElement);
        }

        var isEndConvert = responseFromService.Value<bool>("endConvert");

        int resultPercent;
        var responseUri = string.Empty;
        var responseType = string.Empty;
        if (isEndConvert)
        {
            responseUri = responseFromService.Value<string>("fileUrl");
            responseType = responseFromService.Value<string>("fileType");
            resultPercent = 100;
        }
        else
        {
            resultPercent = responseFromService.Value<int>("percent");
            if (resultPercent >= 100)
            {
                resultPercent = 99;
            }
        }

        return (resultPercent, responseUri, responseType);
    }
}

public static class DocumentServiceHttpClientExtension
{
    public static void AddDocumentServiceHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var httpClientTimeout = Convert.ToInt32(configuration["files:docservice:timeout"] ?? "100000");
        var policyTimeout = TimeSpan.FromSeconds(httpClientTimeout / 1000);
        var retryCount = Convert.ToInt32(configuration["files:docservice:try"] ?? "6");
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: retryCount).ToArray();

        var retryOptions = new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = retryCount,

            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TaskCanceledException>()
                            .Handle<TimeoutRejectedException>()
                            .HandleResult(response => !response.IsSuccessStatusCode),

            DelayGenerator = args => ValueTask.FromResult<TimeSpan?>(delay[args.AttemptNumber])
        };

        services.AddHttpClient(GetHttpClientName(sslVerification: true))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddResilienceHandler(ResiliencePipelineName, builder =>
                {
                    builder.AddTimeout(policyTimeout);
                    builder.AddRetry(retryOptions);
                });

        services.AddHttpClient(GetHttpClientName(sslVerification: false))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .ConfigurePrimaryHttpMessageHandler(_ =>
                {
                    var handler = new SocketsHttpHandler();
                    handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                    return handler;
                })
                .AddResilienceHandler(ResiliencePipelineName, builder =>
                {
                    builder.AddTimeout(policyTimeout);
                    builder.AddRetry(retryOptions);
                });

        services.AddHttpClient(CustomSslVerificationClient)
                .ConfigurePrimaryHttpMessageHandler(_ =>
                {
                    var handler = new SocketsHttpHandler();
                    handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                    return handler;
                });

        services.AddResiliencePipeline<string, LicenseValidationResult>(LicenseResiliencePipelineName, pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new RetryStrategyOptions<LicenseValidationResult>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<LicenseValidationResult>().HandleResult(result => result == null)
            });
        });
    }
}
