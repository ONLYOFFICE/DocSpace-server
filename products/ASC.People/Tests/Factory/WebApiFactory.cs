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

extern alias ASCWebApi;
using ASC.People.Tests.Data;

using DocSpace.API.SDK.Api.Authentication;
using DocSpace.API.SDK.Api.Portal;
using DocSpace.API.SDK.Api.Settings;

using QuotaApi = DocSpace.API.SDK.Api.People.QuotaApi;

namespace ASC.People.Tests.Factory;

public class WepApiFactory : WebApplicationFactory<WebApiProgram>, IAsyncLifetime
{
    public HttpClient HttpClient { get; private set; } = null!;
    public QuotaApi SettingsQuotaApi { get; private set; } = null!;
    public AuthenticationApi AuthenticationApi { get; private set; } = null!;
    public CommonSettingsApi CommonSettingsApi { get; private set; } = null!;
    public UsersApi PortalUsersApi { get; private set; } = null!;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configBuilder =>
        {
            configBuilder.AddInMemoryCollection(Initializer.GlobalSettings);
        });

        return base.CreateHost(builder);
    }

    public ValueTask InitializeAsync()
    {
        HttpClient = CreateClient();

        var configuration = new Configuration { BasePath = HttpClient.BaseAddress!.ToString().TrimEnd('/') };
        SettingsQuotaApi = new QuotaApi(HttpClient, configuration);
        AuthenticationApi = new AuthenticationApi(HttpClient, configuration);
        CommonSettingsApi = new CommonSettingsApi(HttpClient, configuration);
        PortalUsersApi = new UsersApi(HttpClient, configuration);

        return ValueTask.CompletedTask;
    }
}