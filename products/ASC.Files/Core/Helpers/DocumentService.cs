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

using Polly.Contrib.WaitAndRetry;

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
    /// Gets the HTTP client name.
    /// </summary>
    public static string GetHttpClientName(bool sslVerification) => nameof(DocumentService) + (sslVerification ? string.Empty : CustomSslVerificationClient);

    private static readonly JsonSerializerOptions _bodySettings = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions _commonSettings = new()
    {
        AllowTrailingCommas = true, PropertyNameCaseInsensitive = true
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

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(documentConverterUrl),
            Method = HttpMethod.Post
        };
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
        string[] users,
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
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(documentTrackerUrl),
            Method = HttpMethod.Post
        };

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

        if (users is { Length: > 0 })
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

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(docbuilderUrl),
            Method = HttpMethod.Post
        };

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
            dataResponse = await  response.Content.ReadAsStringAsync();
        }

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

    public static Task<bool> HealthcheckRequestAsync(string healthcheckUrl, IHttpClientFactory clientFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(healthcheckUrl);

        return InternalHealthcheckRequestAsync(healthcheckUrl, clientFactory);
    }

    private static async Task<bool> InternalHealthcheckRequestAsync(string healthcheckUrl, IHttpClientFactory clientFactory)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(healthcheckUrl)
        };

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
        public string C
        {
            get { return Command.ToString().ToLower(CultureInfo.InvariantCulture); }
        }

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
        public string[] Users { get; set; }

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
        public bool Form { get; set; }
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
        /// 2 - in this case, the width and height settings are not used.
        /// </summary>
        public int Aspect { get; set; }

        /// <summary>
        /// Specifies if the thumbnails should be generated for the first page only or for all the document pages.
        /// </summary>
        public bool First { get; set; }

        /// <summary>
        /// The thumbnail height in pixels.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// The thumbnail width in pixels.
        /// </summary>
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
        public bool IgnorePrintArea { get; set; }

        /// <summary>
        /// The orientation of the output PDF file.
        /// </summary>
        public string Orientation { get; set; }

        /// <summary>
        /// The height of the converted area, measured in the number of pages.
        /// </summary>
        public int FitToHeight { get; set; }

        /// <summary>
        /// The width of the converted area, measured in the number of pages.
        /// </summary>
        public int FitToWidth { get; set; }

        /// <summary>
        /// Specifies whether to include the headings to the output PDF file or not.
        /// </summary>
        public bool Headings { get; set; }

        /// <summary>
        /// Specifies whether to include grid lines to the output PDF file or not.
        /// </summary>
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
            public string Left { get; set; }

            /// <summary>
            /// The right margin of the output PDF file.
            /// </summary>
            public string Right { get; set; }

            /// <summary>
            /// The top margin of the output PDF file.
            /// </summary>
            public string Top { get; set; }

            /// <summary>
            /// The bottom margin of the output PDF file.
            /// </summary>
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
            public string Height { get; set; }

            /// <summary>
            /// The page width of the output PDF file.
            /// </summary>
            public string Width { get; set; }
        }
    }

    /// <summary>
    /// The conversion  body.
    /// </summary>
    [DebuggerDisplay("{Title} from {FileType} to {OutputType} ({Key})")]
    private sealed class ConvertionBody
    {
        /// <summary>
        /// Specifies whether the conversion is asynchronous or not.
        /// </summary>
        public bool Async { get; set; }

        /// <summary>
        /// The type of the document file to be converted.
        /// </summary>
        [JsonPropertyName("filetype")]
        public required string FileType { get; init; }

        /// <summary>
        /// The document identifier used to unambiguously identify the document file..
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        /// The resulting converted document type.
        /// </summary>
        [JsonPropertyName("outputtype")]
        public required string OutputType { get; init; }

        /// <summary>
        /// The password for the document file if it is protected with a password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The converted file name.
        /// </summary>
        public string Title { get; init; }

        /// <summary>
        /// The thunmbnail settings.
        /// </summary>
        public ThumbnailData Thumbnail { get; set; }

        /// <summary>
        /// The settings for converting the spreadsheet to pdf.
        /// </summary>
        public SpreadsheetLayout SpreadsheetLayout { get; set; }

        /// <summary>
        /// The the absolute URL to the document to be converted.
        /// </summary>
        public required string Url { get; set; }

        /// <summary>
        /// The default display format for currency and date and time when converting from spreadsheet format to PDF.
        /// </summary>
        public required string Region { get; set; }

        /// <summary>
        /// The properties of a watermark which is inserted into the PDF and image files during conversion.
        /// </summary>
        public WatermarkOnDraw Watermark { get; set; }

        /// <summary>
        /// The encrypted signature added to the ONLYOFFICE Docs config in the form of a token.
        /// </summary>        
        public string Token { get; set; }

        /// <summary>
        /// The settings for converting document files to PDF.
        /// </summary>
        public PdfData Pdf { get; set; }

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
    /// The file link properties.
    /// </summary>
    public class FileLink
    {
        /// <summary>
        /// The type of the file for the source viewed or edited document.
        /// </summary>
        [JsonPropertyName("filetype")]
        public string FileType { get; set; }

        /// <summary>
        /// The encrypted signature added to the config in the form of a token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// The absolute URL where the source viewed or edited document is stored.
        /// </summary>
        [Url]
        public string Url { get; set; }
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
        var policyTimeout = httpClientTimeout / 1000;
        var retryCount = Convert.ToInt32(configuration["files:docservice:try"] ?? "6");
        var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: retryCount).ToArray();

        services.AddHttpClient(GetHttpClientName(sslVerification: true))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddResilienceHandler(ResiliencePipelineName, builder =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(policyTimeout));

                    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        MaxRetryAttempts = retryCount,

                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TaskCanceledException>()
                            .HandleResult(response => !response.IsSuccessStatusCode),

                        DelayGenerator = (args) =>
                        {
                            return ValueTask.FromResult<TimeSpan?>(delay[args.AttemptNumber - 1]);
                        }
                    });
                });

        services.AddHttpClient(GetHttpClientName(sslVerification: false))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .ConfigurePrimaryHttpMessageHandler(_ =>
                {
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    };
                })
                .AddResilienceHandler(ResiliencePipelineName, builder =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(policyTimeout));

                    builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                    {
                        MaxRetryAttempts = retryCount,

                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TaskCanceledException>()
                            .HandleResult(response => !response.IsSuccessStatusCode),

                        DelayGenerator = (args) =>
                        {
                            return ValueTask.FromResult<TimeSpan?>(delay[args.AttemptNumber - 1]);
                        }
                    });
                });

        services.AddHttpClient(CustomSslVerificationClient)
                .ConfigurePrimaryHttpMessageHandler(_ =>
                {
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    };
                });
    }
}