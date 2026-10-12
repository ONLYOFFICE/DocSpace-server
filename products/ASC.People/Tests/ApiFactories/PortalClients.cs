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

namespace ASC.People.Tests.ApiFactories;

/// <summary>
/// The People suite's per-portal API clients.
/// </summary>
public sealed class PortalClients : PortalClientsBase
{
    public HttpClient FilesHttpClient { get; }
    public HttpClient PeopleHttpClient { get; }

    // Files service — the room-scoped scenarios the People tests set up (sharing a room with a
    // user or a group, checking who a room is shared with, personal-folder clean-up).
    public RoomsApi RoomsApi { get; }
    public FilesApi FilesApi { get; }
    public FoldersApi FoldersApi { get; }
    public SharingApi SharingApi { get; }

    // People service
    public ProfilesApi ProfilesApi { get; }
    public PeopleSearchApi PeopleSearchApi { get; }
    public UserTypeApi UserTypeApi { get; }
    public UserStatusApi UserStatusApi { get; }
    public UserDataApi UserDataApi { get; }
    public EmailApi EmailApi { get; }
    public PasswordApi PasswordApi { get; }
    public PhotosApi PhotosApi { get; }
    public PeopleQuotaApi PeopleQuotaApi { get; }
    public ThemeApi ThemeApi { get; }
    public PeopleGuestsApi GuestsApi { get; }
    public ThirdPartyAccountsApi ThirdPartyAccountsApi { get; }

    // Group area — also served by the People service
    public GroupApi GroupApi { get; }
    public GroupSearchApi GroupSearchApi { get; }

    // WebApi service
    public UsersApi PortalUsersApi { get; }
    public CommonSettingsApi CommonSettingsApi { get; }
    public DocSpace.API.SDK.Api.Settings.QuotaApi SettingsQuotaApi { get; }
    public WebhooksApi WebhooksApi { get; }

    public PortalClients(PortalContext context) : base(context)
    {
        FilesHttpClient = CreateClient(ResourceNames.Files);
        PeopleHttpClient = CreateClient(ResourceNames.People);

        var filesConfig = new Configuration { BasePath = BasePathOf(ResourceNames.Files) };
        RoomsApi = new RoomsApi(FilesHttpClient, filesConfig);
        FilesApi = new FilesApi(FilesHttpClient, filesConfig);
        FoldersApi = new FoldersApi(FilesHttpClient, filesConfig);
        SharingApi = new SharingApi(FilesHttpClient, filesConfig);

        var peopleConfig = new Configuration { BasePath = BasePathOf(ResourceNames.People) };
        ProfilesApi = new ProfilesApi(PeopleHttpClient, peopleConfig);
        PeopleSearchApi = new PeopleSearchApi(PeopleHttpClient, peopleConfig);
        UserTypeApi = new UserTypeApi(PeopleHttpClient, peopleConfig);
        UserStatusApi = new UserStatusApi(PeopleHttpClient, peopleConfig);
        UserDataApi = new UserDataApi(PeopleHttpClient, peopleConfig);
        EmailApi = new EmailApi(PeopleHttpClient, peopleConfig);
        PasswordApi = new PasswordApi(PeopleHttpClient, peopleConfig);
        PhotosApi = new PhotosApi(PeopleHttpClient, peopleConfig);
        PeopleQuotaApi = new PeopleQuotaApi(PeopleHttpClient, peopleConfig);
        ThemeApi = new ThemeApi(PeopleHttpClient, peopleConfig);
        GuestsApi = new PeopleGuestsApi(PeopleHttpClient, peopleConfig);
        ThirdPartyAccountsApi = new ThirdPartyAccountsApi(PeopleHttpClient, peopleConfig);

        GroupApi = new GroupApi(PeopleHttpClient, peopleConfig);
        GroupSearchApi = new GroupSearchApi(PeopleHttpClient, peopleConfig);

        var webApiConfig = new Configuration { BasePath = BasePathOf(ResourceNames.WebApi) };
        PortalUsersApi = new UsersApi(WebApiHttpClient, webApiConfig);
        CommonSettingsApi = new CommonSettingsApi(WebApiHttpClient, webApiConfig);
        SettingsQuotaApi = new DocSpace.API.SDK.Api.Settings.QuotaApi(WebApiHttpClient, webApiConfig);

        // Webhooks live in ASC.Web.Api (hence webApiConfig, which is what decides the request URI),
        // but the tests drive them with the People client's token — keep it on that client.
        WebhooksApi = new WebhooksApi(PeopleHttpClient, webApiConfig);
    }
}
