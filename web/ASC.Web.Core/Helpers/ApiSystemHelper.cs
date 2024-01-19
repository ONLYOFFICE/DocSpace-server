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

    public bool ApiCacheEnable
    {
        get => _dynamoDbSettings.ApiCacheEnable;
    }

    private readonly byte[] _skey;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly IHttpClientFactory _clientFactory;
    private readonly TenantDomainValidator _tenantDomainValidator;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly DynamoDbSettings _dynamoDbSettings;
    private const string TenantRegionKey = "tenant_region";
    private const string TenantDomainKey = "tenant_domain";
    private readonly string _regionTableName;

    public ApiSystemHelper(
        ConfigurationExtension configuration,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        MachinePseudoKeys machinePseudoKeys,
        IHttpClientFactory clientFactory,
        TenantDomainValidator tenantDomainValidator)
    {
        ApiSystemUrl = configuration["web:api-system"];
        _commonLinkUtility = commonLinkUtility;
        _skey = machinePseudoKeys.GetMachineConstant();
        _clientFactory = clientFactory;
        _tenantDomainValidator = tenantDomainValidator;
        _coreBaseSettings = coreBaseSettings;
        _dynamoDbSettings = configuration.GetSetting<DynamoDbSettings>("aws:dynamoDB");
        _regionTableName = !string.IsNullOrEmpty(_dynamoDbSettings.TableName) ? _dynamoDbSettings.TableName: "docspace-tenants_region";
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

    public async Task AddTenantToCacheAsync(string tenantDomain, string tenantRegion)
    {
        if (String.IsNullOrEmpty(tenantRegion))
        {
            tenantRegion = "default";
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

        await awsDynamoDbClient.PutItemAsync(putItemRequest);
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

    public async Task RemoveTenantFromCacheAsync(string tenantDomain)
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

        await awsDynamoDbClient.DeleteItemAsync(request);
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

        if (getItemResponse.Item.Count == 0)
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

public class DynamoDbSettings
{
    public string AccessKeyId { get; set; }
    public string SecretAccessKey { get; set; }
    public string Region { get; set; }
    public string TableName { get; set; }

    public bool ApiCacheEnable => !String.IsNullOrEmpty(AccessKeyId) &&
                                  !String.IsNullOrEmpty(SecretAccessKey);
}
