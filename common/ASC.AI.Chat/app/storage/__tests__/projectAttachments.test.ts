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

vi.mock("../../requestContext.js", () => ({
  getForwardedHeaders: () => ({ cookie: "asc_auth_key=1" }),
}));
vi.mock("../httpClient.js", () => {
  class AiServiceHttpError extends Error {
    constructor(public readonly status: number) {
      super(`HTTP ${status}`);
    }
  }
  return {
    AiServiceHttpError,
    proxyBaseUrl: "http://portal",
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
vi.mock("../docspaceFileTransfer.js", () => ({ mediaTypeOf: () => "application/docx" }));
vi.mock("../roomSkills.js", () => {
  class FileTooLargeError extends Error {}
  return { FileTooLargeError, readFileBytes: vi.fn() };
});

const { filesApiGet } = await import("../docspaceFilesApi.js");
const { FileTooLargeError, readFileBytes } = await import("../roomSkills.js");
const {
  copyIntoProject,
  copyManyIntoProject,
  MAX_CONCURRENT_COPIES,
  MAX_COPY_BYTES,
  uploadInChunks,
} = await import("../projectAttachments.js");

const fetchMock = vi.fn();

const answer = (status: number, response: unknown = {}) => ({
  ok: status >= 200 && status < 300,
  status,
  statusText: String(status),
  json: async () => ({ response }),
  text: async () => "",
});

/** The portal: its settings, the file's description, and the upload session. */
function portal(file: Record<string, unknown>, chunkUploadSize = 4): void {
  vi.mocked(filesApiGet).mockImplementation(async (path) =>
    path === "settings" ? { chunkUploadSize } : file,
  );
  fetchMock.mockImplementation(async (url: string) =>
    url.endsWith("/session")
      ? answer(200, { id: "s1" })
      : answer(200, { file: { id: 77, title: "Report.docx" } }),
  );
}

beforeEach(() => {
  vi.mocked(filesApiGet).mockReset();
  vi.mocked(readFileBytes)
    .mockReset()
    .mockResolvedValue(new Uint8Array([1, 2, 3, 4, 5, 6]));
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => vi.unstubAllGlobals());

describe("uploadInChunks", () => {
  it("opens a session in the folder and sends the chunks in order", async () => {
    portal({});
    const id = await uploadInChunks(
      "12",
      "Report.docx",
      new Uint8Array([1, 2, 3, 4, 5, 6]),
      "application/docx",
    );
    expect(id).toBe("77");
    const [session, first, second] = fetchMock.mock.calls;
    expect(session![0]).toBe("http://portal/api/2.0/files/12/session");
    expect(JSON.parse(session![1].body)).toEqual({
      fileName: "Report.docx",
      fileSize: 6,
      relativePath: "",
      createNewIfExist: true,
    });
    expect(first![0]).toBe("http://portal/api/2.0/files/12/session/s1");
    expect(((first![1].body as FormData).get("file") as Blob).size).toBe(4);
    expect(((second![1].body as FormData).get("file") as Blob).size).toBe(2);
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});

describe("copyIntoProject", () => {
  it("copies a file of the portal into the project room and answers the copy", async () => {
    portal({ title: "Report.docx", folderId: 5, pureContentLength: 6 });
    expect(await copyIntoProject("12", "40")).toBe("77");
    expect(readFileBytes).toHaveBeenCalledWith("40", MAX_COPY_BYTES);
  });

  it("leaves a file that already lies in the project room", async () => {
    portal({ title: "Report.docx", folderId: 12 });
    expect(await copyIntoProject("12", "40")).toBe("40");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("leaves a third-party file and a file over the limit", async () => {
    portal({ title: "Big.docx", folderId: 5, pureContentLength: MAX_COPY_BYTES + 1 });
    expect(await copyIntoProject("12", "sbox-1-abc")).toBe("sbox-1-abc");
    expect(await copyIntoProject("12", "40")).toBe("40");
    expect(readFileBytes).not.toHaveBeenCalled();
  });

  it("attaches the file where it is when the room refuses it or it grows over the limit", async () => {
    portal({ title: "Movie.mp4", folderId: 5 });
    fetchMock.mockResolvedValue(answer(400));
    expect(await copyIntoProject("12", "40")).toBe("40");

    vi.mocked(readFileBytes).mockRejectedValue(new FileTooLargeError("40", MAX_COPY_BYTES));
    expect(await copyIntoProject("12", "40")).toBe("40");
  });

  it("attaches the file where it is on any other failure: a timeout, a session without an id", async () => {
    portal({ title: "Report.docx", folderId: 5 });
    vi.mocked(readFileBytes).mockRejectedValue(new DOMException("timed out", "TimeoutError"));
    expect(await copyIntoProject("12", "40")).toBe("40");

    vi.mocked(readFileBytes).mockResolvedValue(new Uint8Array([1]));
    fetchMock.mockResolvedValue(answer(200, {}));
    expect(await copyIntoProject("12", "40")).toBe("40");

    vi.mocked(filesApiGet).mockRejectedValue(new TypeError("fetch failed"));
    expect(await copyIntoProject("12", "40")).toBe("40");
  });
});

describe("copyManyIntoProject", () => {
  it("copies a file named more than once a single time", async () => {
    portal({ title: "Report.docx", folderId: 5, pureContentLength: 6 });
    expect(await copyManyIntoProject("12", ["40", "40", "sbox-1-abc", "40"])).toEqual([
      "77",
      "77",
      "sbox-1-abc",
      "77",
    ]);
    expect(readFileBytes).toHaveBeenCalledTimes(1);
  });

  it("holds no more files in memory at once than the limit, across batches", async () => {
    portal({ title: "Report.docx", folderId: 5, pureContentLength: 6 });
    let held = 0;
    let most = 0;
    const release: (() => void)[] = [];
    vi.mocked(readFileBytes).mockImplementation(async () => {
      held += 1;
      most = Math.max(most, held);
      await new Promise<void>((resolve) => release.push(resolve));
      held -= 1;
      return new Uint8Array([1, 2, 3]);
    });

    const ids = ["41", "42", "43", "44", "45", "46", "47"];
    const batches = Promise.all([
      copyManyIntoProject("12", ids.slice(0, 4)),
      copyManyIntoProject("12", ids.slice(4)),
    ]);
    for (let done = 0; done < ids.length; done++) {
      await vi.waitFor(() => expect(release.length).toBeGreaterThan(0));
      release.shift()!();
    }
    expect((await batches).flat()).toHaveLength(ids.length);
    expect(readFileBytes).toHaveBeenCalledTimes(ids.length);
    expect(most).toBe(MAX_CONCURRENT_COPIES);
  });

  it("frees its place when a copy fails", async () => {
    portal({ title: "Report.docx", folderId: 5 });
    vi.mocked(readFileBytes).mockRejectedValue(new Error("boom"));
    expect(await copyManyIntoProject("12", ["41", "42", "43"])).toEqual(["41", "42", "43"]);

    vi.mocked(readFileBytes).mockResolvedValue(new Uint8Array([1]));
    expect(await copyManyIntoProject("12", ["44", "45", "46"])).toEqual(["77", "77", "77"]);
  });
});
