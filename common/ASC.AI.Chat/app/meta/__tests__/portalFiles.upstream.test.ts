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

// The portal reads of the metadata routes against the Files API stubbed at the network (msw), so
// the real `httpClient.js` and `requestContext.js` run: the URL, the forwarded headers, the
// status mapping and the timeout are what these tests see. `portalFiles.ts` caches the
// conversion table for the process, so every test of the table and of the reader imports a fresh
// copy of the module; the file reads do not touch the table and use the static import.

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useFastTimers } from "../../__tests__/helpers/time.js";
import { delay, http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import logger from "../../log.js";
import { DocspaceApiHttpError } from "../../storage/docspaceFilesApi.js";
import { getPortalFile, listFolderFiles } from "../portalFiles.js";

const upstream = useUpstream();

async function fresh() {
  vi.resetModules();
  const portalFiles = await import("../portalFiles.js");
  const { DocspaceApiHttpError } = await import("../../storage/docspaceFilesApi.js");
  const { default: logger } = await import("../../log.js");
  // The helper binds the request context of the module copy it imported: take a fresh one too.
  const { inRequestContext } = await import("../../__tests__/helpers/http.js");
  return { ...portalFiles, DocspaceApiHttpError, logger, inRequestContext };
}

function envelope(response: unknown, status = 200): Response {
  return HttpResponse.json({ response, count: 1, status: 0, statusCode: status }, { status });
}

// FileDto as `GET api/2.0/files/file/{fileId}` and the `files` of a folder listing answer it
// (products/ASC.Files/Core/ApiModels/ResponseDto/FileDto.cs, FileEntryDto.cs).
const FILE_DTO = {
  folderId: 12,
  version: 1,
  versionGroup: 1,
  contentLength: "12.4 KB",
  pureContentLength: 12698,
  fileStatus: 0,
  mute: false,
  viewUrl: "https://portal.example.com/filehandler.ashx?action=view&fileid=10",
  webUrl: "https://portal.example.com/doceditor?fileid=10",
  fileType: 7,
  fileExst: ".docx",
  comment: "Uploaded",
  thumbnailStatus: 1,
  id: 10,
  rootFolderId: 5,
  canShare: true,
  title: "Contract.docx",
  access: 0,
  shared: false,
  created: "2026-11-01T10:00:00.0000000+00:00",
  createdBy: { id: "u1", displayName: "Alice" },
  updated: "2026-11-02T10:00:00.0000000+00:00",
  rootFolderType: 14,
  updatedBy: { id: "u1", displayName: "Alice" },
  fileEntryType: 2,
};

// FolderContentDto of `GET api/2.0/files/{folderId}`
// (products/ASC.Files/Core/ApiModels/ResponseDto/FolderContentDto.cs).
const FOLDER_CONTENT_DTO = {
  files: [FILE_DTO, { ...FILE_DTO, id: 11, title: "Budget.XLSX", fileExst: ".XLSX" }],
  folders: [{ id: 99, title: "Sub" }],
  current: { id: 12, title: "Room" },
  pathParts: [{ id: 12, title: "Room" }],
  startIndex: 0,
  count: 2,
  total: 2,
  new: 0,
};

// FileOperationDto of `PUT api/2.0/files/fileops/bulkdownload` and `GET api/2.0/files/fileops`
// (products/ASC.Files/Core/ApiModels/ResponseDto/FileOperationDto.cs).
function operationDto(id: string, extra: Record<string, unknown> = {}) {
  return {
    id,
    operation: 3,
    progress: 0,
    error: "",
    processed: "0",
    finished: false,
    files: [],
    folders: [],
    ...extra,
  };
}

// `extsConvertible` of `GET api/2.0/files/settings`
// (products/ASC.Files/Server/ApiModels/ResponseDto/FilesSettingsDto.cs).
const SETTINGS_DTO = {
  extsConvertible: { ".docx": [".pdf", ".txt"], ".xlsx": [".csv"], ".png": [".jpg"] },
  extsWebEdited: [".docx"],
};

const DOCX = { id: "10", title: "Contract.docx", ext: ".docx" };

afterEach(() => {
  vi.useRealTimers();
});

describe("getPortalFile against the Files API", () => {
  it("maps the file DTO and forwards the caller's credentials to the portal", async () => {
    upstream.use(http.get("*/api/2.0/files/file/:id", () => envelope(FILE_DTO)));

    const file = await inRequestContext(
      { cookie: "asc_auth_key=k", authorization: "Bearer t" },
      () => getPortalFile("10"),
    );

    expect(file).toEqual({ id: "10", title: "Contract.docx", ext: ".docx" });
    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]).toMatchObject({ method: "GET", path: "/api/2.0/files/file/10" });
    expect(upstream.seen[0]?.headers).toMatchObject({
      cookie: "asc_auth_key=k",
      authorization: "Bearer t",
    });
  });

  it("encodes the id into the path", async () => {
    upstream.use(http.get("*/api/2.0/files/file/*", () => envelope(FILE_DTO)));

    await getPortalFile("a/b c");

    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/file/a%2Fb%20c");
  });

  it.each([
    ["id missing", { id: undefined }, undefined],
    ["id null", { id: null }, undefined],
    ["id a boolean", { id: true }, undefined],
    ["id empty", { id: "" }, undefined],
    ["id a string", { id: "sbox-10" }, { id: "sbox-10", title: "Contract.docx", ext: ".docx" }],
    ["title missing", { title: undefined }, undefined],
    ["title null", { title: null }, undefined],
    ["title a number", { title: 5 }, undefined],
    ["title empty", { title: "" }, undefined],
    [
      "fileExst missing",
      { fileExst: undefined },
      { id: "10", title: "Contract.docx", ext: ".docx" },
    ],
    ["fileExst null", { fileExst: null }, { id: "10", title: "Contract.docx", ext: ".docx" }],
    ["fileExst a number", { fileExst: 7 }, { id: "10", title: "Contract.docx", ext: ".docx" }],
    ["fileExst empty", { fileExst: "" }, { id: "10", title: "Contract.docx", ext: "" }],
    [
      "fileExst upper case",
      { fileExst: ".PDF" },
      { id: "10", title: "Contract.docx", ext: ".pdf" },
    ],
    [
      "no extension anywhere",
      { fileExst: undefined, title: "README" },
      { id: "10", title: "README", ext: "" },
    ],
    [
      "a title that is only a dot-name",
      { fileExst: undefined, title: ".hidden" },
      { id: "10", title: ".hidden", ext: "" },
    ],
  ])("maps a file DTO with %s", async (_what, patch, expected) => {
    upstream.use(http.get("*/api/2.0/files/file/:id", () => envelope({ ...FILE_DTO, ...patch })));

    expect(await getPortalFile("10")).toEqual(expected);
  });

  it.each([
    ["a response that is an array", { response: [FILE_DTO] }],
    ["a response that is null", { response: null }],
    ["no response", { status: 0 }],
    ["a body that is an array", [FILE_DTO]],
    ["a body that is a string", "file"],
  ])("answers undefined for %s", async (_what, body) => {
    upstream.use(http.get("*/api/2.0/files/file/:id", () => HttpResponse.json(body)));

    expect(await getPortalFile("10")).toBeUndefined();
  });

  it("answers undefined for 404 only and rethrows a 500 as the Files API error", async () => {
    upstream.use(
      http.get("*/api/2.0/files/file/404", () => new HttpResponse(null, { status: 404 })),
      http.get("*/api/2.0/files/file/500", () =>
        HttpResponse.json({ error: "boom" }, { status: 500, statusText: "Server Error" }),
      ),
    );

    expect(await getPortalFile("404")).toBeUndefined();
    const failure = await getPortalFile("500").catch((err: unknown) => err);
    expect(failure).toBeInstanceOf(DocspaceApiHttpError);
    expect(failure).toMatchObject({ status: 500, statusText: "Server Error" });
  });

  it("rejects a body that is not JSON instead of reading it as absent", async () => {
    upstream.use(http.get("*/api/2.0/files/file/:id", () => HttpResponse.text("<html>")));

    await expect(getPortalFile("10")).rejects.toBeInstanceOf(SyntaxError);
  });

  it("rejects when the portal does not answer within the upstream timeout", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    upstream.use(
      http.get("*/api/2.0/files/file/:id", async () => {
        await delay("infinite");
        return envelope(FILE_DTO);
      }),
    );

    const read = getPortalFile("10").catch((err: unknown) => err);
    await vi.advanceTimersByTimeAsync(300_000);

    expect(await read).toMatchObject({ message: "upstream timeout after 300000ms" });
  });
});

describe("listFolderFiles against the Files API", () => {
  it("asks for one page of files, newest first, and maps the folder content DTO", async () => {
    upstream.use(http.get("*/api/2.0/files/:folder", () => envelope(FOLDER_CONTENT_DTO)));

    const files = await listFolderFiles("12", 100);

    expect(files).toEqual([
      { id: "10", title: "Contract.docx", ext: ".docx" },
      { id: "11", title: "Budget.XLSX", ext: ".xlsx" },
    ]);
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      "GET /api/2.0/files/12?filterType=FilesOnly&count=100&sortBy=DateAndTime&sortOrder=descending",
    ]);
    expect(logger.info).toHaveBeenCalledWith("meta.listFolderFiles(12) -> 2 file(s)");
  });

  it.each([
    ["files missing", { ...FOLDER_CONTENT_DTO, files: undefined }],
    ["files null", { ...FOLDER_CONTENT_DTO, files: null }],
    ["files an object", { ...FOLDER_CONTENT_DTO, files: { 0: FILE_DTO } }],
    ["files empty", { ...FOLDER_CONTENT_DTO, files: [] }],
    ["files of strings", { ...FOLDER_CONTENT_DTO, files: ["a.docx", 5, null] }],
    ["a response that is a list", [FILE_DTO]],
    ["a response that is null", null],
  ])("answers an empty list for a folder content with %s", async (_what, response) => {
    upstream.use(http.get("*/api/2.0/files/:folder", () => envelope(response)));

    expect(await listFolderFiles("12", 21)).toEqual([]);
  });

  it.each([
    [403, "Forbidden"],
    [404, "Not Found"],
    [500, "Server Error"],
  ])("rejects a folder the portal answers %i for", async (status, statusText) => {
    upstream.use(
      http.get("*/api/2.0/files/:folder", () => HttpResponse.json({}, { status, statusText })),
    );

    const failure = await listFolderFiles("12", 21).catch((err: unknown) => err);

    expect(failure).toBeInstanceOf(DocspaceApiHttpError);
    expect(failure).toMatchObject({ status, statusText });
  });
});

describe("textTargetFor and the conversion table", () => {
  it("drops a target list that is not an array and the targets that are not strings", async () => {
    const { textTargetFor } = await fresh();
    upstream.use(
      http.get("*/api/2.0/files/settings", () =>
        envelope({
          extsConvertible: { ".DOC": [5, ".txt"], ".odt": ".txt", ".rtf": [null, ".csv"] },
        }),
      ),
    );

    expect(await textTargetFor({ id: "1", title: "a.doc", ext: ".doc" })).toBe(".txt");
    expect(await textTargetFor({ id: "2", title: "a.odt", ext: ".odt" })).toBeUndefined();
    expect(await textTargetFor({ id: "3", title: "a.rtf", ext: ".rtf" })).toBe(".csv");
  });

  it.each([
    ["no extsConvertible", { extsWebEdited: [] }],
    ["extsConvertible null", { extsConvertible: null }],
    ["extsConvertible a list", { extsConvertible: [".docx"] }],
    ["extsConvertible empty", { extsConvertible: {} }],
    ["only non-list targets", { extsConvertible: { ".docx": ".txt" } }],
    ["a response that is not an object", "settings"],
  ])(
    "reads a table with %s as nothing converts and asks the portal again next time",
    async (_what, response) => {
      const { textTargetFor } = await fresh();
      let reads = 0;
      upstream.use(
        http.get("*/api/2.0/files/settings", () => {
          reads += 1;
          return reads === 1 ? envelope(response) : envelope(SETTINGS_DTO);
        }),
      );

      expect(await textTargetFor(DOCX)).toBeUndefined();
      expect(await textTargetFor(DOCX)).toBe(".txt");
      expect(await textTargetFor(DOCX)).toBe(".txt");
      expect(reads).toBe(2);
    },
  );

  it("rethrows a failed settings read and asks the portal again next time", async () => {
    const { textTargetFor, DocspaceApiHttpError } = await fresh();
    let reads = 0;
    upstream.use(
      http.get("*/api/2.0/files/settings", () => {
        reads += 1;
        return reads === 1
          ? HttpResponse.json({}, { status: 503, statusText: "Unavailable" })
          : envelope(SETTINGS_DTO);
      }),
    );

    await expect(textTargetFor(DOCX)).rejects.toBeInstanceOf(DocspaceApiHttpError);
    expect(await textTargetFor(DOCX)).toBe(".txt");
    expect(reads).toBe(2);
  });

  it("keeps plain text and csv without asking the portal", async () => {
    const { textTargetFor } = await fresh();

    expect(await textTargetFor({ id: "1", title: "a.csv", ext: ".csv" })).toBe(".csv");
    expect(upstream.seen).toEqual([]);
  });
});

describe("PortalTextReader against the Files API", () => {
  useFastTimers();

  function settings() {
    return http.get("*/api/2.0/files/settings", () => envelope(SETTINGS_DTO));
  }

  it("starts the conversion with the exact body, takes a finished operation without polling and downloads through the proxy", async () => {
    const { PortalTextReader, logger, inRequestContext } = await fresh();
    upstream.use(
      settings(),
      http.put("*/api/2.0/files/fileops/bulkdownload", () =>
        envelope([
          operationDto("op-1", {
            finished: true,
            progress: 100,
            url: "https://public.example.com/filehandler.ashx?action=bulk&ext=.txt",
          }),
        ]),
      ),
      http.get("*/filehandler.ashx", () => HttpResponse.text("  the text  ")),
    );

    const text = await inRequestContext({ cookie: "asc_auth_key=k" }, () =>
      new PortalTextReader(new AbortController().signal).readFileAsText(DOCX),
    );

    expect(text).toBe("  the text  ");
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      "GET /api/2.0/files/settings",
      "PUT /api/2.0/files/fileops/bulkdownload",
      "GET /filehandler.ashx?action=bulk&ext=.txt",
    ]);
    expect(upstream.seen[1]?.body).toEqual({
      fileConvertIds: [{ key: 10, value: ".txt" }],
      returnSingleOperation: true,
    });
    expect(upstream.seen[1]?.headers).toMatchObject({
      cookie: "asc_auth_key=k",
      "content-type": "application/json",
    });
    expect(upstream.seen[2]?.headers).toMatchObject({
      cookie: "asc_auth_key=k",
      accept: "text/plain",
    });
    expect(logger.info).toHaveBeenCalledWith(
      expect.stringMatching(
        /^meta\.readFileAsText\(10 "Contract\.docx" -> \.txt\) 12 chars in \d+ms$/,
      ),
    );
  });

  it("sends a third-party id as a string key", async () => {
    const { readFileAsText } = await fresh();
    upstream.use(
      http.put("*/api/2.0/files/fileops/bulkdownload", () =>
        envelope([operationDto("op-1", { finished: true, url: "/filehandler.ashx?x=1" })]),
      ),
      http.get("*/filehandler.ashx", () => HttpResponse.text("csv")),
    );

    expect(await readFileAsText({ id: "sbox-7", title: "t.csv", ext: ".csv" })).toBe("csv");
    expect(upstream.seen[0]?.body).toEqual({
      fileConvertIds: [{ key: "sbox-7", value: ".csv" }],
      returnSingleOperation: true,
    });
  });

  it.each([
    ["an empty list", envelope([])],
    ["entries without an id", envelope([{ operation: 3 }, "op-1", null, { id: "" }])],
    ["a response that is an object", envelope({ id: "op-1" })],
    ["a body that is a string", HttpResponse.json("op-1")],
    ["a body that is a bare list", HttpResponse.json([operationDto("op-1")])],
  ])("fails the read when the start answers %s", async (_what, answer) => {
    const { readFileAsText } = await fresh();
    upstream.use(
      settings(),
      http.put("*/api/2.0/files/fileops/bulkdownload", () => answer),
    );

    await expect(readFileAsText(DOCX)).rejects.toThrow(
      "bulkdownload for file 10 returned no operation",
    );
  });

  it("takes the first entry with an id and reads a missing error as none", async () => {
    const { readFileAsText } = await fresh();
    upstream.use(
      settings(),
      http.put("*/api/2.0/files/fileops/bulkdownload", () =>
        envelope([
          { operation: 3 },
          { id: "op-2", finished: true, url: "/filehandler.ashx?op=2" },
          operationDto("op-3", { finished: true, url: "/filehandler.ashx?op=3" }),
        ]),
      ),
      http.get("*/filehandler.ashx", ({ request }) =>
        HttpResponse.text(new URL(request.url).search),
      ),
    );

    expect(await readFileAsText(DOCX)).toBe("?op=2");
  });

  it("relays a start the portal answers 500 without retrying", async () => {
    const { readFileAsText, DocspaceApiHttpError } = await fresh();
    upstream.use(
      settings(),
      http.put("*/api/2.0/files/fileops/bulkdownload", () =>
        HttpResponse.json({}, { status: 500, statusText: "Server Error" }),
      ),
    );

    const failure = await readFileAsText(DOCX).catch((err: unknown) => err);

    expect(failure).toBeInstanceOf(DocspaceApiHttpError);
    expect(failure).toMatchObject({ status: 500 });
    expect(upstream.seen.filter((r) => r.method === "PUT")).toHaveLength(1);
  });

  it("fails a conversion that finished without a download URL", async () => {
    const { readFileAsText } = await fresh();
    upstream.use(
      settings(),
      http.put("*/api/2.0/files/fileops/bulkdownload", () =>
        envelope([operationDto("op-1", { finished: true, url: 5 })]),
      ),
    );

    await expect(readFileAsText(DOCX)).rejects.toThrow(
      "conversion of file 10 finished without a download URL",
    );
  });

  it.each([
    [403, "Forbidden"],
    [500, "Server Error"],
  ])("relays a download the portal answers %i for", async (status, statusText) => {
    const { readFileAsText, DocspaceApiHttpError } = await fresh();
    upstream.use(
      settings(),
      http.put("*/api/2.0/files/fileops/bulkdownload", () =>
        envelope([operationDto("op-1", { finished: true, url: "/filehandler.ashx?a=1" })]),
      ),
      http.get("*/filehandler.ashx", () => new HttpResponse("no", { status, statusText })),
    );

    const failure = await readFileAsText(DOCX).catch((err: unknown) => err);

    expect(failure).toBeInstanceOf(DocspaceApiHttpError);
    expect(failure).toMatchObject({ status, statusText });
  });

  it("fetches a download URL it cannot parse as it is", async () => {
    const { readFileAsText } = await fresh();
    upstream.use(
      settings(),
      http.put("*/api/2.0/files/fileops/bulkdownload", () =>
        envelope([operationDto("op-1", { finished: true, url: "http://[bad" })]),
      ),
    );

    await expect(readFileAsText(DOCX)).rejects.toBeInstanceOf(TypeError);
    expect(upstream.seen.map((r) => r.method)).toEqual(["GET", "PUT"]);
  });

  it("names a file without an extension in the refusal", async () => {
    const { readFileAsText, UnsupportedFileError } = await fresh();
    upstream.use(settings());

    const failure = await readFileAsText({ id: "1", title: "README", ext: "" }).catch(
      (err: unknown) => err,
    );

    expect(failure).toBeInstanceOf(UnsupportedFileError);
    expect(failure).toMatchObject({ message: "no text conversion for a file without extension" });
  });

  it("skips list entries that are not operations while polling", async () => {
    const { readFileAsText } = await fresh();
    let polls = 0;
    upstream.use(
      settings(),
      http.put("*/api/2.0/files/fileops/bulkdownload", () => envelope([operationDto("op-1")])),
      http.get("*/api/2.0/files/fileops", () => {
        polls += 1;
        return envelope([
          "op-1",
          null,
          { finished: true },
          operationDto("op-other", { finished: true, url: "/filehandler.ashx?other" }),
          operationDto("op-1", polls < 2 ? {} : { finished: true, url: "/filehandler.ashx?mine" }),
        ]);
      }),
      http.get("*/filehandler.ashx", ({ request }) =>
        HttpResponse.text(new URL(request.url).search),
      ),
    );

    expect(await readFileAsText(DOCX)).toBe("?mine");
    expect(upstream.seen.filter((r) => r.path.startsWith("/api/2.0/files/fileops?"))).toEqual([
      expect.objectContaining({ path: "/api/2.0/files/fileops?id=op-1" }),
      expect.objectContaining({ path: "/api/2.0/files/fileops?id=op-1" }),
    ]);
  });
});

describe("PortalTextReader when the reader is aborted", () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
  });

  it("stops polling once the reader's signal aborts while a read waits", async () => {
    const { PortalTextReader } = await fresh();
    upstream.use(
      http.get("*/api/2.0/files/settings", () => envelope(SETTINGS_DTO)),
      http.put("*/api/2.0/files/fileops/bulkdownload", () => envelope([operationDto("op-1")])),
    );
    const controller = new AbortController();

    const read = new PortalTextReader(controller.signal)
      .readFileAsText(DOCX)
      .catch((err: unknown) => err);
    await vi.waitFor(() => expect(upstream.seen).toHaveLength(2), { interval: 1 });
    await vi.advanceTimersByTimeAsync(0);
    controller.abort(new Error("request ended"));
    await vi.advanceTimersByTimeAsync(2_000);

    expect(await read).toMatchObject({ message: "request ended" });
    expect(upstream.seen.filter((r) => r.path.startsWith("/api/2.0/files/fileops?"))).toEqual([]);
  });

  it("drops a poll that fails because the reader was aborted, without a warning", async () => {
    const { PortalTextReader, logger } = await fresh();
    const controller = new AbortController();
    upstream.use(
      http.get("*/api/2.0/files/settings", () => envelope(SETTINGS_DTO)),
      http.put("*/api/2.0/files/fileops/bulkdownload", () => envelope([operationDto("op-1")])),
      http.get("*/api/2.0/files/fileops", async () => {
        controller.abort(new Error("request ended"));
        await delay("infinite");
        return envelope([]);
      }),
    );

    const read = new PortalTextReader(controller.signal)
      .readFileAsText(DOCX)
      .catch((err: unknown) => err);
    await vi.waitFor(() => expect(upstream.seen).toHaveLength(2), { interval: 1 });
    await vi.advanceTimersByTimeAsync(500);
    await vi.waitFor(() => expect(upstream.seen).toHaveLength(3), { interval: 1 });
    await vi.advanceTimersByTimeAsync(2_000);

    expect(await read).toMatchObject({ message: "request ended" });
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("ignores a poll answer that arrives after the read's own budget ran out", async () => {
    const { PortalTextReader, logger } = await fresh();
    let release: (() => void) | undefined;
    upstream.use(
      http.get("*/api/2.0/files/settings", () => envelope(SETTINGS_DTO)),
      http.put("*/api/2.0/files/fileops/bulkdownload", () => envelope([operationDto("op-1")])),
      http.get("*/api/2.0/files/fileops", async () => {
        await new Promise<void>((resolve) => {
          release = resolve;
        });
        return envelope([operationDto("op-1", { finished: true, url: "/filehandler.ashx" })]);
      }),
    );

    const read = new PortalTextReader(new AbortController().signal)
      .readFileAsText(DOCX)
      .catch((err: unknown) => err);
    await vi.waitFor(() => expect(upstream.seen).toHaveLength(2), { interval: 1 });
    await vi.advanceTimersByTimeAsync(500);
    await vi.waitFor(() => expect(release).toBeDefined(), { interval: 1 });
    await vi.advanceTimersByTimeAsync(90_000);
    expect(await read).toMatchObject({ message: "upstream timeout after 90000ms" });
    release?.();
    await vi.advanceTimersByTimeAsync(1_000);

    expect(upstream.seen.some((r) => r.path.startsWith("/filehandler.ashx"))).toBe(false);
    expect(logger.warn).not.toHaveBeenCalled();
  });
});
