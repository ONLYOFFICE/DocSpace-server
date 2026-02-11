using DocSpace.API.SDK.Api.Authentication;

namespace ASC.Data.Stress.Core;

public static class ApiHelper
{
    
    public static async Task<Configuration> GetConfigurationAsync(string userName, string password)
    {
        var authorizationApi = new AuthenticationApi();
        var response = (await authorizationApi.AuthenticateMeFromBodyWithCodeAsync("", new AuthRequestsDto(userName, password))).Response;
        
        var configuration = new Configuration
        {
            DefaultHeaders =
            {
                ["Authorization"] = $"Bearer {response.Token}"
            }
        };

        return configuration;
    }
}