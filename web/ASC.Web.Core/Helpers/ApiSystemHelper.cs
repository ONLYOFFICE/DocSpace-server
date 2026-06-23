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

using System.Text.Json.Nodes;

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ASC.Web.Core.Helpers;

[Scope]
public class ApiSystemHelper
{
    public string ApiSystemUrl { get; }

    public bool ApiCacheEnable => _dynamoDbSettings.ApiCacheEnable;

    private readonly byte[] _skey;
    private readonly ILogger<ApiSystemHelper> _logger;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly IHttpClientFactory _clientFactory;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly DynamoDbSettings _dynamoDbSettings;
    private const string TenantRegionKey = "tenant_region";
    private const string TenantDomainKey = "tenant_domain";
    private readonly string _regionTableName;
    private readonly Dictionary<string, string> _regions = new()
    {
        { "us-west-2", "US" },
        { "us-east-2", "US" },
        { "eu-central-1", "DEU" }
    };

    public ApiSystemHelper(
        IConfiguration configuration,
        ILogger<ApiSystemHelper> logger,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        MachinePseudoKeys machinePseudoKeys,
        IHttpClientFactory clientFactory)
    {
        ApiSystemUrl = configuration["web:api-system"];
        _logger = logger;
        _commonLinkUtility = commonLinkUtility;
        _skey = machinePseudoKeys.GetMachineConstant();
        _clientFactory = clientFactory;
        _coreBaseSettings = coreBaseSettings;
        _dynamoDbSettings = configuration.GetSection("aws:dynamoDB").Get<DynamoDbSettings>();
        _regionTableName = !string.IsNullOrEmpty(_dynamoDbSettings.TableName) ? _dynamoDbSettings.TableName : "docspace-tenants_region";
    }

    public string CreateAuthToken(string pkey)
    {
        using var hasher = new HMACSHA1(_skey);
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var hash = WebEncoders.Base64UrlEncode(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", now, pkey))));
        return $"ASC {pkey}:{now}:{hash}1"; //hack for .net
    }

    #region system

    public async Task ValidatePortalNameAsync(string domain, Guid userId)
    {
        var data = new
        {
            PortalName = HttpUtility.UrlEncode(domain)
        };

        var dataJson = JsonSerializer.Serialize(data);
        var result = await SendToApiAsync(ApiSystemUrl, "portal/validateportalname", WebRequestMethods.Http.Post, userId, dataJson);
        var resObj = JsonNode.Parse(result)?.AsObject();
        if (resObj?["error"] != null)
        {
            if (resObj["error"].ToString() == "portalNameExist")
            {
                var variants = resObj["variants"].AsArray().Select(r => r.ToString()).ToList();
                throw new TenantAlreadyExistsException("Address busy.", variants);
            }

            throw new Exception(resObj["error"].ToString());
        }
    }


    #endregion

    #region cache

    public async Task<HttpStatusCode> AddTenantToCacheAsync(string tenantDomain, string tenantRegion)
    {
        if (string.IsNullOrEmpty(tenantRegion))
        {
            throw new ArgumentNullException(nameof(tenantRegion));
        }

        using var awsDynamoDbClient = GetDynamoDBClient();

        var putItemRequest = new PutItemRequest
        {
            TableName = _regionTableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { TenantDomainKey, new AttributeValue
                    {
                        S = tenantDomain
                    }
                },
                { TenantRegionKey, new AttributeValue
                    {
                        S = tenantRegion
                    }
                }
            }
        };

        var response = await awsDynamoDbClient.PutItemAsync(putItemRequest);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            _logger.ErrorAddTenantToCache(tenantDomain, tenantRegion, response.HttpStatusCode.ToString());
        }

        return response.HttpStatusCode;
    }

    public async Task UpdateTenantToCacheAsync(string oldTenantDomain, string newTenantDomain)
    {
        using var awsDynamoDbClient = GetDynamoDBClient();

        var getItemRequest = new GetItemRequest
        {
            TableName = _regionTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                    { TenantDomainKey, new AttributeValue { S = oldTenantDomain } }
            },
            ProjectionExpression = TenantRegionKey,
            ConsistentRead = true
        };

        var region = (await awsDynamoDbClient.GetItemAsync(getItemRequest)).Item.Values.First().S;

        await AddTenantToCacheAsync(newTenantDomain, region);
        await RemoveTenantFromCacheAsync(oldTenantDomain);
    }

    public async Task<HttpStatusCode> RemoveTenantFromCacheAsync(string tenantDomain)
    {
        using var awsDynamoDbClient = GetDynamoDBClient();

        var request = new DeleteItemRequest
        {
            TableName = _regionTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { TenantDomainKey, new AttributeValue { S = tenantDomain } }
            }
        };

        var response = await awsDynamoDbClient.DeleteItemAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            _logger.ErrorRemoveTenantFromCache(tenantDomain, response.HttpStatusCode.ToString());
        }

        return response.HttpStatusCode;
    }

    public async Task<string> GetTenantRegionAsync(string portalName)
    {
        using var awsDynamoDbClient = GetDynamoDBClient();

        portalName = portalName.Trim().ToLowerInvariant();

        var tenantDomain = $"{portalName}.{_coreBaseSettings.Basedomain}";

        var getItemRequest = new GetItemRequest
        {
            TableName = _regionTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { TenantDomainKey, new AttributeValue { S = tenantDomain } }
            },
            ProjectionExpression = TenantRegionKey,
            ConsistentRead = true
        };

        var getItemResponse = await awsDynamoDbClient.GetItemAsync(getItemRequest);

        if (getItemResponse.Item.TryGetValue(TenantRegionKey, out var region))
        {
            if (_regions.TryGetValue(region.S, out var value))
            {
                return value;
            }
        }

        return null;
    }

    public async Task<IEnumerable<string>> FindTenantsInCacheAsync(string portalName)
    {
        using var awsDynamoDbClient = GetDynamoDBClient();

        portalName = portalName.Trim().ToLowerInvariant();

        var tenantDomain = $"{portalName}.{_coreBaseSettings.Basedomain}";

        var getItemRequest = new GetItemRequest
        {
            TableName = _regionTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { TenantDomainKey, new AttributeValue { S = tenantDomain } }
            },
            ProjectionExpression = TenantRegionKey,
            ConsistentRead = true
        };

        var getItemResponse = await awsDynamoDbClient.GetItemAsync(getItemRequest);

        if (getItemResponse?.Item == null || getItemResponse.Item.Count == 0)
        {
            return null;
        }

        //// cut number suffix
        //while (true)
        //{
        //    if (_tenantDomainValidator.MinLength < portalName.Length && char.IsNumber(portalName, portalName.Length - 1))
        //    {
        //        portalName = portalName[0..^1];
        //    }
        //    else
        //    {
        //        break;
        //    }
        //}

        //var scanRequest = new ScanRequest
        //{
        //    TableName = _regionTableName,
        //    FilterExpression = "begins_with(tenant_domain, :v_tenant_domain)",
        //    ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
        //                                        {":v_tenant_domain", new AttributeValue { S =  portalName }} },
        //    ProjectionExpression = TenantDomainKey,
        //    ConsistentRead = true
        //};

        //var scanResponse = await awsDynamoDbClient.ScanAsync(scanRequest);
        //var result = scanResponse.Items.Select(x => x.Values.First().S.Split('.')[0]);

        return new List<string> { portalName };
    }

    #endregion

    private AmazonDynamoDBClient GetDynamoDBClient()
    {
        return new AmazonDynamoDBClient(_dynamoDbSettings.AccessKeyId, _dynamoDbSettings.SecretAccessKey, RegionEndpoint.GetBySystemName(_dynamoDbSettings.Region));
    }

    private async Task<string> SendToApiAsync(string absoluteApiUrl, string apiPath, string httpMethod, Guid userId, string data = null)
    {
        if (!Uri.TryCreate(absoluteApiUrl, UriKind.Absolute, out _))
        {
            var appUrl = _commonLinkUtility.GetFullAbsolutePath("/");
            absoluteApiUrl = $"{appUrl.TrimEnd('/')}/{absoluteApiUrl.TrimStart('/')}".TrimEnd('/');
        }

        var url = $"{absoluteApiUrl}/{apiPath}";

        using var request = new HttpRequestMessage(new HttpMethod(httpMethod), new Uri(url));
        request.Headers.Add("Authorization", CreateAuthToken(userId.ToString()));
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        if (data != null)
        {
            request.Content = new StringContent(data, Encoding.UTF8, "application/json");
        }
#pragma warning disable CA2000 // HttpClient is short-lived and disposed by runtime
        var httpClient = _clientFactory.CreateClient();
#pragma warning restore CA2000
        using var response = await httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}

public class DynamoDbSettings
{
    public string AccessKeyId { get; set; }
    public string SecretAccessKey { get; set; }
    public string Region { get; set; }
    public string TableName { get; set; }

    public bool ApiCacheEnable => !string.IsNullOrEmpty(AccessKeyId) &&
                                  !string.IsNullOrEmpty(SecretAccessKey);
}
