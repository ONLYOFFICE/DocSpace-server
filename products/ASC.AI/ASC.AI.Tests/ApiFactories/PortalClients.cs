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

namespace ASC.AI.Tests.ApiFactories;

/// <summary>
/// The AI suite's per-portal clients. The public AI surface is <c>/api/2.0/ai</c>, served by the Node chat
/// service (common/ASC.AI.Chat) and covered by the typed SDK: <see cref="AiProfilesApi"/>,
/// <see cref="AiThreadsApi"/>, <see cref="AiChatApi"/>. Behind it the Node service keeps its data in ASC.AI over
/// <c>/internal/ai</c>, which the router does not publish and the SDK does not describe; <see cref="Ai"/> reaches
/// it raw, for checks of that internal contract only. Files and People go through the typed SDK.
/// </summary>
public sealed class PortalClients : PortalClientsBase
{
    public HttpClient AiHttpClient { get; }
    public HttpClient FilesHttpClient { get; }
    public HttpClient PeopleHttpClient { get; }

    /// <summary>ASC.AI's internal storage API (<c>/internal/ai</c>), the one the Node chat service calls.</summary>
    public RawApiClient Ai { get; }

    // Node chat service: the public /api/2.0/ai surface the client uses
    public HttpClient AiChatHttpClient { get; }
    public AiProfilesApi AiProfilesApi { get; }
    public AiThreadsApi AiThreadsApi { get; }
    public AiChatApi AiChatApi { get; }
    public AiAssignmentsApi AiAssignmentsApi { get; }
    public AiToolsApi AiToolsApi { get; }
    public AiPromptsApi AiPromptsApi { get; }
    public AiPreferencesApi AiPreferencesApi { get; }
    public AiSettingsApi AiSettingsApi { get; }
    public AiContextApi AiContextApi { get; }
    public AiExportApi AiExportApi { get; }
    public DocSpace.API.SDK.Api.AI.AttachmentsApi AiAttachmentsApi { get; }
    public DocSpace.API.SDK.Api.AI.EditorToolsApi AiEditorToolsApi { get; }
    public DocSpace.API.SDK.Api.AI.VectorizationApi AiVectorizationApi { get; }
    public DocSpace.API.SDK.Api.AI.WebSearchApi AiWebSearchApi { get; }

    // Files service
    public FilesApi FilesApi { get; }
    public FoldersApi FoldersApi { get; }
    public RoomsApi RoomsApi { get; }
    public OperationsApi OperationsApi { get; }
    public SharingApi SharingApi { get; }
    public FilesQuotaApi FilesQuotaApi { get; }
    public DocSpace.API.SDK.Api.Files.ThirdPartyIntegrationApi ThirdPartyIntegrationApi { get; }

    /// <summary>The project routes of a room (<c>files/rooms/{id}/model</c>), on the Files service.</summary>
    public DocSpace.API.SDK.Api.Rooms.AIApi RoomsAiApi { get; }

    // People service
    public ProfilesApi ProfilesApi { get; }

    // Web.Api: the portal-wide AI switch (settings/ai-access)
    public CommonSettingsApi CommonSettingsApi { get; }
    public SettingsQuotaApi SettingsQuotaApi { get; }

    public PortalClients(PortalContext context) : base(context)
    {
        AiHttpClient = CreateClient(ResourceNames.Ai);
        FilesHttpClient = CreateClient(ResourceNames.Files);
        PeopleHttpClient = CreateClient(ResourceNames.People);

        Ai = new RawApiClient(AiHttpClient);

        AiChatHttpClient = CreateClient(ResourceNames.AiChat);
        var aiChatConfig = new Configuration { BasePath = BasePathOf(ResourceNames.AiChat) };
        AiProfilesApi = new AiProfilesApi(AiChatHttpClient, aiChatConfig);
        AiThreadsApi = new AiThreadsApi(AiChatHttpClient, aiChatConfig);
        AiChatApi = new AiChatApi(AiChatHttpClient, aiChatConfig);
        AiAssignmentsApi = new AiAssignmentsApi(AiChatHttpClient, aiChatConfig);
        AiToolsApi = new AiToolsApi(AiChatHttpClient, aiChatConfig);
        AiPromptsApi = new AiPromptsApi(AiChatHttpClient, aiChatConfig);
        AiPreferencesApi = new AiPreferencesApi(AiChatHttpClient, aiChatConfig);
        AiSettingsApi = new AiSettingsApi(AiChatHttpClient, aiChatConfig);
        AiContextApi = new AiContextApi(AiChatHttpClient, aiChatConfig);
        AiExportApi = new AiExportApi(AiChatHttpClient, aiChatConfig);
        AiAttachmentsApi = new DocSpace.API.SDK.Api.AI.AttachmentsApi(AiChatHttpClient, aiChatConfig);
        AiEditorToolsApi = new DocSpace.API.SDK.Api.AI.EditorToolsApi(AiChatHttpClient, aiChatConfig);
        AiVectorizationApi = new DocSpace.API.SDK.Api.AI.VectorizationApi(AiChatHttpClient, aiChatConfig);
        AiWebSearchApi = new DocSpace.API.SDK.Api.AI.WebSearchApi(AiChatHttpClient, aiChatConfig);

        var filesConfig = new Configuration { BasePath = BasePathOf(ResourceNames.Files) };
        FilesApi = new FilesApi(FilesHttpClient, filesConfig);
        FoldersApi = new FoldersApi(FilesHttpClient, filesConfig);
        RoomsApi = new RoomsApi(FilesHttpClient, filesConfig);
        OperationsApi = new OperationsApi(FilesHttpClient, filesConfig);
        SharingApi = new SharingApi(FilesHttpClient, filesConfig);
        FilesQuotaApi = new FilesQuotaApi(FilesHttpClient, filesConfig);
        ThirdPartyIntegrationApi = new DocSpace.API.SDK.Api.Files.ThirdPartyIntegrationApi(FilesHttpClient, filesConfig);
        RoomsAiApi = new DocSpace.API.SDK.Api.Rooms.AIApi(FilesHttpClient, filesConfig);

        ProfilesApi = new ProfilesApi(PeopleHttpClient, new Configuration { BasePath = BasePathOf(ResourceNames.People) });

        var webApiConfig = new Configuration { BasePath = BasePathOf(ResourceNames.WebApi) };
        CommonSettingsApi = new CommonSettingsApi(WebApiHttpClient, webApiConfig);
        SettingsQuotaApi = new SettingsQuotaApi(WebApiHttpClient, webApiConfig);
    }
}
