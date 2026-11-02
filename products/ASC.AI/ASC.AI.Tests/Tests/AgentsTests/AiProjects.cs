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

namespace ASC.AI.Tests.Tests.AgentsTests;

/// <summary>
/// The AI project, which replaced the AI agent as the place a conversation gets a model of its own: a room of type
/// <c>AiProjectRoom</c> under the AI rooms root, with one model profile (<c>GET</c> and <c>PUT
/// files/rooms/{id}/model</c>). The chat service reads that model (and the caller's membership) from the Files API
/// when a round or a thread names the project as its <c>entityId</c>. The static helpers take the clients of the
/// calling test, which signs the Files client in as whoever should act.
/// </summary>
public static class AiProjects
{
    /// <summary>Creates an AI project as whoever the Files client is signed in as; returns its room id.</summary>
    public static async Task<int> CreateAsync(RoomsApi roomsApi, string? title = null)
    {
        var room = await roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title ?? $"project-{Guid.NewGuid():N}", roomType: RoomType.AiProjectRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    /// <summary>The generated client of the project routes (<c>files/rooms/{id}/model</c>), on the Files service.</summary>
    public static DocSpace.API.SDK.Api.Rooms.AIApi ClientOf(RoomsApi roomsApi, HttpClient filesClient) =>
        new(filesClient, new Configuration { BasePath = roomsApi.GetBasePath() });

    /// <summary>Selects the model of a project.</summary>
    public static Task<AiProjectModelWrapper> SetModelAsync(DocSpace.API.SDK.Api.Rooms.AIApi client, int projectId, string profileId) =>
        client.SetProjectModelAsync(projectId, new UpdateAiProjectModelRequest(Guid.Parse(profileId)), TestContext.Current.CancellationToken);

    /// <summary>The profile id the project is bound to, in the lower-case text form the chat service uses; null when none.</summary>
    public static async Task<string?> GetModelAsync(DocSpace.API.SDK.Api.Rooms.AIApi client, int projectId)
    {
        var model = await client.GetProjectModelAsync(projectId, TestContext.Current.CancellationToken);

        return model.Response.ProfileId?.ToString("D");
    }
}
