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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
vi.mock("../../requestContext.js", () => ({
  getForwardedHeaders: () => ({ cookie: "asc_auth_key=1" }),
}));
vi.mock("../httpClient.js", () => {
  class AiServiceHttpError extends Error {
    constructor(public readonly status: number) {
      super(`AI service ${status}`);
    }
  }
  return {
    AiServiceHttpError,
    aiServiceBaseUrl: "http://ai",
    withTimeout: () => ({ signal: undefined, cancel: () => {} }),
  };
});
vi.mock("../docspaceFilesApi.js", () => {
  class DocspaceApiHttpError extends Error {
    constructor(public readonly status: number) {
      super(`DocSpace API ${status}`);
    }
  }
  return { DocspaceApiHttpError, filesApiGet: vi.fn() };
});

const { DocspaceApiHttpError, filesApiGet } = await import("../docspaceFilesApi.js");
const { collectUserAttachmentRefs, entryIdOf, listThreadAttachments, uploadThreadArtifact } =
  await import("../threadFiles.js");

const extractRefs = (message: unknown) => (message as { refs?: string[] }).refs ?? [];

const fetchMock = vi.fn();

beforeEach(() => {
  vi.mocked(filesApiGet).mockReset();
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => vi.unstubAllGlobals());

const answer = (status: number, body: unknown = {}) => ({
  ok: status >= 200 && status < 300,
  status,
  statusText: String(status),
  json: async () => body,
  text: async () => "",
});

describe("uploadThreadArtifact", () => {
  it("posts the file to the thread's artifacts with the caller's credentials", async () => {
    fetchMock.mockResolvedValue(answer(200, { response: { id: 55, title: "image (1).png" } }));
    const file = await uploadThreadArtifact(
      "t-1",
      "image.png",
      new Uint8Array([1, 2]),
      "image/png",
    );
    expect(file).toEqual({ id: "55", title: "image (1).png" });
    const [url, init] = fetchMock.mock.calls[0]!;
    expect(url).toBe("http://ai/internal/ai/threads/t-1/artifacts");
    expect(init.method).toBe("POST");
    expect(init.headers).toEqual({ cookie: "asc_auth_key=1" });
    expect((init.body as FormData).get("title")).toBe("image.png");
    expect((init.body as FormData).get("file")).toBeInstanceOf(Blob);
  });

  it.each([400, 404])(
    "answers nothing for a thread that takes no artifacts (%s)",
    async (status) => {
      fetchMock.mockResolvedValue(answer(status));
      expect(
        await uploadThreadArtifact("t-1", "image.png", new Uint8Array([1]), "image/png"),
      ).toBeUndefined();
    },
  );

  it("throws on any other refusal", async () => {
    fetchMock.mockResolvedValue(answer(500));
    await expect(
      uploadThreadArtifact("t-1", "image.png", new Uint8Array([1]), "image/png"),
    ).rejects.toMatchObject({
      status: 500,
    });
  });
});

describe("the attachments of a thread", () => {
  it("collects the references of the user's messages, each once, in order", () => {
    expect(
      collectUserAttachmentRefs(
        [
          { role: "user", refs: ["a", "b"] },
          { role: "assistant", refs: ["tool-image"] },
          { role: "user", refs: ["b", "c"] },
        ],
        extractRefs,
      ),
    ).toEqual(["a", "b", "c"]);
  });

  it("reads the file id from the head of an attachment's path", () => {
    expect(entryIdOf({ path: "12/Report.docx" })).toBe("12");
    expect(entryIdOf({ path: "12" })).toBe("12");
    expect(entryIdOf({})).toBeUndefined();
    expect(entryIdOf(null)).toBeUndefined();
  });

  it("answers the files as DocSpace describes them, without the gone and the unreadable", async () => {
    vi.mocked(filesApiGet).mockImplementation(async (path) => {
      if (path === "file/2") {
        throw new DocspaceApiHttpError(404, "Not Found", "url");
      }
      if (path === "file/3") {
        throw new DocspaceApiHttpError(403, "Forbidden", "url");
      }
      return { id: Number(path.split("/")[1]), title: "File" };
    });
    const files = await listThreadAttachments("t-1", {
      extractRefs,
      messages: { readByThread: async () => [{ role: "user", refs: ["a", "b", "c", "d", "e"] }] },
      attachments: {
        readManyByIds: async () => [
          { path: "1/One.docx" },
          { path: "2/Gone.docx" },
          { path: "3/Closed.docx" },
          { path: "1/One.docx" },
          null,
        ],
      },
    });
    expect(files).toEqual([{ id: 1, title: "File" }]);
    expect(filesApiGet).toHaveBeenCalledTimes(3);
  });

  it("fails on a Files error that is not about the one file", async () => {
    vi.mocked(filesApiGet).mockRejectedValue(new DocspaceApiHttpError(500, "Error", "url"));
    await expect(
      listThreadAttachments("t-1", {
        extractRefs,
        messages: { readByThread: async () => [{ role: "user", refs: ["a"] }] },
        attachments: { readManyByIds: async () => [{ path: "1/One.docx" }] },
      }),
    ).rejects.toMatchObject({ status: 500 });
  });
});
