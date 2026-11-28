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

import { describe, expect, it, vi } from "vitest";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { refreshContextRoomSkills } from "../contextRoomSkills.js";

// GET api/2.0/files/rooms/skills: products/ASC.Files/Server/Api/AiHubController.cs GetRoomsSkills,
// answering `AiSkillsDto` (ApiModels/ResponseDto/AiSkillsDto.cs) in the Files API envelope.
const upstream = useUpstream();

function catalog(rooms: unknown[]) {
  upstream.use(
    http.get("*/api/2.0/files/rooms/skills", () => HttpResponse.json({ response: { rooms } })),
  );
}

describe("refreshContextRoomSkills", () => {
  it.each([
    ["null", null],
    ["a string", "body"],
    ["a body without actionArgs", {}],
    ["actionArgs that are not an object", { actionArgs: "x" }],
    ["no contextRooms", { actionArgs: {} }],
    ["contextRooms that are not a list", { actionArgs: { contextRooms: { id: "7" } } }],
    ["an empty contextRooms", { actionArgs: { contextRooms: [] } }],
    [
      "rooms of another cloud only",
      { actionArgs: { contextRooms: [{ cloud: "drive", id: "7" }] } },
    ],
    [
      "rooms without a usable id",
      { actionArgs: { contextRooms: [{ cloud: "docspace" }, { id: "" }, { id: 7 }, "room"] } },
    ],
  ])("leaves %s alone without a request", async (_name, body) => {
    const before = JSON.stringify(body);
    await refreshContextRoomSkills(body);
    expect(JSON.stringify(body)).toBe(before);
    expect(upstream.seen).toEqual([]);
  });

  it("replaces the skills of every DocSpace room with the catalog's and leaves the rest as sent", async () => {
    catalog([
      {
        id: 7,
        title: "Brand voice",
        logo: null,
        skills: [{ id: "5001", name: "tone", description: "Tone." }],
      },
    ]);
    const body = {
      actionArgs: {
        contextRooms: [
          { cloud: "docspace", id: "7", title: "kept", skills: [{ id: "old" }] },
          { id: "8", skills: [{ id: "sent" }] },
          { cloud: "drive", id: "7", skills: [] },
          { cloud: "docspace" },
          "room",
        ],
        other: 1,
      },
    };

    await refreshContextRoomSkills(body);

    expect(body).toEqual({
      actionArgs: {
        contextRooms: [
          {
            cloud: "docspace",
            id: "7",
            title: "kept",
            skills: [{ id: "5001", name: "tone", description: "Tone." }],
          },
          { id: "8", skills: [{ id: "sent" }] },
          { cloud: "drive", id: "7", skills: [] },
          { cloud: "docspace" },
        ],
        other: 1,
      },
    });
    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/rooms/skills?roomIds=7&roomIds=8");
  });

  it("keeps the skills sent when the catalog answers 500, with a warning", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms/skills", () => new HttpResponse(null, { status: 500 })),
    );
    const rooms = [{ id: "7", skills: [{ id: "sent" }] }];
    const body = { actionArgs: { contextRooms: rooms } };

    await refreshContextRoomSkills(body);

    expect(body.actionArgs.contextRooms).toBe(rooms);
    expect(vi.mocked(logger.warn).mock.calls).toEqual([
      [
        expect.stringMatching(
          /^context rooms 7: skills not refreshed, the round keeps the ones sent: DocSpace API 500 /,
        ),
      ],
    ]);
  });
});
