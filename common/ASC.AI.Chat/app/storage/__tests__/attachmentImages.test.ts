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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
const forwarded = { cookie: "asc_auth_key=1", authorization: "Bearer t", "if-none-match": "x" };
vi.mock("../../requestContext.js", () => ({
  getForwardedHeaders: () => forwarded,
  getAnalyzeEntryIds: () => undefined,
}));
vi.mock("../../../config/index.js", () => ({ getMaxImageSize: () => 8 }));
const cancel = vi.fn();
const aiGet = vi.fn();
const aiPost = vi.fn();
vi.mock("../httpClient.js", () => {
  class AiServiceHttpError extends Error {
    constructor(public readonly status: number) {
      super(`HTTP ${status}`);
    }
  }
  return {
    AiServiceHttpError,
    aiService: { get: aiGet, post: aiPost },
    proxyBaseUrl: "http://portal:8092",
    withTimeout: () => ({ signal: new AbortController().signal, cancel }),
  };
});
vi.mock("../docspaceFilesApi.js", () => {
  class DocspaceApiHttpError extends Error {}
  return {
    DocspaceApiHttpError,
    filesApiGet: vi.fn(),
    getFolderInfo: vi.fn(),
    getMyDocumentsFolderId: vi.fn(),
    canTakeUpload: vi.fn(),
    resolveAiProjectEntityId: vi.fn(async () => undefined),
  };
});
vi.mock("../threadFiles.js", () => ({ uploadThreadArtifact: vi.fn() }));
vi.mock("../projectAttachments.js", () => ({ copyManyIntoProject: vi.fn() }));

const { HttpAttachmentsStorage } = await import("../attachmentsStorage.js");

const PNG = [0x89, 0x50, 0x4e, 0x47];
const fetchMock = vi.fn();
const image = (bytes: number[]) =>
  new Response(new Uint8Array(bytes), { headers: { "content-type": "application/octet-stream" } });
const dto = (id: string, dataUrl: string) => ({
  id,
  title: "Pic.png",
  kind: "Image",
  entryId: "40",
  dataUrl,
});

beforeEach(() => {
  aiGet.mockReset();
  aiPost.mockReset();
  cancel.mockReset();
  fetchMock.mockReset().mockImplementation(async () => image(PNG));
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => vi.unstubAllGlobals());

const headersOf = (call: number) =>
  (fetchMock.mock.calls[call]?.[1] as { headers: Record<string, string> }).headers;

describe("an image attachment is inlined", () => {
  it("from the portal with the caller's session, the conditional headers left out", async () => {
    aiGet.mockResolvedValue(dto("a1", "/storage/files/1/pic.png?auth=sig"));

    const read = await new HttpAttachmentsStorage().readById("a1");

    expect(fetchMock.mock.calls[0]?.[0]).toBe(
      "http://portal:8092/storage/files/1/pic.png?auth=sig",
    );
    expect(headersOf(0)).toEqual({ cookie: "asc_auth_key=1", authorization: "Bearer t" });
    expect(read?.base64).toBe(`data:image/png;base64,${Buffer.from(PNG).toString("base64")}`);
    expect(cancel).toHaveBeenCalled();
  });

  // S3, a CDN: the address is signed in its query, and the host is not ours.
  it.each([
    "https://bucket.s3.amazonaws.com/files/pic.png?X-Amz-Signature=sig",
    "//elsewhere.example/pic.png",
  ])("from %s without the caller's cookie or token", async (url) => {
    aiGet.mockResolvedValue(dto("a1", url));

    const read = await new HttpAttachmentsStorage().readById("a1");

    expect(new URL(String(fetchMock.mock.calls[0]?.[0])).host).not.toBe("portal:8092");
    expect(headersOf(0)).toEqual({});
    expect(read?.base64).toMatch(/^data:image\/png;base64,/);
  });

  it("not at all when it is over the size a chat takes, and the address stays as it was", async () => {
    aiGet.mockResolvedValue(dto("a1", "/storage/files/1/big.png?auth=sig"));
    fetchMock.mockImplementation(async () => image([...PNG, 1, 2, 3, 4, 5]));

    const read = await new HttpAttachmentsStorage().readById("a1");

    expect(read?.base64).toBe("/storage/files/1/big.png?auth=sig");
  });

  it("once for a record named twice in one read", async () => {
    aiPost.mockResolvedValue([dto("a1", "/storage/files/1/pic.png?auth=sig")]);

    const read = await new HttpAttachmentsStorage().readManyByIds(["a1", "a1"]);

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(read[0]).toBe(read[1]);
    expect(read[0]?.base64).toMatch(/^data:image\/png;base64,/);
  });
});
