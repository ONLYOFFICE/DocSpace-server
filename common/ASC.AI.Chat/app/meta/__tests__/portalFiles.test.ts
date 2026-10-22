import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  FakeDocspaceApiHttpError,
  fakeFetch,
  headers,
  logger,
  portalJson,
  PROXY,
  textResponse,
  withTimeout,
} from "./mocks.js";

vi.mock("../../log.js", () => ({ default: logger }));
vi.mock("../../storage/httpClient.js", () => ({ withTimeout, proxyBaseUrl: PROXY }));
vi.mock("../../requestContext.js", () => ({ getForwardedHeaders: () => headers }));
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
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

type Route = { match: (url: string, init?: RequestInit) => boolean; answer: () => Response };

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
    const { fetchMock } = fakeFetch([
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
    const read = reader.readFileAsText({ id: "1", title: "a.docx", ext: ".docx" });
    setTimeout(() => controller.abort(new Error("budget")), 50);
    await expect(read).rejects.toThrow(/budget/);
  });
});
