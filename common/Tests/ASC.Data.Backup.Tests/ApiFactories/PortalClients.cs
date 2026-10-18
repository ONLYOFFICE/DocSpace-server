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


namespace ASC.Data.Backup.Tests.ApiFactories;

/// <summary>
/// The backup suite's per-portal API clients.
/// </summary>
public sealed class PortalClients : PortalClientsBase
{
    public HttpClient BackupHttpClient { get; }
    public HttpClient PeopleHttpClient { get; }
    public HttpClient FilesHttpClient { get; }

    // Backup service - raw access for the bodies the generated DTOs cannot carry
    public RawApiClient Backup { get; }
    public BackupApi BackupApi { get; }

    // People service - members a permission test invites
    public ProfilesApi ProfilesApi { get; }

    // Files service - the rooms, folders and third-party storages a backup is written to
    public RoomsApi RoomsApi { get; }
    public FoldersApi FoldersApi { get; }
    public ThirdPartyIntegrationApi ThirdPartyIntegrationApi { get; }

    // Web.Api service - the wallet the paid backups are charged to, and the consumer keys of S3
    public PaymentApi PaymentApi { get; }
    public SettingsAuthorizationApi SettingsAuthorizationApi { get; }

    public PortalClients(PortalContext context) : base(context)
    {
        BackupHttpClient = CreateClient(ResourceNames.Backup);
        Backup = new RawApiClient(BackupHttpClient);
        BackupApi = new BackupApi(BackupHttpClient, new Configuration { BasePath = BasePathOf(ResourceNames.Backup) });

        PeopleHttpClient = CreateClient(ResourceNames.People);
        ProfilesApi = new ProfilesApi(PeopleHttpClient, new Configuration { BasePath = BasePathOf(ResourceNames.People) });

        FilesHttpClient = CreateClient(ResourceNames.Files);
        var filesConfig = new Configuration { BasePath = BasePathOf(ResourceNames.Files) };
        RoomsApi = new RoomsApi(FilesHttpClient, filesConfig);
        FoldersApi = new FoldersApi(FilesHttpClient, filesConfig);
        ThirdPartyIntegrationApi = new ThirdPartyIntegrationApi(FilesHttpClient, filesConfig);

        var webApiConfig = new Configuration { BasePath = BasePathOf(ResourceNames.WebApi) };
        PaymentApi = new PaymentApi(WebApiHttpClient, webApiConfig);
        SettingsAuthorizationApi = new SettingsAuthorizationApi(WebApiHttpClient, webApiConfig);
    }
}
