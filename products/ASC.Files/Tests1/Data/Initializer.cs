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

extern alias ASCWebApi;

namespace ASC.Files.Tests1.Data;

public class Initializer
{
    private static readonly (string Email, string Password) _user = ("test@example.com", "11111111");
    
    private static bool _initialized;
    private static HttpClient? _apiClient;
    
    public static async Task InitializeAsync(FilesApiFactory filesFactory, WebApplicationFactory<WebApiProgram> factory)
    {
        var passwordHasher = filesFactory.Services.GetRequiredService<PasswordHasher>();
        
        if (!_initialized)
        {
            _apiClient = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("log:dir", Path.Combine("..", "..", "..", "Logs", "Test"));
                builder.UseSetting("$STORAGE_ROOT", Path.Combine("..", "..", "..", "Data", "Test"));
                builder.UseSetting("ConnectionStrings:default:connectionString", filesFactory.ConnectionString);
                builder.UseSetting("web:hub:internal", "");
            }).CreateClient();

            _apiClient.BaseAddress = new Uri(_apiClient.BaseAddress, "api/2.0/");

            var response = await _apiClient.GetAsync("settings");
            var settings = await HttpClientHelper.ReadFromJson<SettingsDto>(response);
            
            if (!string.IsNullOrEmpty(settings?.WizardToken))
            {
                _apiClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", settings.WizardToken);

                response = await _apiClient.PutAsJsonAsync("settings/wizard/complete", new WizardRequestsDto { Email = _user.Email, PasswordHash = passwordHasher.GetClientPassword(_user.Password) });

                _ = await HttpClientHelper.ReadFromJson<WizardSettings>(response);
            }
        }
        
        var authenticationResponse = await _apiClient.PostAsJsonAsync("authentication", new AuthRequestsDto { UserName = _user.Email, PasswordHash = passwordHasher.GetClientPassword(_user.Password) }, filesFactory.JsonRequestSerializerOptions);

        var authenticationTokenDto = await HttpClientHelper.ReadFromJson<AuthenticationTokenDto>(authenticationResponse);
        if (authenticationTokenDto != null)
        {
            filesFactory.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationTokenDto.Token);
        }
        
        _ = await filesFactory.HttpClient.GetAsync("files/@root");

        if (!_initialized)
        {
            await filesFactory.BackupTables();
        }

        _initialized = true;
    }
}