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
import { useFastTimers } from "../../__tests__/helpers/time.js";
import {
  fakeFetch,
  headers,
  logger,
  portalJson,
  PROXY,
  textResponse,
  withTimeout,
} from "./mocks.js";

// The Files API clients are the real ones, over the `fetch` each test stubs: only the proxy
// address, the timeout and the caller's headers are the test's.
vi.mock("../../storage/httpClient.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../storage/httpClient.js")>()),
  withTimeout,
  proxyBaseUrl: PROXY,
}));
vi.mock("../../requestContext.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../requestContext.js")>()),
  getForwardedHeaders: () => headers,
}));

const { getPortalFile, listFolderFiles, PortalTextReader, textTargetFor, UnsupportedFileError } =
  await import("../portalFiles.js");

const SETTINGS = {
  extsConvertible: {
    ".docx": [".pdf", ".txt", ".odt"],
    ".xlsx": [".pdf", ".csv", ".ods"],
    ".pptx": [".pdf", ".txt"],
    ".png": [".jpg"],
  },
};

const settingsRoute = {
  match: (url: string) => url.endsWith("/api/2.0/files/settings"),
  answer: () => portalJson(SETTINGS),
};

function operation(id: string, extra: Record<string, unknown> = {}) {
  return { id, operation: 3, progress: 0, error: "", processed: "0", finished: false, ...extra };
}

describe("folder listing and file info", () => {
  beforeEach(() => vi.unstubAllGlobals());

  it("lists a folder's files, top level, newest first, with the extension lower-cased", async () => {
    const { fetchMock, calls } = fakeFetch([
      {
        match: (url) => url.includes("/api/2.0/files/12?"),
        answer: () =>
          portalJson({
            files: [
              { id: 1, title: "A.DOCX", fileExst: ".DOCX" },
              { id: "2", title: "b.txt" },
              { title: "no id" },
            ],
            folders: [{ id: 99, title: "sub" }],
          }),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const files = await listFolderFiles("12", 21);
    expect(files).toEqual([
      { id: "1", title: "A.DOCX", ext: ".docx" },
      { id: "2", title: "b.txt", ext: ".txt" },
    ]);
    const url = new URL(calls[0]!.url);
    expect(url.pathname).toBe("/api/2.0/files/12");
    expect(url.searchParams.get("filterType")).toBe("FilesOnly");
    expect(url.searchParams.get("count")).toBe("21");
    expect(url.searchParams.get("sortBy")).toBe("DateAndTime");
    expect(url.searchParams.get("sortOrder")).toBe("descending");
  });

  it("answers undefined for a file the portal does not know and relays other failures", async () => {
    const { fetchMock } = fakeFetch([
      {
        match: (url) => url.endsWith("/files/file/404"),
        answer: () => new Response("", { status: 404, statusText: "Not Found" }),
      },
      {
        match: (url) => url.endsWith("/files/file/403"),
        answer: () => new Response("", { status: 403, statusText: "Forbidden" }),
      },
      {
        match: (url) => url.endsWith("/files/file/1"),
        answer: () => portalJson({ id: 1, title: "x.pdf", fileExst: ".pdf" }),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    expect(await getPortalFile("404")).toBeUndefined();
    await expect(getPortalFile("403")).rejects.toMatchObject({ status: 403 });
    expect(await getPortalFile("1")).toEqual({ id: "1", title: "x.pdf", ext: ".pdf" });
  });
});

describe("textTargetFor", () => {
  beforeEach(() => vi.unstubAllGlobals());

  it("keeps txt/csv as they are and picks txt or csv from the conversion table", async () => {
    const { fetchMock, calls } = fakeFetch([settingsRoute]);
    vi.stubGlobal("fetch", fetchMock);
    expect(await textTargetFor({ id: "1", title: "a.txt", ext: ".txt" })).toBe(".txt");
    expect(await textTargetFor({ id: "2", title: "a.csv", ext: ".csv" })).toBe(".csv");
    expect(await textTargetFor({ id: "3", title: "a.docx", ext: ".docx" })).toBe(".txt");
    expect(await textTargetFor({ id: "4", title: "a.xlsx", ext: ".xlsx" })).toBe(".csv");
    expect(await textTargetFor({ id: "5", title: "a.png", ext: ".png" })).toBeUndefined();
    expect(await textTargetFor({ id: "6", title: "noext", ext: "" })).toBeUndefined();
    // The table is read once for the process.
    expect(calls.filter((c) => c.url.endsWith("/files/settings"))).toHaveLength(1);
  });
});

describe("PortalTextReader", () => {
  // The reader waits 500 ms between polls and 1.5 s between retries of a start.
  useFastTimers();
  beforeEach(() => vi.unstubAllGlobals());

  it("converts through bulkdownload, polls the operation and downloads the text through the proxy", async () => {
    let polls = 0;
    const { fetchMock, calls } = fakeFetch([
      settingsRoute,
      {
        match: (url, init) => url.endsWith("/files/fileops/bulkdownload") && init?.method === "PUT",
        answer: () => portalJson([operation("op-1")]),
      },
      {
        match: (url) => url.includes("/api/2.0/files/fileops?id="),
        answer: () => {
          polls += 1;
          return portalJson(
            polls < 2
              ? [operation("op-1", { progress: 50 })]
              : [
                  operation("op-1", {
                    progress: 100,
                    finished: true,
                    url: "https://public.example.com/filehandler.ashx?action=bulk&ext=.txt",
                  }),
                ],
          );
        },
      },
      {
        match: (url) => url.startsWith(`${PROXY}/filehandler.ashx?action=bulk`),
        answer: () => textResponse("Hello, contract."),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);

    const reader = new PortalTextReader(new AbortController().signal);
    const text = await reader.readFileAsText({ id: "77", title: "c.docx", ext: ".docx" });
    expect(text).toBe("Hello, contract.");

    const start = calls.find((c) => c.url.endsWith("/fileops/bulkdownload"));
    expect(JSON.parse(String(start?.init?.body))).toEqual({
      fileConvertIds: [{ key: 77, value: ".txt" }],
      returnSingleOperation: true,
    });
    expect(polls).toBe(2);
    // Each poll asks for this operation only: a read of the whole list would
    // dequeue the user's other finished operations behind the UI's back.
    for (const poll of calls.filter((c) => c.url.includes("/api/2.0/files/fileops?"))) {
      expect(new URL(poll.url).searchParams.get("id")).toBe("op-1");
    }
    const download = calls.find((c) => c.url.includes("filehandler.ashx"));
    expect(download?.url).toBe(`${PROXY}/filehandler.ashx?action=bulk&ext=.txt`);
    expect((download?.init?.headers as Record<string, string>)["accept"]).toBe("text/plain");
  }, 10_000);

  it("starts the conversions one at a time, since the portal allows one download per user", async () => {
    const started: string[] = [];
    const finished = new Set<string>();
    const startedWhileRunning: string[] = [];
    const fetchMock = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
      if (url.endsWith("/files/settings")) {
        return portalJson(SETTINGS);
      }
      if (url.endsWith("/fileops/bulkdownload")) {
        const body = JSON.parse(String(init?.body)) as { fileConvertIds: { key: number }[] };
        const id = `op-${body.fileConvertIds[0]!.key}`;
        if (started.some((s) => !finished.has(s))) {
          startedWhileRunning.push(id);
        }
        started.push(id);
        return portalJson([operation(id)]);
      }
      if (url.includes("/api/2.0/files/fileops?id=")) {
        // Every operation started so far is finished by the next poll; the
        // portal answers with the one operation asked for.
        for (const id of started) {
          finished.add(id);
        }
        const asked = new URL(url).searchParams.get("id");
        return portalJson(
          started
            .filter((id) => id === asked)
            .map((id) =>
              operation(id, { finished: true, url: `https://x/filehandler.ashx?op=${id}` }),
            ),
        );
      }
      if (url.includes("filehandler.ashx")) {
        return textResponse(`text of ${new URL(url).searchParams.get("op")}`);
      }
      return new Response("", { status: 404 });
    });
    vi.stubGlobal("fetch", fetchMock);

    const reader = new PortalTextReader(new AbortController().signal);
    const [a, b] = await Promise.all([
      reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" }),
      reader.readFileAsText({ id: "2", title: "b.docx", ext: ".docx" }),
    ]);
    expect(a).toBe("text of op-1");
    expect(b).toBe("text of op-2");
    expect(started).toEqual(["op-1", "op-2"]);
    expect(startedWhileRunning).toEqual([]);
  }, 10_000);

  it("retries the start while the user's download slot is busy", async () => {
    let starts = 0;
    const { fetchMock } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => {
          starts += 1;
          return starts < 3
            ? new Response("", { status: 403, statusText: "Forbidden" })
            : portalJson([
                operation("op-1", { finished: true, url: "https://x/filehandler.ashx?a=1" }),
              ]);
        },
      },
      {
        match: (url) => url.includes("filehandler.ashx"),
        answer: () => textResponse("late but here"),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" })).resolves.toBe(
      "late but here",
    );
    expect(starts).toBe(3);
  }, 10_000);

  it("fails a read that is refused even after the retries", async () => {
    const { fetchMock, calls } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => new Response("", { status: 403, statusText: "Forbidden" }),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(
      reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" }),
    ).rejects.toMatchObject({ status: 403 });
    expect(calls.filter((c) => c.url.endsWith("/fileops/bulkdownload"))).toHaveLength(4);
  }, 15_000);

  it("starts the next queued conversion after one the portal refused", async () => {
    const { fetchMock } = fakeFetch([
      settingsRoute,
      {
        match: (url, init) =>
          url.endsWith("/fileops/bulkdownload") && String(init?.body).includes('"key":1'),
        answer: () => new Response("", { status: 500, statusText: "Server Error" }),
      },
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () =>
          portalJson([
            operation("op-2", { finished: true, url: "https://x/filehandler.ashx?b=2" }),
          ]),
      },
      {
        match: (url) => url.includes("filehandler.ashx"),
        answer: () => textResponse("text of b"),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    const [a, b] = await Promise.allSettled([
      reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" }),
      reader.readFileAsText({ id: "2", title: "b.docx", ext: ".docx" }),
    ]);
    expect(a).toMatchObject({ status: "rejected", reason: { status: 500 } });
    expect(b).toEqual({ status: "fulfilled", value: "text of b" });
  });

  it("survives a failed poll and reads the result on the next one", async () => {
    let polls = 0;
    const { fetchMock } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => portalJson([operation("op-1")]),
      },
      {
        match: (url) => url.includes("/api/2.0/files/fileops?id="),
        answer: () => {
          polls += 1;
          return polls === 1
            ? new Response("", { status: 502, statusText: "Bad Gateway" })
            : portalJson([
                operation("op-1", { finished: true, url: "https://x/filehandler.ashx?a=1" }),
              ]);
        },
      },
      { match: (url) => url.includes("filehandler.ashx"), answer: () => textResponse("ok") },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" })).resolves.toBe(
      "ok",
    );
    expect(polls).toBe(2);
  }, 10_000);

  it("counts only consecutive misses, so an operation that flickers out of the list survives", async () => {
    let polls = 0;
    const { fetchMock } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => portalJson([operation("op-1")]),
      },
      {
        match: (url) => url.includes("/api/2.0/files/fileops?id="),
        answer: () => {
          polls += 1;
          // Missing on polls 1, 3 and 5 (three misses, never consecutive),
          // finished on poll 6.
          if (polls % 2 === 1 && polls < 6) {
            return portalJson([]);
          }
          return portalJson([
            operation(
              "op-1",
              polls >= 6
                ? { finished: true, url: "https://x/filehandler.ashx?a=1" }
                : { progress: 40 },
            ),
          ]);
        },
      },
      { match: (url) => url.includes("filehandler.ashx"), answer: () => textResponse("ok") },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" })).resolves.toBe(
      "ok",
    );
    expect(polls).toBe(6);
  }, 10_000);

  it("reports an operation that is born with an error", async () => {
    const { fetchMock } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => portalJson([operation("op-1", { error: "Unsupported" })]),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" })).rejects.toThrow(
      /conversion failed: Unsupported/,
    );
  });

  it("fails the read when the conversion reports an error", async () => {
    const { fetchMock } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => portalJson([operation("op-err")]),
      },
      {
        match: (url) => url.includes("/api/2.0/files/fileops?id="),
        answer: () => portalJson([operation("op-err", { error: "Password protected" })]),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" })).rejects.toThrow(
      /conversion failed: Password protected/,
    );
  }, 10_000);

  it("gives up on an operation that vanished from the list before it finished", async () => {
    const { fetchMock } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => portalJson([operation("op-gone")]),
      },
      { match: (url) => url.includes("/api/2.0/files/fileops?id="), answer: () => portalJson([]) },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" })).rejects.toThrow(
      /disappeared before it finished/,
    );
  }, 10_000);

  it("refuses a file the portal cannot convert to text without calling the portal", async () => {
    const { fetchMock, calls } = fakeFetch([settingsRoute]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(
      reader.readFileAsText({ id: "1", title: "a.png", ext: ".png" }),
    ).rejects.toBeInstanceOf(UnsupportedFileError);
    expect(calls.some((c) => c.url.includes("bulkdownload"))).toBe(false);
  });

  it("relays a 403 from the conversion start", async () => {
    const { fetchMock } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => new Response("", { status: 403, statusText: "Forbidden" }),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const reader = new PortalTextReader(new AbortController().signal);
    await expect(
      reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" }),
    ).rejects.toMatchObject({ status: 403 });
  });
});

describe("PortalTextReader budget", () => {
  beforeEach(() => vi.unstubAllGlobals());

  it("gives up waiting for an operation when the read's own signal aborts", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    const { fetchMock, calls } = fakeFetch([
      settingsRoute,
      {
        match: (url) => url.endsWith("/fileops/bulkdownload"),
        answer: () => portalJson([operation("op-slow")]),
      },
      {
        match: (url) => url.includes("/api/2.0/files/fileops?id="),
        answer: () => portalJson([operation("op-slow", { progress: 10 })]),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const controller = new AbortController();
    const reader = new PortalTextReader(controller.signal);
    const polls = () => calls.filter((c) => c.url.includes("/api/2.0/files/fileops?id=")).length;
    try {
      const read = reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" });
      const outcome = expect(read).rejects.toThrow(/budget/);
      // Two polls of 500 ms each find the operation still running.
      await vi.advanceTimersByTimeAsync(1_000);
      expect(polls()).toBe(2);

      controller.abort(new Error("budget"));

      await outcome;
      // The poller has no waiter left, so it stops.
      await vi.advanceTimersByTimeAsync(2_000);
      expect(polls()).toBe(2);
    } finally {
      vi.useRealTimers();
    }
  });
});
