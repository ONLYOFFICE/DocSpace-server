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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

// `sandbox_save_file` with the document agent's save run for real: where a saved file goes, and
// what the model is told about it. The gateway, the People API, the Files API and the AI service
// are stubbed at the network.

import { beforeEach, afterEach, describe, expect, it, vi } from "vitest";

import { fileDto, folderDto } from "../../__tests__/fixtures/filesApi.js";
import { selfDto } from "../../__tests__/fixtures/peopleApi.js";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { fakeGateway } from "../../__tests__/helpers/sandboxGateway.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { setRoundThreadId } from "../../requestContext.js";
import { sandboxTools } from "../sandboxTools.js";

const upstream = useUpstream();

const THREAD = "0b9f2c66-6a0e-4a7e-9f0e-1c2d3e4f5a6b";
// POST internal/ai/threads/{id}/artifacts (ThreadArtifactsController.UploadAsync).
const ARTIFACTS = "*/internal/ai/threads/:threadId/artifacts";
// POST api/2.0/files/{folderId}/insert (UploadController.InsertFile).
const INSERT = "*/api/2.0/files/:folderId/insert";

/** The requests made to the portal's services, without the gateway's and the People API's. */
const saves = (): string[] =>
  upstream.seen
    .filter((request) => /\/artifacts$|\/insert$|\/files\/(folder\/|@my)/.test(request.path))
    .map((request) => `${request.method} ${request.path.replace(/\?.*$/, "")}`);

const save = (entityId?: string): Promise<unknown> =>
  inRequestContext({ authorization: "Bearer user" }, () => {
    setRoundThreadId(THREAD);
    return sandboxTools.callTool(
      "sandbox_save_file",
      { path: "/workspace/out/report.csv" },
      entityId,
    );
  });

beforeEach(() => {
  vi.stubEnv("AI__SANDBOX__URL", "http://gateway:9090");
  vi.stubEnv("AI__SANDBOX__TOKEN", "gw-secret");
  fakeGateway(upstream).files.set("/workspace/out/report.csv", Buffer.from("a,b\n"));
  upstream.use(
    // GET api/2.0/people/@self (PeopleController.SelfAsync).
    http.get("*/api/2.0/people/@self", () => HttpResponse.json(selfDto())),
    // GET api/2.0/files/folder/{folderId} (FoldersController.GetFolderInfo): a room the caller
    // may create in.
    http.get("*/api/2.0/files/folder/30", () => HttpResponse.json(folderDto())),
  );
});

afterEach(() => {
  vi.unstubAllEnvs();
});

describe("where sandbox_save_file saves a file", () => {
  it("keeps it among the artifacts of a conversation of an AI project, and says so", async () => {
    upstream.use(
      http.post(ARTIFACTS, () => HttpResponse.json(fileDto({ id: 950, title: "report.csv" }))),
    );

    expect(await save("30")).toBe(
      JSON.stringify({
        fileId: "950",
        title: "report.csv",
        bytes: 4,
        where: `among the artifacts of thread ${THREAD}`,
      }),
    );
    expect(saves()).toEqual([
      "GET /api/2.0/files/folder/30",
      `POST /internal/ai/threads/${THREAD}/artifacts`,
    ]);
  });

  it("puts it into the current folder when the conversation takes no artifacts", async () => {
    upstream.use(
      http.post(ARTIFACTS, () => docspaceError(400)),
      http.post(INSERT, () => HttpResponse.json(fileDto({ id: 901, title: "report.csv" }))),
    );

    expect(await save("30")).toBe(
      JSON.stringify({ fileId: "901", title: "report.csv", bytes: 4, where: "into folder 30" }),
    );
    expect(saves().at(-1)).toBe("POST /api/2.0/files/30/insert");
  });

  it("puts it into My documents when the round names no folder", async () => {
    upstream.use(
      // GET api/2.0/files/@my (FoldersController.GetMyFolder): only `current.id` is read.
      http.get("*/api/2.0/files/@my", () => docspace({ current: { id: 12 } })),
      http.post(ARTIFACTS, () => docspaceError(400)),
      http.post(INSERT, () => HttpResponse.json(fileDto({ id: 902, title: "report.csv" }))),
    );

    expect(await save()).toBe(
      JSON.stringify({ fileId: "902", title: "report.csv", bytes: 4, where: "into folder 12" }),
    );
    expect(saves().at(-1)).toBe("POST /api/2.0/files/12/insert");
  });

  it("says nothing was saved when the Files API refuses the upload", async () => {
    upstream.use(
      http.post(ARTIFACTS, () => docspaceError(400)),
      http.post(INSERT, () => docspaceError(403)),
    );

    expect(await save("30")).toBe(
      "The workspace refused to save report.csv: it answered 403. No file was saved.",
    );
  });

  it("says the file may have been saved when the AI service fails the artifact", async () => {
    upstream.use(http.post(ARTIFACTS, () => docspaceError(500, "MARKER of the body")));

    const answer = await save("30");

    expect(answer).toBe(
      "The workspace's answer to saving report.csv could not be read: the file may or may not have been saved. Look for it before saving again.",
    );
    expect(saves().at(-1)).toBe(`POST /internal/ai/threads/${THREAD}/artifacts`);
  });
});
