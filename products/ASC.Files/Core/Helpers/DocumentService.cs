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

using Polly;
using Polly.Extensions.Http;

namespace ASC.Files.Core.Helpers;

public static class DocumentService
{
    private const int Timeout = 120000;
    

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
    /// The method is to convert the file to the required format
    /// </summary>
    /// <param name="fileUtility"></param>
    /// <param name="documentConverterUrl">Url to the service of conversion</param>
    /// <param name="documentUri">Uri for the document to convert</param>
    /// <param name="fromExtension">Document extension</param>
    /// <param name="toExtension">Extension to which to convert</param>
    /// <param name="documentRevisionId">Key for caching on service</param>
    /// <param name="password">Password</param>
    /// <param name="region"></param>
    /// <param name="thumbnail">Thumbnail settings</param>
    /// <param name="spreadsheetLayout"></param>
    /// <param name="isAsync">Perform conversions asynchronously</param>
    /// <param name="signatureSecret">Secret key to generate the token</param>
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
        FileUtility fileUtility,
        string documentConverterUrl,
        string documentUri,
        string fromExtension,
        string toExtension,
        string documentRevisionId,
        string password,
        string region,
        ThumbnailData thumbnail,
        SpreadsheetLayout spreadsheetLayout,
        bool isAsync,
        string signatureSecret,
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

        return InternalGetConvertedUriAsync(fileUtility, documentConverterUrl, documentUri, fromExtension, toExtension, documentRevisionId, password, region, thumbnail, spreadsheetLayout, isAsync, signatureSecret, clientFactory, toForm);
    }

    private static async Task<(int ResultPercent, string ConvertedDocumentUri, string convertedFileType)> InternalGetConvertedUriAsync(
       FileUtility fileUtility,
       string documentConverterUrl,
       string documentUri,
       string fromExtension,
       string toExtension,
       string documentRevisionId,
       string password,
       string region,
       ThumbnailData thumbnail,
       SpreadsheetLayout spreadsheetLayout,
       bool isAsync,
       string signatureSecret,
       IHttpClientFactory clientFactory,
       bool toForm)
    {
        var title = Path.GetFileName(documentUri ?? "");
        title = string.IsNullOrEmpty(title) || title.Contains('?') ? Guid.NewGuid().ToString() : title;

        documentRevisionId = string.IsNullOrEmpty(documentRevisionId)
                                 ? documentUri
                                 : documentRevisionId;
        documentRevisionId = GenerateRevisionId(documentRevisionId);

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(documentConverterUrl),
            Method = HttpMethod.Post
        };
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        var httpClient = clientFactory.CreateClient(nameof(DocumentService));

        var body = new ConvertionBody
        {
            Async = isAsync,
            FileType = fromExtension.Trim('.'),
            Key = documentRevisionId,
            OutputType = toExtension.Trim('.'),
            Title = title,
            Thumbnail = thumbnail,
            SpreadsheetLayout = spreadsheetLayout,
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
            request.Headers.Add(fileUtility.SignatureHeader, "Bearer " + token);

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
    /// Request to Document Server with command
    /// </summary>
    /// <param name="fileUtility"></param>
    /// <param name="documentTrackerUrl">Url to the command service</param>
    /// <param name="method">Name of method</param>
    /// <param name="documentRevisionId">Key for caching on service, whose used in editor</param>
    /// <param name="callbackUrl">Url to the callback handler</param>
    /// <param name="users">users id for drop</param>
    /// <param name="meta">file meta data for update</param>
    /// <param name="signatureSecret">Secret key to generate the token</param>
    /// <param name="clientFactory"></param>
    /// <returns>Response</returns>

    public static async Task<CommandResponse> CommandRequestAsync(FileUtility fileUtility,
        string documentTrackerUrl,
        CommandMethod method,
        string documentRevisionId,
        string callbackUrl,
        string[] users,
        MetaData meta,
        string signatureSecret,
        IHttpClientFactory clientFactory)
    {
        var defaultTimeout = Timeout;
        var commandTimeout = defaultTimeout;

        if (method == CommandMethod.Version)
        {
            commandTimeout = 5000;
        }

        var cancellationTokenSource = new CancellationTokenSource(commandTimeout);
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(documentTrackerUrl),
            Method = HttpMethod.Post
        };

        var httpClient = clientFactory.CreateClient(nameof(DocumentService));

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
            request.Headers.Add(fileUtility.SignatureHeader, "Bearer " + token);

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
                ErrorString = ex.Message
            };
        }
    }

    public static Task<(string DocBuilderKey, Dictionary<string, string> Urls)> DocbuilderRequestAsync(
        FileUtility fileUtility,
        string docbuilderUrl,
        string requestKey,
        string scriptUrl,
        bool isAsync,
        string signatureSecret,
       IHttpClientFactory clientFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(docbuilderUrl);

        if (string.IsNullOrEmpty(requestKey) && string.IsNullOrEmpty(scriptUrl))
        {
            throw new ArgumentException("requestKey or inputScript is empty");
        }

        return InternalDocbuilderRequestAsync(fileUtility, docbuilderUrl, requestKey, scriptUrl, isAsync, signatureSecret, clientFactory);
    }

    private static async Task<(string DocBuilderKey, Dictionary<string, string> Urls)> InternalDocbuilderRequestAsync(
       FileUtility fileUtility,
       string docbuilderUrl,
       string requestKey,
       string scriptUrl,
       bool isAsync,
       string signatureSecret,
       IHttpClientFactory clientFactory)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(docbuilderUrl),
            Method = HttpMethod.Post
        };

        var httpClient = clientFactory.CreateClient(nameof(DocumentService));

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
            request.Headers.Add(fileUtility.SignatureHeader, "Bearer " + token);

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

    [DebuggerDisplay("{Key}")]
    public class CommandResponse
    {
        public ErrorTypes Error { get; set; }
        
        public string ErrorString { get; set; }
        
        public string Key { get; set; }
        
        public License License { get; set; }
        
        public ServerInfo Server { get; set; }
        
        public QuotaInfo Quota { get; set; }
        
        public string Version { get; set; }

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

        [DebuggerDisplay("{BuildVersion}")]
        public class ServerInfo
        {
            public DateTime BuildDate { get; set; }
            
            public int BuildNumber { get; set; }
            public string BuildVersion { get; set; }
            
            public PackageTypes PackageType { get; set; }
            
            public ResultTypes ResultType { get; set; }
            
            public int WorkersCount { get; set; }

            public enum PackageTypes
            {
                OpenSource = 0,
                IntegrationEdition = 1,
                DeveloperEdition = 2
            }

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

        public class QuotaInfo
        {
            public List<User> Users { get; set; }

            [DebuggerDisplay("{UserId} ({Expire})")]
            public class User
            {
                [JsonPropertyName("userid")]
                public string UserId { get; set; }
                
                public DateTime Expire { get; set; }
            }
        }
    }

    [DebuggerDisplay("{Command} ({Key})")]
    private class CommandBody
    {
        [JsonIgnore]
        public CommandMethod Command { get; init; }

        public string C
        {
            get { return Command.ToString().ToLower(CultureInfo.InvariantCulture); }
        }

        public string Callback { get; set; }
        
        public string Key { get; init; }
        public MetaData Meta { get; set; }
        
        public string[] Users { get; set; }
        
        public string Token { get; set; }

        //not used
        [JsonPropertyName("userdata")]
        public string UserData { get; set; }
    }

    public class PdfData
    {
        public bool Form { get; set; }
    }

    [DebuggerDisplay("{Title}")]
    public class MetaData
    {
        public string Title { get; set; }
    }

    [DebuggerDisplay("{Height}x{Width}")]
    public class ThumbnailData
    {
        public int Aspect { get; set; }
        public bool First { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }

    [DebuggerDisplay("SpreadsheetLayout {IgnorePrintArea} {Orientation} {FitToHeight} {FitToWidth} {Headings} {GridLines}")]
    public class SpreadsheetLayout
    {
        public bool IgnorePrintArea { get; set; }
        public string Orientation { get; set; }
        public int FitToHeight { get; set; }
        public int FitToWidth { get; set; }
        public bool Headings { get; set; }
        public bool GridLines { get; set; }
        public LayoutMargins Margins { get; set; }
        public LayoutPageSize PageSize { get; set; }


        [DebuggerDisplay("Margins {Top} {Right} {Bottom} {Left}")]
        public class LayoutMargins
        {
            public string Left { get; set; }
            public string Right { get; set; }
            public string Top { get; set; }
            public string Bottom { get; set; }
        }

        [DebuggerDisplay("PageSize {Width} {Height}")]
        public class LayoutPageSize
        {
            public string Height { get; set; }
            public string Width { get; set; }
        }
    }

    [DebuggerDisplay("{Title} from {FileType} to {OutputType} ({Key})")]
    private sealed class ConvertionBody
    {
        public bool Async { get; set; }

        [JsonPropertyName("filetype")]
        public required string FileType { get; init; }
        public required string Key { get; init; }

        [JsonPropertyName("outputtype")]
        public required string OutputType { get; init; }
        public string Password { get; set; }
        public string Title { get; init; }
        public ThumbnailData Thumbnail { get; set; }
        public SpreadsheetLayout SpreadsheetLayout { get; set; }
        public required string Url { get; set; }
        public required string Region { get; set; }
        public string Token { get; set; }
        public PdfData Pdf { get; set; }

    }

    [DebuggerDisplay("{Key}")]
    private sealed class BuilderBody
    {
        public bool Async { get; set; }
        public required string Key { get; init; }
        public required string Url { get; set; }
        public string Token { get; set; }
    }

    public class FileLink
    {
        [JsonPropertyName("filetype")]
        public string FileType { get; set; }
        public string Token { get; set; }
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
    /// Processing document received from the editing service
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
        
        services.AddHttpClient(nameof(DocumentService))
            .SetHandlerLifetime(TimeSpan.FromMilliseconds(Convert.ToInt32(configuration["files:docservice:timeout"] ?? "5000")))
            .AddPolicyHandler((_, _) => 
                HttpPolicyExtensions
                .HandleTransientHttpError() 
                .OrResult(response => response.IsSuccessStatusCode
                    ? false
                    : throw new HttpRequestException($"Response status code: {response.StatusCode}", null, response.StatusCode))
                .WaitAndRetryAsync(Convert.ToInt32(configuration["files:docservice:try"] ?? "3"), retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
    }
}