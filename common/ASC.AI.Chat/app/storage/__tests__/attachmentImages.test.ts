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

import { beforeEach, describe, expect, it, vi } from "vitest";

const forwarded = { cookie: "asc_auth_key=1", authorization: "Bearer t", "if-none-match": "x" };
vi.mock("../../requestContext.js", () => ({
  getForwardedHeaders: () => forwarded,
  getAnalyzeEntryIds: () => undefined,
}));
vi.mock("../../../config/index.js", async (original) => ({
  ...(await original<typeof import("../../../config/index.js")>()),
  getMaxImageSize: () => 8,
}));
const aiGet = vi.fn();
const aiPost = vi.fn();
vi.mock("../httpClient.js", async (original) => {
  class AiServiceHttpError extends Error {
    constructor(public readonly status: number) {
      super(`HTTP ${status}`);
    }
  }
  return {
    // The real capped body reader: the bytes of the image go through it.
    ...(await original<typeof import("../httpClient.js")>()),
    AiServiceHttpError,
    aiService: { get: aiGet, post: aiPost },
    proxyBaseUrl: "http://portal:8092",
  };
});
vi.mock("../docspaceFilesApi.js", () => {
  class DocspaceApiHttpError extends Error {
    constructor(public readonly status: number) {
      super(`HTTP ${status}`);
    }
  }
  return {
    DocspaceApiHttpError,
    filesApiGet: vi.fn(),
    // The download client: its timeout and its refusals are tested with it, the bytes here.
    filesApiDownload: async <T>(
      href: string,
      headers: Record<string, string>,
      read: (res: Response) => Promise<T>,
    ): Promise<T> => read((await fetchMock(href, { headers })) as Response),
    getFolderInfo: vi.fn(),
    getMyDocumentsFolderId: vi.fn(),
    canTakeUpload: vi.fn(),
    resolveAiProjectEntityId: vi.fn(async () => undefined),
  };
});
vi.mock("../threadFiles.js", () => ({ uploadThreadArtifact: vi.fn() }));
vi.mock("../projectAttachments.js", () => ({ copyManyIntoProject: vi.fn() }));

const { HttpAttachmentsStorage } = await import("../attachmentsStorage.js");
const { DocspaceApiHttpError, resolveAiProjectEntityId } = await import("../docspaceFilesApi.js");
const { copyManyIntoProject } = await import("../projectAttachments.js");

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
  vi.mocked(copyManyIntoProject).mockReset();
  fetchMock.mockReset().mockImplementation(async () => image(PNG));
});

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

describe("a file is saved from a folder that cannot be read", () => {
  const saved = [{ id: "a1", title: "Report.docx", kind: "File", entryId: "40" }];
  const save = () =>
    new HttpAttachmentsStorage().createMany([
      { kind: "file", title: "Report.docx", path: "40", entityId: "12" },
    ]);

  it.each([
    ["the caller may not open it (403)", new DocspaceApiHttpError(403, "", "url")],
    ["the Files API is down (503)", new DocspaceApiHttpError(503, "", "url")],
    ["the read times out", new DOMException("timed out", "TimeoutError")],
  ])("attached as it is, without a copy, when %s", async (_case, failure) => {
    vi.mocked(resolveAiProjectEntityId).mockRejectedValueOnce(failure);
    aiPost.mockResolvedValue(saved);

    const [attachment] = await save();

    expect(attachment?.id).toBe("a1");
    expect(copyManyIntoProject).not.toHaveBeenCalled();
    expect(aiPost.mock.calls[0]?.[1]).toMatchObject({ entryIds: ["40"] });
  });

  it("and copied into the project when the folder is one", async () => {
    vi.mocked(resolveAiProjectEntityId).mockResolvedValueOnce("12");
    vi.mocked(copyManyIntoProject).mockResolvedValueOnce(["77"]);
    aiPost.mockResolvedValue([{ ...saved[0], entryId: "77" }]);

    await save();

    expect(copyManyIntoProject).toHaveBeenCalledWith("12", ["40"]);
    expect(aiPost.mock.calls[0]?.[1]).toMatchObject({ entryIds: ["77"] });
  });
});

describe("findMissingIds", () => {
  it("names the ids the backend does not hold and downloads no image for it", async () => {
    aiPost.mockResolvedValue([dto("a1", "/storage/files/1/pic.png?auth=sig")]);

    expect(await new HttpAttachmentsStorage().findMissingIds(["a1", "a2"])).toEqual(["a2"]);
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
