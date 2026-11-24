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

import { UpstreamShapeError } from "../upstreamShape.js";
import { describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { getFilesApiReadCount } from "../../requestContext.js";
import { DocspaceApiHttpError } from "../docspaceFilesApi.js";
import { proxyBaseUrl } from "../httpClient.js";
import {
  downloadHeaders,
  FileTooLargeError,
  getContextFolders,
  getKnowledgeFolders,
  getRoomSkill,
  getRoomSkills,
  getRoomsSkills,
  parseSkillsCatalog,
  presignedDownloadUrl,
  readBodyCapped,
  readFileBytes,
  readFileText,
  throughProxy,
} from "../roomSkills.js";

// The real Files API client over a stubbed network. Routes of products/ASC.Files/Server:
// GET api/2.0/files/rooms (VirtualRoomsController, the rooms listing), GET api/2.0/files/rooms/skills
// (Api/AiHubController.cs GetRoomsSkills), GET api/2.0/files/file/{fileId}/presigneduri
// (Api/FilesController.cs), and the download address it mints.
const upstream = useUpstream();

/** `AiSkillsDto` (products/ASC.Files/Server/ApiModels/ResponseDto/AiSkillsDto.cs) in the Files API envelope. */
const skillsDto = () => ({
  rooms: [
    {
      id: 17001,
      title: "Brand voice",
      logo: { original: "", large: "", medium: "", small: "", color: "FF6680", cover: null },
      skills: [
        { id: "5001", name: "tone", description: "Write in the brand's tone." },
        { id: "5002", name: "glossary", description: "" },
      ],
    },
  ],
});

const envelope = (response: unknown) => HttpResponse.json({ response, count: 1, status: 0 });

describe("parseSkillsCatalog", () => {
  it("maps a full AiSkillsDto", () => {
    expect(parseSkillsCatalog(skillsDto())).toEqual([
      {
        id: "17001",
        title: "Brand voice",
        logo: { original: "", large: "", medium: "", small: "", color: "FF6680", cover: null },
        skills: [
          { id: "5001", name: "tone", description: "Write in the brand's tone." },
          { id: "5002", name: "glossary", description: "" },
        ],
      },
    ]);
  });

  // AiSkillsDto carries `rooms`: a catalog without the list is unknown, not a catalog of no room.
  it.each([null, "rooms", [], {}, { rooms: null }, { rooms: "x" }])(
    "throws UpstreamShapeError for %j",
    (raw) => {
      expect(() => parseSkillsCatalog(raw)).toThrow(
        new UpstreamShapeError("GET files/rooms/skills"),
      );
    },
  );

  it("answers no room for an empty list", () => {
    expect(parseSkillsCatalog({ rooms: [] })).toEqual([]);
  });

  it.each([
    ["id missing", { title: "T" }],
    ["id null", { id: null }],
    ["id empty", { id: "" }],
    ["not an object", "room"],
  ])("drops a room with %s", (_name, room) => {
    expect(parseSkillsCatalog({ rooms: [room] })).toEqual([]);
  });

  it.each([
    ["title missing", { id: 1 }, { id: "1", title: "", logo: null, skills: [] }],
    ["title null", { id: 1, title: null }, { id: "1", title: "", logo: null, skills: [] }],
    ["logo a string", { id: 1, logo: "l" }, { id: "1", title: "", logo: null, skills: [] }],
    ["skills null", { id: "r", skills: null }, { id: "r", title: "", logo: null, skills: [] }],
  ])("reads a room with %s", (_name, room, expected) => {
    expect(parseSkillsCatalog({ rooms: [room] })).toEqual([expected]);
  });

  it.each([
    ["not an object", "skill"],
    ["id missing", { name: "n" }],
    ["id null", { id: null, name: "n" }],
    ["name missing", { id: "1" }],
    ["name empty", { id: "1", name: "" }],
    ["name a number", { id: "1", name: 3 }],
  ])("drops a skill with %s", (_name, skill) => {
    expect(parseSkillsCatalog({ rooms: [{ id: 1, skills: [skill] }] })[0]?.skills).toEqual([]);
  });

  it.each([
    [
      "a numeric id",
      { id: 9, name: "n", description: "d" },
      { id: "9", name: "n", description: "d" },
    ],
    ["description missing", { id: "1", name: "n" }, { id: "1", name: "n", description: "" }],
    [
      "description null",
      { id: "1", name: "n", description: null },
      { id: "1", name: "n", description: "" },
    ],
  ])("reads a skill with %s", (_name, skill, expected) => {
    expect(parseSkillsCatalog({ rooms: [{ id: 1, skills: [skill] }] })[0]?.skills).toEqual([
      expected,
    ]);
  });
});

describe("getRoomsSkills", () => {
  it("asks once per distinct room, as a repeated roomIds parameter, with the caller's credentials", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms/skills", () => envelope(skillsDto())));

    const result = await inRequestContext({ cookie: "asc_auth_key=abc" }, async () => ({
      skills: await getRoomsSkills(["17001", "17001", "8"]),
      reads: getFilesApiReadCount(),
    }));

    expect(result.reads).toBe(1);
    expect([...result.skills.keys()]).toEqual(["17001"]);
    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/rooms/skills?roomIds=17001&roomIds=8");
    expect(upstream.seen[0]?.headers["cookie"]).toBe("asc_auth_key=abc");
  });

  it("asks nothing for no room", async () => {
    await expect(getRoomsSkills([])).resolves.toEqual(new Map());
    expect(upstream.seen).toEqual([]);
  });

  it("rethrows a refusal of the catalog", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms/skills", () => new HttpResponse(null, { status: 403 })),
    );
    const error = await getRoomsSkills(["1"]).catch((e: unknown) => e);
    expect(error).toBeInstanceOf(DocspaceApiHttpError);
    expect((error as DocspaceApiHttpError).status).toBe(403);
  });
});

describe("the rooms listings", () => {
  /** A page of the rooms listing: `{ response: { folders: FolderDto[], total } }`. */
  const page = (folders: unknown[], total?: number) =>
    HttpResponse.json({ response: total === undefined ? { folders } : { folders, total } });

  it("pages through the Instructions rooms of the AI rooms root and offers the ones with a skill", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms", ({ request }) => {
        const start = Number(new URL(request.url).searchParams.get("startIndex"));
        return start === 0
          ? page(
              [
                { id: 17001, title: "Brand voice" },
                { id: 8, title: "Empty" },
              ],
              101,
            )
          : page([{ id: 9, title: "Last" }], 101);
      }),
      http.get("*/api/2.0/files/rooms/skills", () => envelope(skillsDto())),
    );

    await expect(getContextFolders()).resolves.toEqual([
      { cloud: "docspace", rooms: [{ id: "17001", name: "Brand voice" }] },
    ]);
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/api/2.0/files/rooms?type=10&searchArea=AiHub&count=100&startIndex=0",
      "/api/2.0/files/rooms?type=10&searchArea=AiHub&count=100&startIndex=100",
      "/api/2.0/files/rooms/skills?roomIds=17001&roomIds=8&roomIds=9",
    ]);
  });

  it("offers nothing and reads no catalog when there is no Instructions room", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms", () => page([], 0)));
    await expect(getContextFolders()).resolves.toEqual([]);
    expect(upstream.seen).toHaveLength(1);
  });

  it("offers nothing when no Instructions room holds a skill", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms", () => page([{ id: 8, title: "Empty" }])),
      http.get("*/api/2.0/files/rooms/skills", () => envelope({ rooms: [{ id: 8, skills: [] }] })),
    );
    await expect(getContextFolders()).resolves.toEqual([]);
  });

  it("drops a listed room without id or title and reads a missing total as the page's length", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms", () =>
        page([
          { id: 1, title: "" },
          { title: "No id" },
          { id: 2 },
          "x",
          { id: "3", title: "Three" },
        ]),
      ),
    );
    await expect(getKnowledgeFolders()).resolves.toEqual([
      { cloud: "docspace", rooms: [{ id: "3", name: "Three" }] },
    ]);
    expect(upstream.seen).toHaveLength(1);
  });

  it.each([
    ["a list", () => HttpResponse.json([])],
    ["text", () => HttpResponse.json("rooms")],
    ["an object without an envelope", () => HttpResponse.json({ folders: [] })],
  ])("rejects a listing that is %s instead of reading it as no room", async (_name, reply) => {
    upstream.use(http.get("*/api/2.0/files/rooms", reply));
    await expect(getKnowledgeFolders()).rejects.toEqual(new UpstreamShapeError("GET files/rooms"));
  });

  // FolderContentDto carries `folders`: a page without the list is unknown, not a page of no room.
  it.each([
    ["whose envelope is null", { response: null }],
    ["whose envelope is a string", { response: "x" }],
    ["without folders", { response: { total: 0 } }],
    ["whose folders are not a list", { response: { folders: {}, total: 0 } }],
  ])("rejects a first page %s instead of reading it as no room", async (_name, body) => {
    upstream.use(http.get("*/api/2.0/files/rooms", () => HttpResponse.json(body)));
    await expect(getContextFolders()).rejects.toEqual(new UpstreamShapeError("GET files/rooms"));
    expect(upstream.seen).toHaveLength(1);
  });

  it("reads at most ten pages of a runaway total", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms", () => page([{ id: 1, title: "R" }], 1_000_000)));
    const folders = await getKnowledgeFolders();
    expect(folders[0]?.rooms).toHaveLength(10);
    expect(upstream.seen).toHaveLength(10);
  });

  it("answers no Knowledge folder when there is none", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms", () => page([])));
    await expect(getKnowledgeFolders()).resolves.toEqual([]);
  });

  it("rejects a listing with a later page whose envelope is null, not keeping the pages before it", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms", ({ request }) => {
        const start = new URL(request.url).searchParams.get("startIndex");
        return start === "100"
          ? HttpResponse.json({ response: null })
          : page([{ id: Number(start) + 1, title: `R${start}` }], 300);
      }),
    );
    await expect(getKnowledgeFolders()).rejects.toEqual(new UpstreamShapeError("GET files/rooms"));
  });

  it("rejects when a later page has no envelope", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms", ({ request }) =>
        new URL(request.url).searchParams.get("startIndex") === "0"
          ? page([{ id: 1, title: "R" }], 500)
          : HttpResponse.json({ folders: [] }),
      ),
    );
    await expect(getKnowledgeFolders()).rejects.toEqual(new UpstreamShapeError("GET files/rooms"));
  });

  it("reads the pages after the first at once and joins them in page order", async () => {
    const started: number[] = [];
    const arrived: number[] = [];
    const gate = (): { promise: Promise<void>; open: () => void } => {
      let open: () => void = () => undefined;
      const promise = new Promise<void>((resolve) => {
        open = resolve;
      });
      return { promise, open };
    };
    // Page 300 is answered once all three were asked for; page 200 once the client has read
    // page 300, page 100 once it has read page 200: they arrive in reverse order, every run.
    const allStarted = gate();
    const read = new Map([
      [300, gate()],
      [200, gate()],
    ]);
    const after = new Map([
      [300, allStarted.promise],
      [200, read.get(300)?.promise],
      [100, read.get(200)?.promise],
    ]);
    // The client's own read of each answer, as it completes (filesApiGet reads it with json()).
    const json = Response.prototype.json;
    vi.spyOn(Response.prototype, "json").mockImplementation(async function (this: Response) {
      const body: unknown = await json.call(this);
      const start = Number(new URL(this.url).searchParams.get("startIndex"));
      if (start > 0) {
        arrived.push(start);
        read.get(start)?.open();
      }
      return body;
    });
    upstream.use(
      http.get("*/api/2.0/files/rooms", async ({ request }) => {
        const start = Number(new URL(request.url).searchParams.get("startIndex"));
        if (start > 0) {
          started.push(start);
          if (started.length === 3) {
            allStarted.open();
          }
          await after.get(start);
        }
        return page([{ id: start + 1, title: `R${start}` }], 350);
      }),
    );

    const folders = await getKnowledgeFolders();

    expect(arrived).toEqual([300, 200, 100]);
    expect(folders[0]?.rooms.map((room) => room.id)).toEqual(["1", "101", "201", "301"]);
    expect(started.sort()).toEqual([100, 200, 300]);
  });

  it("reads the first page alone when it tells no total beyond itself", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms", () => page([{ id: 1, title: "R" }], 100)));
    await getKnowledgeFolders();
    expect(upstream.seen).toHaveLength(1);
  });

  it.each([401, 403, 500])(
    "rethrows a %i of the listing as a DocspaceApiHttpError",
    async (status) => {
      upstream.use(http.get("*/api/2.0/files/rooms", () => new HttpResponse(null, { status })));
      const error = await getContextFolders().catch((e: unknown) => e);
      expect(error).toBeInstanceOf(DocspaceApiHttpError);
      expect((error as DocspaceApiHttpError).status).toBe(status);
    },
  );

  it("counts each page as a Files API read of the request", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms", () => page([])));
    await expect(
      inRequestContext({}, async () => {
        await getKnowledgeFolders();
        return getFilesApiReadCount();
      }),
    ).resolves.toBe(1);
  });
});

describe("throughProxy", () => {
  it.each([
    [
      "https://portal.example.com/storage/f?k=1",
      "http://proxy:8092",
      "http://proxy:8092/storage/f?k=1",
    ],
    ["HTTP://portal/a/b", "http://proxy:8092/", "http://proxy:8092/a/b"],
    ["/storage/f?k=1", "http://proxy:8092", "http://proxy:8092/storage/f?k=1"],
    ["storage/f", "http://proxy:8092/base", "http://proxy:8092/base/storage/f"],
    ["https://portal/x#frag", "http://proxy", "http://proxy/x"],
  ])("sends %s through %s as %s", (url, base, expected) => {
    expect(throughProxy(url, base)).toBe(expected);
  });

  it("uses the proxy this service reaches the portal through by default", () => {
    expect(throughProxy("/x")).toBe(new URL("/x", `${proxyBaseUrl}/`).toString());
  });
});

describe("presignedDownloadUrl", () => {
  it("answers the address the portal mints", async () => {
    upstream.use(
      http.get("*/api/2.0/files/file/:id/presigneduri", () => envelope("https://portal/storage/f")),
    );
    await expect(presignedDownloadUrl("a/1")).resolves.toBe("https://portal/storage/f");
    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/file/a%2F1/presigneduri");
  });

  it.each([
    ["an empty address", () => envelope("")],
    ["an address that is not text", () => envelope({ url: "u" })],
  ])("throws on %s", async (_name, answer) => {
    upstream.use(http.get("*/api/2.0/files/file/:id/presigneduri", answer));
    await expect(presignedDownloadUrl("5")).rejects.toEqual(
      new UpstreamShapeError("GET files/file/{id}/presigneduri"),
    );
  });

  it("throws on an answer without an envelope", async () => {
    upstream.use(
      http.get("*/api/2.0/files/file/:id/presigneduri", () =>
        HttpResponse.json("https://portal/f"),
      ),
    );
    await expect(presignedDownloadUrl("5")).rejects.toEqual(
      new UpstreamShapeError("GET files/file/5/presigneduri"),
    );
  });

  it("throws a DocspaceApiHttpError for a refusal", async () => {
    upstream.use(
      http.get(
        "*/api/2.0/files/file/:id/presigneduri",
        () => new HttpResponse(null, { status: 403 }),
      ),
    );
    await expect(presignedDownloadUrl("5")).rejects.toMatchObject({ status: 403 });
  });
});

/**
 * A streamed body. The download readers cancel the rest of a body they stop reading; msw only
 * settles that cancel for a streamed body, so the tests that stop early stream theirs.
 */
function streamOf(...chunks: (string | Uint8Array)[]): ReadableStream<Uint8Array> {
  return new ReadableStream({
    start(controller) {
      for (const chunk of chunks) {
        controller.enqueue(typeof chunk === "string" ? new TextEncoder().encode(chunk) : chunk);
      }
      controller.close();
    },
  });
}

describe("readFileText and readFileBytes", () => {
  function minted(address = "https://portal.example.com/storage/files/5?k=1") {
    upstream.use(http.get("*/api/2.0/files/file/:id/presigneduri", () => envelope(address)));
  }

  it("downloads the file through the proxy without the caller's conditional headers", async () => {
    minted();
    upstream.use(http.get("*/storage/files/5", () => new HttpResponse("# Tone\nBe brief.")));

    const text = await inRequestContext(
      { cookie: "c=1", "if-none-match": '"etag"', range: "bytes=0-1", "If-Modified-Since": "x" },
      () => readFileText("5"),
    );

    expect(text).toBe("# Tone\nBe brief.");
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/api/2.0/files/file/5/presigneduri",
      "/storage/files/5?k=1",
    ]);
    expect(upstream.seen[0]?.headers["if-none-match"]).toBe('"etag"');
    expect(upstream.seen[1]?.headers["cookie"]).toBe("c=1");
    expect(upstream.seen[1]?.headers).not.toHaveProperty("if-none-match");
    expect(upstream.seen[1]?.headers).not.toHaveProperty("range");
    expect(upstream.seen[1]?.headers).not.toHaveProperty("if-modified-since");
  });

  it("reads at most 256 KiB of a skill file", async () => {
    minted();
    upstream.use(
      http.get(
        "*/storage/files/5",
        () => new HttpResponse(streamOf("a".repeat(150 * 1024), "b".repeat(150 * 1024))),
      ),
    );
    const text = await readFileText("5");
    expect(text).toHaveLength(256 * 1024);
    expect(text.endsWith("b".repeat(106 * 1024))).toBe(true);
  });

  it("reads a download without a body as empty text", async () => {
    minted();
    upstream.use(http.get("*/storage/files/5", () => new HttpResponse(null, { status: 200 })));
    await expect(readFileText("5")).resolves.toBe("");
  });

  it("throws a DocspaceApiHttpError for a refused download", async () => {
    minted();
    upstream.use(http.get("*/storage/files/5", () => new HttpResponse(null, { status: 404 })));
    await expect(readFileText("5")).rejects.toMatchObject({ status: 404 });
    await expect(readFileBytes("5", 10)).rejects.toMatchObject({ status: 404 });
  });

  it("reads the bytes of a file within the limit", async () => {
    minted("/storage/files/5");
    upstream.use(http.get("*/storage/files/5", () => new HttpResponse(new Uint8Array([1, 2, 3]))));
    expect(Array.from(await readFileBytes("5", 3))).toEqual([1, 2, 3]);
  });

  // The declared-length refusal cancels the body before reading it, a cancel msw never settles:
  // it is pinned on a plain Response in readBodyCapped below.
  it("names the file and the limit in FileTooLargeError", () => {
    expect(new FileTooLargeError("5", 4)).toMatchObject({
      name: "FileTooLargeError",
      message: "file 5 is over the limit of 4 bytes",
    });
  });

  it("throws FileTooLargeError for a file that proves larger than the limit", async () => {
    minted();
    upstream.use(
      http.get(
        "*/storage/files/5",
        () => new HttpResponse(streamOf(new Uint8Array(3), new Uint8Array(3))),
      ),
    );
    await expect(readFileBytes("5", 4)).rejects.toBeInstanceOf(FileTooLargeError);
  });
});

describe("readBodyCapped", () => {
  it("answers null for a body that declares more than the limit", async () => {
    const res = new Response(new Uint8Array(5), { headers: { "content-length": "5" } });
    expect(await readBodyCapped(res, 4)).toBeNull();
  });

  it("answers no bytes for a response without a body", async () => {
    expect(await readBodyCapped(new Response(null), 3)).toEqual(new Uint8Array(0));
  });

  it("reads a non-numeric declared length by the bytes received", async () => {
    const res = new Response(new Uint8Array([1, 2]), { headers: { "content-length": "x" } });
    expect(Array.from((await readBodyCapped(res, 2)) ?? [])).toEqual([1, 2]);
  });
});

describe("downloadHeaders", () => {
  it("is empty outside a request context", () => {
    expect(downloadHeaders()).toEqual({});
  });
});

describe("getRoomSkills and getRoomSkill", () => {
  it("answers the skills of an Instructions room from the catalog", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms/skills", () => envelope(skillsDto())));
    await expect(getRoomSkills("17001")).resolves.toEqual([
      { id: "5001", name: "tone", description: "Write in the brand's tone." },
      { id: "5002", name: "glossary", description: "" },
    ]);
  });

  it("answers 404 naming the catalog path for a room the catalog leaves out", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms/skills", () => envelope({ rooms: [] })));
    const error = await getRoomSkills("a b").catch((e: unknown) => e);
    expect(error).toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toMatchObject({
      status: 404,
      statusText: "Not Found",
      url: "api/2.0/files/rooms/skills?roomIds=a%20b",
    });
  });

  it("reads the body of a skill of the room", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms/skills", () => envelope(skillsDto())),
      http.get("*/api/2.0/files/file/:id/presigneduri", () => envelope("/storage/files/5001")),
      http.get("*/storage/files/5001", () => new HttpResponse("# Tone")),
    );
    await expect(getRoomSkill("17001", "5001")).resolves.toBe("# Tone");
  });

  it("answers an exposed 404 for a file that is not a skill of the room, reading nothing", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms/skills", () => envelope(skillsDto())));
    const error = await getRoomSkill("17001", "9999").catch((e: unknown) => e);
    expect(error).toMatchObject({
      message: "Skill 9999 is not a skill of room 17001",
      status: 404,
      expose: true,
    });
    expect(upstream.seen).toHaveLength(1);
  });
});
