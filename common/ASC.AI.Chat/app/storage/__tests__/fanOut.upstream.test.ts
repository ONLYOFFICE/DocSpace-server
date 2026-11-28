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

import { describe, expect, it } from "vitest";
import {
  delay,
  docspace,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { fileDto, folderDto, inRequest } from "../../__tests__/fixtures/filesApi.js";
import { HttpAttachmentsStorage } from "../attachmentsStorage.js";
import { resolveContextFolders } from "../contextFolders.js";
import { aiServiceBaseUrl, proxyBaseUrl } from "../httpClient.js";
import { UPSTREAM_CONCURRENCY } from "../pooled.js";
import { copyManyIntoProject } from "../projectAttachments.js";
import { HttpPromptsStorage } from "../promptsStorage.js";

// The calls one request makes in a loop are a few in flight at a time (ts-ai-style.md,
// "Upstream calls"): each place that loops over the items of a request, with an upstream that
// counts the requests it is holding.

const upstream = useUpstream();
const AI = `${aiServiceBaseUrl}/internal/ai`;
const FILES = `${proxyBaseUrl}/api/2.0/files`;
const PNG = new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

/** Holds every answer for a moment and keeps the most requests it held at once. */
function meter(): { most: number; total: number; hold: <T>(answer: () => T) => Promise<T> } {
  const state = {
    most: 0,
    total: 0,
    hold: async <T>(answer: () => T): Promise<T> => {
      inFlight += 1;
      state.total += 1;
      state.most = Math.max(state.most, inFlight);
      await delay(2);
      inFlight -= 1;
      return answer();
    },
  };
  let inFlight = 0;
  return state;
}

/** `PromptDto` (products/ASC.AI/Server/Models/ResponseDto/PromptDto.cs). */
const promptDto = (id: string): Record<string, unknown> => ({
  id,
  name: id,
  text: "t",
  folderId: "f",
  createdAt: 1_700_000_000_000,
  updatedAt: 1_700_000_005_000,
});

const ids = (count: number): string[] => Array.from({ length: count }, (_, i) => String(i + 1));

describe("the calls of one request, a few at a time", () => {
  it("creates the prompts of a bundle", async () => {
    const calls = meter();
    // PromptStorageController: `POST internal/ai/prompts`.
    upstream.use(
      http.post(`${AI}/prompts`, async ({ request }) => {
        const body = (await request.json()) as { name: string };
        return calls.hold(() => docspace(promptDto(body.name)));
      }),
    );

    const created = await new HttpPromptsStorage().createMany(
      ids(12).map((name) => ({ name, text: "t" })),
    );

    expect(created.map((p) => p.name)).toEqual(ids(12));
    expect(calls.total).toBe(12);
    expect(calls.most).toBe(UPSTREAM_CONCURRENCY);
  });

  it("deletes the prompts of a folder", async () => {
    const calls = meter();
    // PromptStorageController: `GET internal/ai/prompt-folders/{id}/prompts`, `DELETE prompts/{id}`.
    upstream.use(
      http.get(`${AI}/prompt-folders/f/prompts`, () => docspace(ids(12).map(promptDto))),
      http.delete(`${AI}/prompts/:id`, () =>
        calls.hold(() => new HttpResponse(null, { status: 204 })),
      ),
    );

    await new HttpPromptsStorage().deleteByFolder("f");

    expect(calls.total).toBe(12);
    expect(calls.most).toBe(UPSTREAM_CONCURRENCY);
  });

  it("reads the context folders", async () => {
    const calls = meter();
    // FoldersController.GetFolderInfo: `GET api/2.0/files/folder/{id}`.
    upstream.use(
      http.get(`${FILES}/folder/:id`, ({ params }) =>
        calls.hold(() => HttpResponse.json(folderDto({ id: Number(params["id"]) }))),
      ),
    );

    const folders = await inRequest(() => resolveContextFolders(ids(20)));

    expect(folders.map((f) => f.id)).toEqual(ids(20));
    expect(calls.most).toBe(UPSTREAM_CONCURRENCY);
  });

  it("reads the files to copy into a project, each once", async () => {
    const calls = meter();
    // FilesController.GetFileInfo: `GET api/2.0/files/file/{id}`; a file that already lies in
    // the project is attached where it is.
    upstream.use(
      http.get(`${FILES}/file/:id`, ({ params }) =>
        calls.hold(() => HttpResponse.json(fileDto({ id: Number(params["id"]), folderId: 7 }))),
      ),
    );

    const attached = await inRequest(() => copyManyIntoProject("7", [...ids(12), "1", "2"]));

    expect(attached).toEqual([...ids(12), "1", "2"]);
    expect(calls.total).toBe(12);
    expect(calls.most).toBe(UPSTREAM_CONCURRENCY);
  });

  it("downloads the images of a history", async () => {
    const calls = meter();
    // AttachmentsStorageController: `POST internal/ai/attachments/read`; the images are on the
    // portal's storage handler.
    upstream.use(
      http.post(`${AI}/attachments/read`, () =>
        docspace(
          ids(12).map((id) => ({
            id,
            kind: "image",
            type: 0,
            title: `${id}.png`,
            dataUrl: `/storage/files/${id}/pic.png?auth=sig`,
            entryId: id,
            createdAt: 1763900000000,
            canAnalyze: false,
          })),
        ),
      ),
      http.get(`${proxyBaseUrl}/storage/files/:id/pic.png`, () =>
        calls.hold(() => HttpResponse.arrayBuffer(PNG.buffer)),
      ),
    );

    const read = await inRequest(() => new HttpAttachmentsStorage().readManyByIds(ids(12)));

    expect(read.every((a) => a?.base64?.startsWith("data:image/png;base64,"))).toBe(true);
    expect(calls.total).toBe(12);
    expect(calls.most).toBe(UPSTREAM_CONCURRENCY);
  });
});
