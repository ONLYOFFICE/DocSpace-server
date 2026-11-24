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
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import {
  buildLocationLines,
  CONTEXT_FOLDERS_LIMIT,
  resolveContextFolders,
  resolveLocation,
} from "../contextFolders.js";
import { inRound, scopeOf, snapshotOf } from "../../__tests__/fixtures/chatContext.js";

// GET api/2.0/files/folder/{id}: FoldersController.GetFolderInfo (products/ASC.Files), answering
// `FolderDto` (products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs) in the Files API envelope.
const upstream = useUpstream();

const folderDto = (id: number, overrides: Record<string, unknown> = {}) => ({
  id,
  title: `Folder ${id}`,
  parentId: 5,
  rootFolderType: 14,
  security: { Read: true, Create: true },
  ...overrides,
});

function folders(answers: Record<string, () => Response>) {
  upstream.use(
    http.get("*/api/2.0/files/folder/:id", ({ params }) => {
      const answer = answers[String(params["id"])];
      return answer ? answer() : new HttpResponse(null, { status: 404 });
    }),
  );
}

const ok = (dto: unknown) => () => HttpResponse.json({ response: dto });
const status = (code: number) => () => new HttpResponse(null, { status: code });

describe("resolveContextFolders over the Files API", () => {
  it("describes each folder from its FolderDto, in the order asked", async () => {
    folders({
      "12": ok(folderDto(12, { title: "Contracts", roomType: 2, security: { create: false } })),
      "45": ok(folderDto(45, { security: undefined })),
    });

    await expect(resolveContextFolders(["12", "45"])).resolves.toEqual([
      { id: "12", title: "Contracts", kind: "room", canCreate: false },
      { id: "45", title: "Folder 45", kind: "folder" },
    ]);
    expect(upstream.seen.map((r) => r.path).sort()).toEqual([
      "/api/2.0/files/folder/12",
      "/api/2.0/files/folder/45",
    ]);
  });

  it("leaves out a folder that does not exist without a warning", async () => {
    folders({ "1": ok(folderDto(1)) });
    await expect(resolveContextFolders(["404", "1"])).resolves.toEqual([
      { id: "1", title: "Folder 1", kind: "folder", canCreate: true },
    ]);
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("leaves out a folder the caller cannot read, with a warning", async () => {
    folders({ "2": status(403) });
    await expect(resolveContextFolders(["2"])).resolves.toEqual([]);
    expect(vi.mocked(logger.warn).mock.calls).toEqual([
      [
        expect.stringMatching(
          /^context folders: folder 2 not readable by the caller, left out: DocSpace API 403 /,
        ),
      ],
    ]);
  });

  it("rejects when a folder's read fails with a 500, instead of leaving it out", async () => {
    folders({ "3": status(500), "1": ok(folderDto(1)) });
    await expect(resolveContextFolders(["3", "1"])).rejects.toMatchObject({ status: 500 });
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("rejects when a folder's read times out, instead of leaving it out", async () => {
    vi.useFakeTimers();
    try {
      upstream.use(
        // The Files service never answers: only the client's own timeout ends the read.
        http.get("*/api/2.0/files/folder/:id", () => new Promise<Response>(() => undefined)),
      );
      const read = resolveContextFolders(["3"]);
      const outcome = expect(read).rejects.toThrow(/upstream timeout/);
      await vi.advanceTimersByTimeAsync(60 * 60 * 1000);
      await outcome;
      expect(logger.warn).not.toHaveBeenCalled();
    } finally {
      vi.useRealTimers();
    }
  });

  it("reads at most the limit of folders", async () => {
    folders({});
    const ids = Array.from({ length: CONTEXT_FOLDERS_LIMIT + 3 }, (_, i) => String(i + 1));
    await resolveContextFolders(ids);
    expect(upstream.seen).toHaveLength(CONTEXT_FOLDERS_LIMIT);
  });

  it("serves the round's folders from the snapshot without a request", async () => {
    const snapshot = snapshotOf({
      entity: scopeOf({ entityId: "42", folder: { title: "Launch plan", folderType: 40 } }),
    });
    await expect(inRound(snapshot, () => resolveContextFolders(["42"]))).resolves.toEqual([
      { id: "42", title: "Launch plan", kind: "folder" },
    ]);
    expect(upstream.seen).toEqual([]);
  });

  it("reads a folder asked twice in one request once", async () => {
    folders({ "7": ok(folderDto(7)) });
    await inRequestContext({}, async () => {
      await resolveContextFolders(["7"]);
      await resolveContextFolders(["7"]);
    });
    expect(upstream.seen).toHaveLength(1);
  });
});

describe("resolveLocation over the Files API", () => {
  it("names the location from its FolderDto", async () => {
    folders({ "9": ok(folderDto(9, { title: "Reports", roomType: 6 })) });
    await expect(resolveLocation("9")).resolves.toEqual({
      id: "9",
      title: "Reports",
      kind: "room",
      canCreate: true,
    });
  });

  it.each([
    ["does not exist", status(404)],
    ["is unparseable", ok(null)],
  ])("keeps a location that %s by its id alone", async (_name, answer) => {
    folders({ "9": answer });
    await expect(resolveLocation("9")).resolves.toEqual({ id: "9" });
  });

  it.each([403, 500])(
    "keeps a location whose read answers %i by its id, with a warning",
    async (code) => {
      folders({ "9": status(code) });
      await expect(resolveLocation("9")).resolves.toEqual({ id: "9" });
      expect(String(vi.mocked(logger.warn).mock.calls[0]?.[0])).toMatch(
        new RegExp(
          `^context: location 9 not readable by the caller, described by its id: DocSpace API ${code} `,
        ),
      );
    },
  );
});

describe("buildLocationLines", () => {
  it("calls a named location of unknown kind a folder or room", () => {
    expect(buildLocationLines({ id: "9", title: "Reports" })[0]).toBe(
      "- You are currently in the folder or room \"Reports\" (id 9) — this is the user's current location. Any tool you call reads or modifies data on the user's behalf and with their permissions — never assume access beyond that.",
    );
  });
});
