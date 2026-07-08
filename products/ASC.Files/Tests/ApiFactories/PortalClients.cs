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

using DocSpace.API.SDK.Api.Privacyroom;

using GroupApi = DocSpace.API.SDK.Api.Group.GroupApi;
using SettingsApi = DocSpace.API.SDK.Api.Files.SettingsApi;
using QuotaApi = DocSpace.API.SDK.Api.Files.QuotaApi;

namespace ASC.Files.Tests.ApiFactories;

/// <summary>
/// A self-contained set of HTTP/API clients bound to a single portal via the <c>Origin</c> header.
/// Each test owns one instance, which makes tests fully independent and safe to run in parallel.
/// </summary>
public sealed class PortalClients : IDisposable
{
    /// <summary>The portal (tenant) alias these clients are bound to.</summary>
    public string PortalName { get; }

    /// <summary>The owner of this portal. Its Id is unique per portal.</summary>
    public User Owner { get; }

    public HttpClient FilesHttpClient { get; }
    public HttpClient PeopleHttpClient { get; }
    public HttpClient WebApiHttpClient { get; }

    // Files service
    public FoldersApi FoldersApi { get; }
    public FilesApi FilesApi { get; }
    public OperationsApi OperationsApi { get; }
    public RoomsApi RoomsApi { get; }
    public SettingsApi SettingsApi { get; }
    public QuotaApi QuotaApi { get; }
    public SharingApi SharingApi { get; }
    public PrivacyroomApi PrivacyroomApi { get; }

    // People service
    public ProfilesApi ProfilesApi { get; }
    public GroupApi GroupApi { get; }
    public UserStatusApi UserStatusApi { get; }
    public PhotosApi PhotosApi { get; }

    // WebApi service
    public AuthenticationApi AuthenticationApi { get; }
    public CommonSettingsApi CommonSettingsApi { get; }
    public UsersApi PortalUsersApi { get; }
    public DocSpace.API.SDK.Api.Settings.QuotaApi SettingsQuotaApi { get; }

    public PortalClients(Uri filesBaseAddress, Uri peopleBaseAddress, Uri webApiBaseAddress, string portalName, User owner, Func<Uri, string?, HttpClient> createClient)
    {
        PortalName = portalName;
        Owner = owner;

        var origin = $"http://{portalName}";

        // The clients are per-test (own Origin/Auth headers) but share the fixture's connection pool.
        FilesHttpClient = createClient(filesBaseAddress, origin);
        PeopleHttpClient = createClient(peopleBaseAddress, origin);
        WebApiHttpClient = createClient(webApiBaseAddress, origin);

        var filesConfig = new Configuration { BasePath = filesBaseAddress.ToString().TrimEnd('/') };
        FoldersApi = new FoldersApi(FilesHttpClient, filesConfig);
        FilesApi = new FilesApi(FilesHttpClient, filesConfig);
        OperationsApi = new OperationsApi(FilesHttpClient, filesConfig);
        RoomsApi = new RoomsApi(FilesHttpClient, filesConfig);
        SettingsApi = new SettingsApi(FilesHttpClient, filesConfig);
        QuotaApi = new QuotaApi(FilesHttpClient, filesConfig);
        SharingApi = new SharingApi(FilesHttpClient, filesConfig);
        PrivacyroomApi = new PrivacyroomApi(FilesHttpClient, filesConfig);

        var peopleConfig = new Configuration { BasePath = peopleBaseAddress.ToString().TrimEnd('/') };
        ProfilesApi = new ProfilesApi(PeopleHttpClient, peopleConfig);
        GroupApi = new GroupApi(PeopleHttpClient, peopleConfig);
        UserStatusApi = new UserStatusApi(PeopleHttpClient, peopleConfig);
        PhotosApi = new PhotosApi(PeopleHttpClient, peopleConfig);

        var webApiConfig = new Configuration { BasePath = webApiBaseAddress.ToString().TrimEnd('/') };
        AuthenticationApi = new AuthenticationApi(WebApiHttpClient, webApiConfig);
        CommonSettingsApi = new CommonSettingsApi(WebApiHttpClient, webApiConfig);
        PortalUsersApi = new UsersApi(WebApiHttpClient, webApiConfig);
        SettingsQuotaApi = new DocSpace.API.SDK.Api.Settings.QuotaApi(WebApiHttpClient, webApiConfig);

        // Associate every client with this portal's authentication endpoint so the
        // HttpClient.Authenticate(user) extension knows where to sign in.
        Initializer.RegisterAuthApi(FilesHttpClient, AuthenticationApi);
        Initializer.RegisterAuthApi(PeopleHttpClient, AuthenticationApi);
        Initializer.RegisterAuthApi(WebApiHttpClient, AuthenticationApi);
    }

    public void Dispose()
    {
        Initializer.UnregisterAuthApi(FilesHttpClient);
        Initializer.UnregisterAuthApi(PeopleHttpClient);
        Initializer.UnregisterAuthApi(WebApiHttpClient);

        FilesHttpClient.Dispose();
        PeopleHttpClient.Dispose();
        WebApiHttpClient.Dispose();
    }
}
