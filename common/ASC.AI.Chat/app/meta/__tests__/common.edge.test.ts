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

// The branches of `common.ts` that `common.test.ts` leaves open: a failed file read that is not a
// refusal, the skip reasons, a hand-built rejection of the model call and the budget check. The
// portal reads and the model call are stubbed at the module boundary, as in `common.test.ts`.

import { beforeEach, describe, expect, it, vi } from "vitest";
import { FakeDocspaceApiHttpError, logger } from "./mocks.js";

vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
}));
vi.mock("../../storage/index.js", () => ({
  storage: {
    assignments: { readByType: vi.fn() },
    profiles: { readById: vi.fn() },
  },
}));
vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => ({
  ...(await importOriginal<Record<string, unknown>>()),
  customSync: vi.fn(),
}));

const readFileAsText = vi.fn<(file: { id: string }) => Promise<string>>();
vi.mock("../portalFiles.js", () => ({
  getPortalFile: vi.fn(),
  listFolderFiles: vi.fn(),
  PortalTextReader: class {
    readFileAsText = readFileAsText;
  },
  UnsupportedFileError: class UnsupportedFileError extends Error {},
}));

const portalFiles = await import("../portalFiles.js");
const { storage } = await import("../../storage/index.js");
type MetaSkipped = import("../common.js").MetaSkipped;
const {
  assertNotTimedOut,
  classifyModelError,
  collectFiles,
  MAX_FILES,
  parseJsonObject,
  readDocuments,
  resolveMetaProfile,
  toSources,
  totalChars,
} = await import("../common.js");

const file = (id: string) => ({ id, title: `${id}.docx`, ext: ".docx" });

/** A promise the test settles itself, to order the reads without waiting on the clock. */
function deferred(): { promise: Promise<void>; resolve: () => void } {
  let resolve: () => void = () => undefined;
  const promise = new Promise<void>((r) => {
    resolve = r;
  });
  return { promise, resolve };
}

/** Lets every pending promise callback run: a macrotask comes after all microtasks. */
const settle = () => new Promise<void>((resolve) => setImmediate(resolve));

describe("collectFiles when a file read fails", () => {
  beforeEach(() => vi.clearAllMocks());

  it.each([
    ["a 500 of the Files API", new FakeDocspaceApiHttpError(500, "Server Error", "u")],
    ["a 404 thrown instead of answered", new FakeDocspaceApiHttpError(404, "Not Found", "u")],
    ["a timeout", new Error("upstream timeout after 300000ms")],
  ])("fails the request on %s instead of skipping the file", async (_what, failure) => {
    vi.mocked(portalFiles.getPortalFile).mockRejectedValue(failure);
    const skipped: MetaSkipped[] = [];

    await expect(collectFiles(["1", "2"], ["9"], skipped)).rejects.toBe(failure);
    expect(skipped).toEqual([]);
    // The second read runs beside the first; the folder is never listed.
    expect(portalFiles.getPortalFile).toHaveBeenCalledTimes(2);
    expect(portalFiles.listFolderFiles).not.toHaveBeenCalled();
  });

  it("starts no read after one failed, beyond the ones already running", async () => {
    const failure = new FakeDocspaceApiHttpError(500, "Server Error", "u");
    vi.mocked(portalFiles.getPortalFile).mockImplementation(async (id) => {
      if (id === "1") throw failure;
      return file(id);
    });
    const ids = Array.from({ length: 12 }, (_, i) => String(i + 1));

    await expect(collectFiles(ids, [], [])).rejects.toBe(failure);
    expect(vi.mocked(portalFiles.getPortalFile).mock.calls.length).toBeLessThanOrEqual(4);
  });

  it("relays the failure of the earliest id, with the skips of the ids before it", async () => {
    const early = new FakeDocspaceApiHttpError(500, "Server Error", "early");
    const late = new FakeDocspaceApiHttpError(502, "Bad Gateway", "late");
    const lateFailed = deferred();
    vi.mocked(portalFiles.getPortalFile).mockImplementation(async (id) => {
      if (id === "gone") return undefined;
      if (id === "2") {
        // Fails after "3" has failed already.
        await lateFailed.promise;
        throw early;
      }
      if (id === "3") {
        lateFailed.resolve();
        throw late;
      }
      return file(id);
    });
    const skipped: MetaSkipped[] = [];

    await expect(collectFiles(["gone", "2", "3"], [], skipped)).rejects.toBe(early);
    expect(skipped).toEqual([{ id: "gone", reason: "not_found" }]);
  });
});

describe("collectFiles reading the file ids in parallel", () => {
  beforeEach(() => vi.clearAllMocks());

  it("reads at most four file infos at a time", async () => {
    let running = 0;
    let most = 0;
    const reads: ReturnType<typeof deferred>[] = [];
    vi.mocked(portalFiles.getPortalFile).mockImplementation(async (id) => {
      running += 1;
      most = Math.max(most, running);
      const read = deferred();
      reads.push(read);
      await read.promise;
      running -= 1;
      return file(id);
    });
    const ids = Array.from({ length: 10 }, (_, i) => String(i + 1));

    const collected = collectFiles(ids, [], []);
    await settle();
    expect(reads).toHaveLength(4);
    // Each read that ends lets the next one start, never more than four at once.
    for (let done = 0; done < reads.length; done += 1) {
      reads[done]?.resolve();
      await settle();
      expect(running).toBe(Math.min(4, ids.length - done - 1));
    }
    const files = await collected;

    expect(files).toHaveLength(10);
    expect(reads).toHaveLength(10);
    expect(most).toBe(4);
  });

  it("keeps the order of the ids, the skip reasons and the limit when the reads finish out of order", async () => {
    const ids = Array.from({ length: MAX_FILES + 4 }, (_, i) => `id${i}`);
    const pending: ReturnType<typeof deferred>[] = [];
    const answered: number[] = [];
    vi.mocked(portalFiles.getPortalFile).mockImplementation(async (id) => {
      const n = Number(id.slice(2));
      const read = deferred();
      pending.push(read);
      await read.promise;
      answered.push(n);
      if (n === 1) return undefined;
      if (n === 2) throw new FakeDocspaceApiHttpError(403, "Forbidden", "u");
      return file(id);
    });
    // The folder repeats a named file and adds one past the limit.
    vi.mocked(portalFiles.listFolderFiles).mockResolvedValue([file("id3"), file("f1")]);
    const skipped: MetaSkipped[] = [];

    const collected = collectFiles([...ids, "id0"], ["9"], skipped);
    // The read started last answers first, each time.
    await settle();
    while (pending.length > 0) {
      pending.pop()?.resolve();
      await settle();
    }
    const files = await collected;

    expect(answered).toHaveLength(ids.length);
    expect(answered.slice(0, 4)).toEqual([3, 4, 5, 6]);
    const taken = ids.filter((id) => id !== "id1" && id !== "id2");
    expect(files.map((f) => f.id)).toEqual(taken.slice(0, MAX_FILES));
    expect(skipped).toEqual([
      { id: "id1", reason: "not_found" },
      { id: "id2", reason: "forbidden" },
      ...[...taken.slice(MAX_FILES), "f1"].map((id) => ({
        id,
        title: `${id}.docx`,
        reason: "limit",
      })),
    ]);
    expect(portalFiles.getPortalFile).toHaveBeenCalledTimes(ids.length);
  });

  it("trims the ids, drops blank ones and asks for each distinct id once", async () => {
    vi.mocked(portalFiles.getPortalFile).mockImplementation(async (id: string) => file(id));

    const files = await collectFiles([" 1 ", "1", "", "  ", "2"], [], []);

    expect(files.map((f) => f.id)).toEqual(["1", "2"]);
    expect(vi.mocked(portalFiles.getPortalFile).mock.calls).toEqual([["1"], ["2"]]);
  });
});

describe("readDocuments skip reasons", () => {
  beforeEach(() => vi.clearAllMocks());

  it("reports a 404 as not_found, another 4xx and a non-Error as failed, and logs each", async () => {
    // The four workers each take one file, in order, so the answers go out in that order too.
    readFileAsText
      .mockRejectedValueOnce(new FakeDocspaceApiHttpError(404, "Not Found", "u"))
      .mockRejectedValueOnce(new FakeDocspaceApiHttpError(409, "Conflict", "u"))
      .mockRejectedValueOnce("a string")
      .mockResolvedValueOnce("text");
    const skipped: MetaSkipped[] = [];

    const documents = await readDocuments(
      ["gone", "conflict", "odd", "ok"].map(file),
      skipped,
      new AbortController().signal,
    );

    expect(documents.map((d) => d.file.id)).toEqual(["ok"]);
    expect(skipped).toEqual([
      { id: "gone", title: "gone.docx", reason: "not_found" },
      { id: "conflict", title: "conflict.docx", reason: "failed" },
      { id: "odd", title: "odd.docx", reason: "failed" },
    ]);
    expect(logger.warn).toHaveBeenCalledWith(
      'meta: file odd "odd.docx" skipped (failed): a string',
    );
    expect(logger.warn).toHaveBeenCalledWith(
      'meta: file gone "gone.docx" skipped (not_found): DocSpace API 404 Not Found for u',
    );
  });

  it("starts no reader work for an empty selection", async () => {
    expect(await readDocuments([], [], new AbortController().signal)).toEqual([]);
    expect(readFileAsText).not.toHaveBeenCalled();
  });
});

describe("classifyModelError for a rejection that is not an Error", () => {
  const live = new AbortController().signal;

  it("classifies a plain object by its status and keeps its message", () => {
    const failure = classifyModelError({ status: 429, message: "429 slow down" }, live);

    expect(failure).toMatchObject({
      status: 429,
      code: "rate_limit",
      providerMessage: "429 slow down",
    });
  });

  it("names a plain object without a string message by its string form", () => {
    const failure = classifyModelError({ status: 402, message: 5 }, live);

    expect(failure).toMatchObject({ status: 402, code: "insufficient_funds" });
    expect(logger.error).not.toHaveBeenCalled();
  });

  it("logs a failed round with its code and detail", () => {
    classifyModelError({ status: 500, message: "boom" }, live);

    expect(logger.error).toHaveBeenCalledWith(expect.stringMatching(/^meta: model call failed \(/));
  });

  it("reads null as an unknown failure", () => {
    expect(classifyModelError(null, live)).toMatchObject({
      status: 502,
      message: "The model request failed",
      providerMessage: undefined,
    });
  });
});

describe("assertNotTimedOut", () => {
  it("throws 504 timeout once the budget signal aborted and nothing before", () => {
    expect(() => assertNotTimedOut(new AbortController().signal)).not.toThrow();
    expect(() => assertNotTimedOut(AbortSignal.abort())).toThrow(
      expect.objectContaining({
        status: 504,
        code: "timeout",
        message: "Reading the files did not finish within the time budget",
        providerMessage: undefined,
        expose: true,
      }),
    );
  });
});

describe("resolveMetaProfile when the storage fails", () => {
  it("rethrows a failed slot read instead of answering no profile", async () => {
    const failure = new Error("AI service down");
    vi.mocked(storage.assignments.readByType).mockRejectedValue(failure);

    await expect(resolveMetaProfile()).rejects.toBe(failure);
    expect(storage.profiles.readById).not.toHaveBeenCalled();
  });
});

describe("the small helpers", () => {
  it("lists the sources and counts the characters", () => {
    const documents = [
      { file: file("a"), text: "abc", truncated: true },
      { file: file("b"), text: "de", truncated: false },
    ];

    expect(toSources(documents)).toEqual([
      { id: "a", title: "a.docx", truncated: true },
      { id: "b", title: "b.docx", truncated: false },
    ]);
    expect(totalChars(documents)).toBe(5);
  });

  it("answers undefined for an object that never closes and keeps an escaped backslash", () => {
    expect(parseJsonObject('{"a":"}')).toBeUndefined();
    expect(parseJsonObject('x {"a":"\\\\"} y')).toEqual({ a: "\\" });
  });
});
