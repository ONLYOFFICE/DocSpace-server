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

import { afterEach, describe, expect, it, vi } from "vitest";
import {
  filesApiDownload,
  filesApiDownloadBytes,
  filesApiDownloadText,
  filesApiGet,
  filesApiRequest,
  filesApiSend,
  getFileInfo,
  getFolderInfo,
  getMyDocumentsFolderId,
} from "../docspaceFilesApi.js";
import { addWebPlugin, getSelf, getSelfId } from "../docspacePeopleApi.js";

// A refused answer of the Files or People API is left with no unread body: a body nobody reads
// keeps its connection out of the pool until the response is collected (ts-ai-style.md,
// "Upstream calls"). msw reads a stubbed answer to its end by itself, so the answer here is a
// `Response` of the test's own, handed out by a stubbed `fetch`, whose `bodyUsed` says whether
// the client read or cancelled it.

const CALLS: Readonly<Record<string, () => Promise<unknown>>> = {
  filesApiGet: () => filesApiGet("rooms/7/sources"),
  filesApiSend: () => filesApiSend("PUT", "rooms/7/model", {}),
  filesApiRequest: () => filesApiRequest("POST", "7/insert", { json: {} }),
  filesApiDownload: () => filesApiDownload("http://storage/f", {}, (res) => res.text()),
  filesApiDownloadText: () => filesApiDownloadText("http://storage/f", {}, 10),
  filesApiDownloadBytes: () => filesApiDownloadBytes("http://storage/f", {}, 10),
  getFolderInfo: () => getFolderInfo("7"),
  getFileInfo: () => getFileInfo("7"),
  getMyDocumentsFolderId: () => getMyDocumentsFolderId(),
  getSelf: () => getSelf(),
  getSelfId: () => getSelfId(),
  addWebPlugin: () => addWebPlugin("plugin.zip", new Uint8Array([1])),
};

const rows = Object.keys(CALLS).flatMap((name) =>
  [403, 404, 500].map((status) => [name, status] as const),
);

describe("a refused answer of the Files and People clients", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it.each(rows)("%s leaves no unread body behind a %i", async (name, status) => {
    const refused = new Response("<html>refused</html>", { status });
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => refused),
    );

    // Thrown or read as "absent": either way the call has ended.
    await (CALLS[name] as () => Promise<unknown>)().catch((e: unknown) => e);

    expect(refused.bodyUsed).toBe(true);
  });

  it("does not let a body that fails hide the status of the refusal", async () => {
    const body = new ReadableStream<Uint8Array>({
      pull() {
        throw new Error("connection reset");
      },
    });
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => new Response(body, { status: 500 })),
    );

    await expect(filesApiGet("rooms/7/sources")).rejects.toMatchObject({ status: 500 });
  });
});
