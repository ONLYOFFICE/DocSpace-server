// (c) Copyright Ascensio System SIA 2010-2022
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

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ASC.Web.Core.Helpers;

[Scope]
public class ApiSystemHelper
{
    public string ApiSystemUrl { get; private set; }
    private static byte[] _skey;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly IHttpClientFactory _clientFactory;
    private readonly AmazonDynamoDBClient _awsDynamoDBClient;
    private readonly TenantDomainValidator _tenantDomainValidator;
    private readonly CoreBaseSettings _coreBaseSettings;

    public ApiSystemHelper(IConfiguration configuration,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        MachinePseudoKeys machinePseudoKeys,
        IHttpClientFactory clientFactory,
        TenantDomainValidator tenantDomainValidator)
    {
        ApiSystemUrl = configuration["web:api-system"];
        _commonLinkUtility = commonLinkUtility;
        _skey = machinePseudoKeys.GetMachineConstant();

        var awsAccessKeyId = configuration["aws:dynamoDB:accessKeyId"];
        var awsSecretAccessKey = configuration["aws:dynamoDB:secretAccessKey"];

        if (!string.IsNullOrEmpty(awsAccessKeyId) && !string.IsNullOrEmpty(awsSecretAccessKey))
        {
            _awsDynamoDBClient = new AmazonDynamoDBClient(awsAccessKeyId, awsSecretAccessKey);
        }

        _clientFactory = clientFactory;
        _tenantDomainValidator = tenantDomainValidator;
        _coreBaseSettings = coreBaseSettings;
    }


    public string CreateAuthToken(string pkey)
    {
        using var hasher = new HMACSHA1(_skey);
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var hash = WebEncoders.Base64UrlEncode(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", now, pkey))));
        return $"ASC {pkey}:{now}:{hash}1"; //hack for .net
    }

    #region system

    public async Task ValidatePortalNameAsync(string domain, Guid userId)
    {
        try
        {
            var data = "{\"portalName\":\"" + HttpUtility.UrlEncode(domain) + "\"}";
            await SendToApiAsync(ApiSystemUrl, "portal/validateportalname", WebRequestMethods.Http.Post, userId, data);
        }
        catch (WebException exception)
        {
            if (exception.Status != WebExceptionStatus.ProtocolError || exception.Response == null)
            {
                return;
            }

            var response = exception.Response;
            try
            {
                await using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var result = await reader.ReadToEndAsync();

                var resObj = JObject.Parse(result);
                if (resObj["error"] != null)
                {
                    if (resObj["error"].ToString() == "portalNameExist")
                    {
                        var varians = resObj.Value<JArray>("variants").Select(jv => jv.Value<string>());
                        throw new TenantAlreadyExistsException("Address busy.", varians);
                    }

                    throw new Exception(resObj["error"].ToString());
                }
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }
    }

    #endregion

    #region cache

    public async Task AddTenantToCacheAsync(string tenantDomain, string tenantRegion)
    {      
        var putItemRequest = new PutItemRequest
        {
            TableName = "docspace-tenants_origin",
            Item = new Dictionary<string, AttributeValue>()
                {
                    { "tenant_domain", new AttributeValue {
                            S = tenantDomain
                        }},
                    { "region", new AttributeValue {
                            S = tenantRegion
                    }}
                }
        };

        await _awsDynamoDBClient.PutItemAsync(putItemRequest);
    }

    public async Task UpdateTenantToCacheAsync(string oldTenantDomain, string newTenantDomain)
    {
        var getItemRequest = new GetItemRequest
        {
            TableName = "docspace-tenants_origin",
            Key = new Dictionary<string, AttributeValue>()
                {
                    { "tenant_domain", new AttributeValue { S = oldTenantDomain } }
                },
            ProjectionExpression = "region",
            ConsistentRead = true
        };

        var region = (await _awsDynamoDBClient.GetItemAsync(getItemRequest)).Item.Values.First().S;            

        await AddTenantToCacheAsync(newTenantDomain, region);
        await RemoveTenantFromCacheAsync(oldTenantDomain);
    }

    public async Task RemoveTenantFromCacheAsync(string tenantDomain)
    {   
        var request = new DeleteItemRequest
        {
            TableName = "docspace-tenants_origin",
            Key = new Dictionary<string, AttributeValue>() 
                { 
                  { "tenant_domain", new AttributeValue { S = tenantDomain } 
                } 
            },
        };

        await _awsDynamoDBClient.DeleteItemAsync(request);
    }

    public async Task<IEnumerable<string>> FindTenantsInCacheAsync(string portalName)
    {
        var tenantDomain = $"{portalName}.{_coreBaseSettings.Basedomain}";

        var getItemRequest = new GetItemRequest
        {
            TableName = "docspace-tenants_origin",
            Key = new Dictionary<string, AttributeValue>()
                {
                    { "tenant_domain", new AttributeValue { S = tenantDomain } }
                },
            ProjectionExpression = "origin_domain",
            ConsistentRead = true
        };

        var getItemResponse = await _awsDynamoDBClient.GetItemAsync(getItemRequest);

        if (getItemResponse.Item.Count == 0) return null;

        // cut number suffix
        while (true)
        {
            if (_tenantDomainValidator.MinLength < portalName.Length && char.IsNumber(portalName, portalName.Length - 1))
            {
                portalName = portalName[0..^1];
            }
            else
            {
                break;
            }
        }

        var scanRequest = new ScanRequest
        {
            TableName = "docspace-tenants_origin",
            FilterExpression = "begins_with(tenant_domain, :v_origin_domain)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                                {":v_origin_domain", new AttributeValue { S =  portalName }} },
            ProjectionExpression = "origin_domain",
            ConsistentRead = true
        };

        var scanResponce = await _awsDynamoDBClient.ScanAsync(scanRequest);
        var result = scanResponce.Items.Select(x => x.Values.First().S.Split('.')[0]);

        return result;
    }

    #endregion

    private async Task<string> SendToApiAsync(string absoluteApiUrl, string apiPath, string httpMethod, Guid userId, string data = null)
    {
        if (!Uri.TryCreate(absoluteApiUrl, UriKind.Absolute, out var uri))
        {
            var appUrl = _commonLinkUtility.GetFullAbsolutePath("/");
            absoluteApiUrl = $"{appUrl.TrimEnd('/')}/{absoluteApiUrl.TrimStart('/')}".TrimEnd('/');
        }

        var url = $"{absoluteApiUrl}/{apiPath}";

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = new HttpMethod(httpMethod)
        };
        request.Headers.Add("Authorization", CreateAuthToken(userId.ToString()));
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        if (data != null)
        {
            request.Content = new StringContent(data, Encoding.UTF8, "application/json");
        }

        var httpClient = _clientFactory.CreateClient();
        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
