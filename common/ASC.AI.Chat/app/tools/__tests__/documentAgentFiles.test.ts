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

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import request from "supertest";
import { afterAll, describe, expect, it, vi } from "vitest";
import { publishedOperations } from "../../__tests__/helpers/schema.js";
import {
  HttpResponse,
  docspace,
  docspaceError,
  http,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";

// The two routes the document agent calls on this service, through the whole application
// (parsers, request context, 404), with the Files and AI services stubbed at the network.
// The document agent is configured from the environment, which wins over appsettings.

vi.stubEnv("AI__DOCUMENT_AGENT__URL", "http://agent:9838");
vi.stubEnv("AI__DOCUMENT_AGENT__TOKEN", "secret");

const { default: logger } = await import("../../log.js");
const { createApp } = await import("../../createApp.js");
const { DOCUMENT_AGENT_FILES_PATH, DOCUMENT_AGENT_OUTPUTS_PATH, MAX_FILE_BYTES } =
  await import("../documentAgentFiles.js");

const upstream = useUpstream({ localApp: true });
const app = createApp();

// What the app itself sent anywhere; supertest's own requests to it are not listed.
const calledOut = () => upstream.seen;

afterAll(() => {
  vi.unstubAllEnvs();
});

const AGENT = { "x-docagent-token": "secret", Authorization: "Bearer user-session" };
const MCP_ACCEPT = { Accept: "application/json, text/event-stream" };
const THREAD = "0198c3f4-6a2e-7c41-9b8d-2f5e7a1c3d90";

// A FileDto<int> as `GET api/2.0/files/file/{fileId}` answers it
// (products/ASC.Files/Core/ApiModels/ResponseDto/FileDto.cs, FileEntryDto.cs).
const fileDto = {
  id: 30,
  title: "Report.docx",
  folderId: 77,
  version: 1,
  versionGroup: 1,
  contentLength: "12.4 KB",
  pureContentLength: 12700,
  fileExst: ".docx",
  webUrl: "https://portal.example.com/doceditor?fileid=30",
  rootFolderType: 5,
  access: 0,
  shared: false,
  created: "2026-11-20T10:00:00.0000000+00:00",
  updated: "2026-11-20T10:00:00.0000000+00:00",
};

/** The JSON-RPC message of a stream answer of the MCP transport (one `data:` line). */
function rpc(text: string): unknown {
  const line = text.split("\n").find((l) => l.startsWith("data: "));
  return line === undefined ? undefined : JSON.parse(line.slice("data: ".length));
}

function mcp(body: unknown, headers: Record<string, string> = AGENT) {
  return request(app)
    .post(DOCUMENT_AGENT_FILES_PATH)
    .set({ ...MCP_ACCEPT, ...headers })
    .send(body as object);
}

function callLink(fileId: string, headers?: Record<string, string>) {
  return mcp(
    {
      jsonrpc: "2.0",
      id: 7,
      method: "tools/call",
      params: { name: "get_file_link", arguments: { fileId } },
    },
    headers,
  );
}

function readResource(uri: string) {
  return mcp({ jsonrpc: "2.0", id: 8, method: "resources/read", params: { uri } });
}

function deliver(
  query: string,
  headers: Record<string, string> = {},
  body: Buffer | string = Buffer.from("PK-bytes"),
) {
  return request(app)
    .post(`${DOCUMENT_AGENT_OUTPUTS_PATH}${query}`)
    .set({
      ...AGENT,
      "Content-Disposition": 'attachment; filename="Report.docx"',
      ...headers,
    })
    .send(body);
}

describe("the gate of the document agent's routes", () => {
  const routes = [
    ["the MCP server", DOCUMENT_AGENT_FILES_PATH],
    ["the outputs endpoint", `${DOCUMENT_AGENT_OUTPUTS_PATH}?folder=5`],
  ] as const;

  it("is not in the published OpenAPI document", () => {
    expect(publishedOperations().filter((op) => op.path.includes("/document-agent/files"))).toEqual(
      [],
    );
  });

  it.each(routes)("answers 404 on %s when no document agent is configured", async (_what, path) => {
    vi.stubEnv("AI__DOCUMENT_AGENT__TOKEN", "");
    try {
      const reply = await request(app)
        .post(path)
        .set({ ...MCP_ACCEPT, ...AGENT })
        .send({});

      expect(reply.status).toBe(404);
      expect(reply.body).toEqual({ error: "not found" });
      expect(calledOut()).toEqual([]);
    } finally {
      vi.stubEnv("AI__DOCUMENT_AGENT__TOKEN", "secret");
    }
  });

  it.each(routes)(
    "answers 401 on %s without the agent's token and logs where it came from",
    async (_what, path) => {
      const reply = await request(app)
        .post(path)
        .set({ ...MCP_ACCEPT, Authorization: "Bearer user-session" })
        .send({});

      expect(reply.status).toBe(401);
      expect(reply.body).toEqual({ error: "Unauthorized" });
      expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
        expect.stringMatching(/^documentAgentFiles: request without the agent's token from \S+$/),
      );
      expect(calledOut()).toEqual([]);
    },
  );

  it.each([
    ["the same length", "secreT"],
    ["a longer one", "secret-and-more"],
    ["a shorter one", "sec"],
    ["an empty one", ""],
  ])("answers 401 to a token of %s that does not match", async (_what, token) => {
    const reply = await deliver("?folder=5", { "x-docagent-token": token });

    expect(reply.status).toBe(401);
    expect(reply.body).toEqual({ error: "Unauthorized" });
    expect(calledOut()).toEqual([]);
  });

  it.each(routes)("answers 401 on %s to the token without a user session", async (_what, path) => {
    const reply = await request(app)
      .post(path)
      .set({ ...MCP_ACCEPT, "x-docagent-token": "secret" })
      .send({});

    expect(reply.status).toBe(401);
    expect(reply.body).toEqual({ error: "Unauthorized: no user session" });
    expect(calledOut()).toEqual([]);
  });

  it.each([
    ["the auth cookie", { Cookie: "asc_auth_key=abc" }],
    ["the authorization cookie", { Cookie: "other=1; authorization=abc" }],
    ["the Authorization header", { Authorization: "Bearer abc" }],
  ])("admits the token with %s as the session", async (_what, session) => {
    upstream.use(http.post("*/api/2.0/files/:folderId/insert", () => docspace(fileDto)));

    const reply = await deliver("?folder=5", { ...session, "x-docagent-token": "secret" });

    expect(reply.status).toBe(201);
  });

  it("answers a GET of the MCP server 401 without the token, before the method is looked at", async () => {
    const reply = await request(app).get(DOCUMENT_AGENT_FILES_PATH);

    expect(reply.status).toBe(401);
    expect(reply.body).toEqual({ error: "Unauthorized" });
  });

  it("answers a GET of the MCP server 405 with the methods it takes", async () => {
    const reply = await request(app).get(DOCUMENT_AGENT_FILES_PATH).set(AGENT);

    expect(reply.status).toBe(405);
    expect(reply.headers["allow"]).toBe("POST, DELETE");
    expect(reply.body).toEqual({ error: "method not allowed" });
  });

  it.each(["get", "put", "delete"] as const)(
    "answers a %s of the outputs endpoint with the application's 404",
    async (method) => {
      const reply = await request(app)
        [method](`${DOCUMENT_AGENT_OUTPUTS_PATH}?folder=5`)
        .set(AGENT);

      expect(reply.status).toBe(404);
      expect(reply.body).toEqual({ error: "Not Found" });
    },
  );
});

describe("the document agent's MCP server", () => {
  it("lists get_file_link as its one tool", async () => {
    const reply = await mcp({ jsonrpc: "2.0", id: 1, method: "tools/list" });

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("text/event-stream");
    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 1,
      result: {
        tools: [
          {
            name: "get_file_link",
            title: "Get a file's download link",
            description:
              "Returns a link to a workspace file by its id. The link brings the file itself into " +
              "your sandbox, as it is in the workspace (docx, xlsx, pptx, pdf, ...), so you can open " +
              "or edit it with Document Builder. Use it when a task is about an existing file; " +
              "`docspace_download_file_as_text` gives only its text.",
            inputSchema: {
              type: "object",
              properties: {
                fileId: { type: "string", minLength: 1, description: "The workspace file id" },
              },
              required: ["fileId"],
              additionalProperties: false,
              $schema: "http://json-schema.org/draft-07/schema#",
            },
            annotations: { readOnlyHint: true, destructiveHint: false, openWorldHint: false },
            execution: { taskSupport: "forbidden" },
          },
        ],
      },
    });
    expect(calledOut()).toEqual([]);
  });

  it("mints a workspace link for a file the user may read, asking the Files API as the user only", async () => {
    upstream.use(http.get("*/api/2.0/files/file/:fileId", () => docspace(fileDto)));

    const reply = await callLink("30", {
      ...AGENT,
      Cookie: "asc_auth_key=abc",
      "x-task-id": "task-1",
      "x-context-id": "ctx-1",
      "mcp-protocol-version": "2025-06-18",
      "Content-Disposition": "inline",
      "x-forwarded-host": "portal.example.com",
    });

    expect(reply.status).toBe(200);
    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 7,
      result: {
        content: [
          {
            type: "resource_link",
            uri: "workspace://files/30",
            name: "Report.docx",
            mimeType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            description: "Workspace file 30",
          },
        ],
      },
    });
    expect(calledOut()).toEqual([
      {
        method: "GET",
        path: "/api/2.0/files/file/30",
        headers: {
          authorization: "Bearer user-session",
          cookie: "asc_auth_key=abc",
          "x-forwarded-host": "portal.example.com",
        },
        body: undefined,
      },
    ]);
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "documentAgentFiles: link for file 30 (Report.docx) minted",
    );
  });

  it("encodes an id with a slash in the uri and in the Files API path", async () => {
    upstream.use(http.get("*/api/2.0/files/file/:fileId", () => docspace(fileDto)));

    const reply = await callLink("box/12");

    expect(
      (rpc(reply.text) as { result: { content: { uri: string }[] } }).result.content[0]!.uri,
    ).toBe("workspace://files/box%2F12");
    expect(calledOut().map((r) => r.path)).toEqual(["/api/2.0/files/file/box%2F12"]);
  });

  it("names a file without a title after its id, as a binary", async () => {
    upstream.use(
      http.get("*/api/2.0/files/file/:fileId", () => docspace({ ...fileDto, title: undefined })),
    );

    const reply = await callLink("30");

    expect((rpc(reply.text) as { result: unknown }).result).toEqual({
      content: [
        {
          type: "resource_link",
          uri: "workspace://files/30",
          name: "file-30",
          mimeType: "application/octet-stream",
          description: "Workspace file 30",
        },
      ],
    });
  });

  it.each([
    ["404", () => docspaceError(404), "file 30 not found"],
    // DEFECT docagent-1: a 200 whose body is no FileDto (no id) is answered "not found" — expected an unknown failure, as for a 5xx (ts-ai-failure-paths.md §1)
    ["a body without an id", () => docspace({ title: "x" }), "file 30 not found"],
    ["403", () => docspaceError(403), "file 30 cannot be read by the current user"],
    ["500", () => docspaceError(500, "db down"), "file 30: the workspace answered 500"],
    ["a dropped connection", () => HttpResponse.error(), "file 30 could not be read"],
  ])(
    "answers a tool error the model reads when the Files API answers %s",
    async (_what, answer, text) => {
      upstream.use(http.get("*/api/2.0/files/file/:fileId", answer));

      const reply = await callLink("30");

      expect(reply.status).toBe(200);
      expect(rpc(reply.text)).toEqual({
        jsonrpc: "2.0",
        id: 7,
        result: { content: [{ type: "text", text }], isError: true },
      });
      expect(reply.text).not.toContain("http");
      expect(reply.text).not.toContain("db down");
    },
  );

  it("answers the SDK's validation error for an empty file id without asking the Files API", async () => {
    const reply = await callLink("");

    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 7,
      result: {
        content: [
          {
            type: "text",
            text:
              "MCP error -32602: Input validation error: Invalid arguments for tool get_file_link: " +
              "String must contain at least 1 character(s) at fileId",
          },
        ],
        isError: true,
      },
    });
    expect(calledOut()).toEqual([]);
  });

  it("reads a workspace file as one blob under the user's session", async () => {
    upstream.use(
      http.get("*/api/2.0/files/file/:fileId", () => docspace(fileDto)),
      http.get("*/api/2.0/files/file/:fileId/presigneduri", () =>
        docspace("https://portal.example.com/storage/files/30.docx?sig=1"),
      ),
      http.get("*/storage/files/30.docx", () => new HttpResponse(new Uint8Array([1, 2, 3]))),
    );

    const reply = await readResource("workspace://files/30");

    expect(reply.status).toBe(200);
    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 8,
      result: {
        contents: [
          {
            uri: "workspace://files/30",
            mimeType: "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            blob: "AQID",
          },
        ],
      },
    });
    expect(
      calledOut().map((r) => [r.path, r.headers["authorization"], r.headers["x-docagent-token"]]),
    ).toEqual([
      ["/api/2.0/files/file/30", "Bearer user-session", undefined],
      ["/api/2.0/files/file/30/presigneduri", "Bearer user-session", undefined],
      ["/storage/files/30.docx?sig=1", "Bearer user-session", undefined],
    ]);
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "documentAgentFiles: file 30 (Report.docx) read, 3 bytes",
    );
  });

  it("decodes the id of the uri and names a file without a title after it", async () => {
    upstream.use(
      http.get("*/api/2.0/files/file/:fileId", () => docspace({ id: 31 })),
      http.get("*/api/2.0/files/file/:fileId/presigneduri", () => docspace("/storage/x")),
      http.get("*/storage/x", () => new HttpResponse(new Uint8Array([]))),
    );

    const reply = await readResource("workspace://files/a%20b");

    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 8,
      result: {
        contents: [
          { uri: "workspace://files/a%20b", mimeType: "application/octet-stream", blob: "" },
        ],
      },
    });
    expect(calledOut()[0]!.path).toBe("/api/2.0/files/file/a%20b");
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "documentAgentFiles: file a b (file-a b) read, 0 bytes",
    );
  });

  const readFailures: [string, Parameters<typeof upstream.use>, string][] = [
    [
      "the file is not found",
      [http.get("*/api/2.0/files/file/:fileId", () => docspaceError(404))],
      "file 30 not found",
    ],
    [
      "the Files API refuses the file",
      [http.get("*/api/2.0/files/file/:fileId", () => docspaceError(403))],
      "file 30 cannot be read by the current user",
    ],
    [
      "the download address is refused",
      [
        http.get("*/api/2.0/files/file/:fileId", () => docspace(fileDto)),
        http.get("*/api/2.0/files/file/:fileId/presigneduri", () => docspaceError(500)),
      ],
      "file 30: the workspace answered 500",
    ],
    [
      "no download address comes back",
      [
        http.get("*/api/2.0/files/file/:fileId", () => docspace(fileDto)),
        http.get("*/api/2.0/files/file/:fileId/presigneduri", () => docspace(null)),
      ],
      "file 30 could not be read",
    ],
    [
      "the file is larger than the limit",
      [
        http.get("*/api/2.0/files/file/:fileId", () => docspace(fileDto)),
        http.get("*/api/2.0/files/file/:fileId/presigneduri", () => docspace("/storage/big")),
        http.get(
          "*/storage/big",
          () =>
            new HttpResponse(
              new ReadableStream({
                start(controller) {
                  controller.enqueue(new Uint8Array(MAX_FILE_BYTES + 1));
                  controller.close();
                },
              }),
            ),
        ),
      ],
      `file 30 is over the limit of ${MAX_FILE_BYTES} bytes`,
    ],
  ];

  it.each(readFailures)(
    "answers a read error naming no address when %s",
    async (_what, handlers, message) => {
      upstream.use(...handlers);

      const reply = await readResource("workspace://files/30");

      expect(reply.status).toBe(200);
      expect(rpc(reply.text)).toEqual({
        jsonrpc: "2.0",
        id: 8,
        error: { code: -32603, message },
      });
      expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
        expect.stringMatching(/^documentAgentFiles: file 30 not read: /),
      );
    },
  );

  it("answers the SDK's not-found error for a uri without a file id", async () => {
    const reply = await readResource("workspace://files/");

    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 8,
      error: { code: -32602, message: "MCP error -32602: Resource workspace://files/ not found" },
    });
    expect(calledOut()).toEqual([]);
  });

  it("answers an unknown method with the protocol's method-not-found error", async () => {
    const reply = await mcp({ jsonrpc: "2.0", id: 1, method: "prompts/list" });

    expect(rpc(reply.text)).toEqual({
      jsonrpc: "2.0",
      id: 1,
      error: { code: -32601, message: "Method not found" },
    });
  });

  it.each([
    ["null", "null"],
    ["a string", '"x"'],
    ["a number", "5"],
    ["{}", "{}"],
    ["a message without a method", '{"jsonrpc":"2.0","id":1}'],
  ])("answers a body of %s with the transport's 400 parse error", async (_what, body) => {
    const reply = await request(app)
      .post(DOCUMENT_AGENT_FILES_PATH)
      .set({ ...MCP_ACCEPT, ...AGENT, "Content-Type": "application/json" })
      .send(body);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({
      jsonrpc: "2.0",
      error: { code: -32700, message: "Parse error: Invalid JSON-RPC message" },
      id: null,
    });
  });

  it("answers an empty batch with 202 and no body", async () => {
    const reply = await mcp([]);

    expect(reply.status).toBe(202);
    expect(reply.text).toBe("");
  });

  // DEFECT docagent-2: a body that is not JSON is answered by the application's `{ error }`, not in the envelope of the protocol — expected the JSON-RPC parse error the transport answers for every other bad body (ts-ai-express.md §1)
  it("answers a body that is not JSON with the application's 400", async () => {
    const reply = await request(app)
      .post(DOCUMENT_AGENT_FILES_PATH)
      .set({ ...MCP_ACCEPT, ...AGENT, "Content-Type": "application/json" })
      .send("{bad");

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "The request body is not valid JSON" });
    expect(reply.text).not.toContain("SyntaxError");
  });

  it("answers 406 to a client that does not take an event stream", async () => {
    const reply = await mcp(
      { jsonrpc: "2.0", id: 1, method: "tools/list" },
      { ...AGENT, Accept: "application/json" },
    );

    expect(reply.status).toBe(406);
    expect(reply.body).toEqual({
      jsonrpc: "2.0",
      error: {
        code: -32000,
        message: "Not Acceptable: Client must accept both application/json and text/event-stream",
      },
      id: null,
    });
  });

  it("answers a DELETE of the stateless session with an empty 200", async () => {
    const reply = await request(app)
      .delete(DOCUMENT_AGENT_FILES_PATH)
      .set({ ...MCP_ACCEPT, ...AGENT });

    expect(reply.status).toBe(200);
    expect(reply.text).toBe("");
  });

  it.each([
    ["an Error", new Error("transport broke"), "transport broke"],
    ["a string", "broke", "broke"],
  ])(
    "answers 500 and logs when the server cannot be connected, with %s",
    async (_what, err, text) => {
      vi.spyOn(McpServer.prototype, "connect").mockRejectedValue(err);

      const reply = await mcp({ jsonrpc: "2.0", id: 1, method: "tools/list" });

      expect(reply.status).toBe(500);
      expect(reply.body).toEqual({ error: "internal error" });
      expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
        `documentAgentFiles: request failed: ${text}`,
      );
    },
  );

  it("keeps the answer already started when the transport fails after it", async () => {
    vi.spyOn(StreamableHTTPServerTransport.prototype, "handleRequest").mockImplementation(
      async (_req, res) => {
        res.writeHead(200, { "content-type": "text/plain" });
        res.end("partial");
        throw new Error("late");
      },
    );

    const reply = await mcp({ jsonrpc: "2.0", id: 1, method: "tools/list" });

    expect(reply.status).toBe(200);
    expect(reply.text).toBe("partial");
    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      "documentAgentFiles: request failed: late",
    );
  });
});

describe("the outputs endpoint", () => {
  function inserted(answer: () => Response = () => docspace({ ...fileDto, id: 901 })) {
    upstream.use(http.post("*/api/2.0/files/:folderId/insert", answer));
  }
  const filePart = (body: unknown) => String(body);

  it("inserts the delivered bytes into the named folder as the user and answers 201 with the editor link", async () => {
    inserted(() => docspace({ ...fileDto, id: 901, title: "Report (1).docx" }));

    const reply = await deliver("?folder=5", {
      "Content-Type":
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document; charset=binary",
      "x-task-id": "task-1",
      "x-context-id": "ctx-1",
      Cookie: "asc_auth_key=abc",
    });

    expect(reply.status).toBe(201);
    expect(reply.headers["location"]).toBe("/doceditor?fileId=901");
    expect(reply.text).toBe("");
    const [insert] = calledOut();
    expect(calledOut()).toHaveLength(1);
    expect(insert!.method).toBe("POST");
    expect(insert!.path).toBe("/api/2.0/files/5/insert");
    expect(insert!.headers).toEqual({
      authorization: "Bearer user-session",
      cookie: "asc_auth_key=abc",
      "content-type": expect.stringMatching(/^multipart\/form-data; boundary=/),
    });
    const body = filePart(insert!.body);
    expect(body).toContain('name="title"\r\n\r\nReport.docx\r\n');
    expect(body).toContain('name="createNewIfExist"\r\n\r\ntrue\r\n');
    expect(body).toContain(
      'name="file"; filename="Report.docx"\r\nContent-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document\r\n\r\nPK-bytes\r\n',
    );
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "documentAgentFiles: Report.docx (8 bytes) saved into folder 5 as file 901 (Report (1).docx)",
    );
  });

  it("logs no second title when the file kept its name", async () => {
    inserted(() => docspace({ ...fileDto, id: 902 }));

    await deliver("?folder=5");

    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "documentAgentFiles: Report.docx (8 bytes) saved into folder 5 as file 902",
    );
  });

  it("keeps the delivered name when the insert answer has no title", async () => {
    inserted(() => docspace({ id: 903 }));

    const reply = await deliver("?folder=5");

    expect(reply.status).toBe(201);
    expect(reply.headers["location"]).toBe("/doceditor?fileId=903");
  });

  it.each([
    ["octet-stream", { "Content-Type": "application/octet-stream" }],
    ["no type", {}],
  ])("takes the media type from the name when the body is sent as %s", async (_what, headers) => {
    inserted();

    await deliver("?folder=5", headers);

    expect(filePart(calledOut()[0]!.body)).toContain(
      "Content-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document\r\n",
    );
  });

  it("saves an empty body as an empty file", async () => {
    inserted();

    const reply = await deliver("?folder=5", {}, Buffer.alloc(0));

    expect(reply.status).toBe(201);
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "documentAgentFiles: Report.docx (0 bytes) saved into folder 5 as file 901",
    );
  });

  // DEFECT docagent-3: a file delivered as application/x-www-form-urlencoded is parsed by the application's urlencoded parser before the handler and saved as an empty file with 201 — expected its bytes saved, as for any other media type (ts-ai-express.md §2: no parser without a reader; createApp.ts keeps only the JSON parser off this path)
  it("saves a file delivered as form-urlencoded with no bytes", async () => {
    inserted();

    const reply = await deliver(
      "?folder=5",
      {
        "Content-Type": "application/x-www-form-urlencoded",
        "Content-Disposition": 'attachment; filename="data.txt"',
      },
      "a=1&b=2",
    );

    expect(reply.status).toBe(201);
    expect(filePart(calledOut()[0]!.body)).toContain(
      'filename="data.txt"\r\nContent-Type: application/x-www-form-urlencoded\r\n\r\n\r\n',
    );
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "documentAgentFiles: data.txt (0 bytes) saved into folder 5 as file 901 (Report.docx)",
    );
  });

  it("keeps a result of an AI project's conversation among its artifacts", async () => {
    upstream.use(
      http.post("*/internal/ai/threads/:threadId/artifacts", () =>
        docspace({ id: 950, title: "Report.docx" }),
      ),
    );

    const reply = await deliver(`?folder=5&thread=${THREAD}`);

    expect(reply.status).toBe(201);
    expect(reply.headers["location"]).toBe("/doceditor?fileId=950");
    expect(calledOut().map((r) => `${r.method} ${r.path}`)).toEqual([
      `POST /internal/ai/threads/${THREAD}/artifacts`,
    ]);
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      `documentAgentFiles: Report.docx (8 bytes) saved among the artifacts of thread ${THREAD} as file 950`,
    );
  });

  it.each([400, 403, 404])(
    "puts the file into the folder when the conversation answers %i",
    async (status) => {
      upstream.use(
        http.post("*/internal/ai/threads/:threadId/artifacts", () => docspaceError(status)),
      );
      inserted();

      const reply = await deliver(`?folder=5&thread=${THREAD}`);

      expect(reply.status).toBe(201);
      expect(reply.headers["location"]).toBe("/doceditor?fileId=901");
      expect(calledOut().map((r) => r.path)).toEqual([
        `/internal/ai/threads/${THREAD}/artifacts`,
        "/api/2.0/files/5/insert",
      ]);
    },
  );

  it.each([
    ["not a UUID", "?folder=5&thread=abc"],
    ["repeated", `?folder=5&thread=${THREAD}&thread=${THREAD}`],
  ])("ignores a thread that is %s", async (_what, query) => {
    inserted();

    const reply = await deliver(query);

    expect(reply.status).toBe(201);
    expect(calledOut().map((r) => r.path)).toEqual(["/api/2.0/files/5/insert"]);
  });

  it.each([
    [
      "the conversation answers 500",
      `?folder=5&thread=${THREAD}`,
      () =>
        upstream.use(
          http.post("*/internal/ai/threads/:threadId/artifacts", () => docspaceError(500, "inner")),
        ),
      502,
      { error: "the workspace answered 500" },
    ],
    [
      "the folder refuses the user",
      "?folder=5",
      () => inserted(() => docspaceError(403, "inner")),
      403,
      { error: "the current user may not save into that folder" },
    ],
    [
      "the Files API answers 500",
      "?folder=5",
      () => inserted(() => docspaceError(500, "inner")),
      502,
      { error: "the workspace answered 500" },
    ],
    [
      "the Files API answers 404",
      "?folder=5",
      () => inserted(() => docspaceError(404, "inner")),
      502,
      { error: "the workspace answered 404" },
    ],
    [
      "the insert answers no id",
      "?folder=5",
      () => inserted(() => docspace({ title: "x" })),
      502,
      { error: "the file could not be saved" },
    ],
    [
      "the insert answers no object",
      "?folder=5",
      () => inserted(() => docspace("x")),
      502,
      { error: "the file could not be saved" },
    ],
    [
      "the connection drops",
      "?folder=5",
      () => inserted(() => HttpResponse.error()),
      502,
      { error: "the file could not be saved" },
    ],
  ])("answers when %s", async (_what, query, arrange, status, body) => {
    arrange();

    const reply = await deliver(query);

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual(body);
    expect(reply.headers["location"]).toBeUndefined();
    expect(reply.text).not.toContain("inner");
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      expect.stringMatching(/^documentAgentFiles: Report\.docx not saved into folder 5: /),
    );
  });

  it.each([
    ["missing", ""],
    ["empty", "?folder="],
    ["repeated", "?folder=5&folder=6"],
  ])("answers 400 when the folder is %s, before reading the file", async (_what, query) => {
    const reply = await deliver(query);

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "the folder to save into is not named" });
    expect(calledOut()).toEqual([]);
  });

  it.each([
    ["no Content-Disposition", undefined],
    ["no filename", "attachment"],
    ["an empty quoted filename", 'attachment; filename=""'],
    ["a blank filename", "attachment; filename=   "],
    ["a name that is only a path", 'attachment; filename="dir/"'],
    ["a malformed extended name and no plain one", "attachment; filename*=UTF-8''%E0%A4%A"],
  ])("answers 400 to a file with %s", async (_what, disposition) => {
    const headers: Record<string, string> = { ...AGENT };
    const req = request(app).post(`${DOCUMENT_AGENT_OUTPUTS_PATH}?folder=5`).set(headers);
    if (disposition !== undefined) {
      void req.set("Content-Disposition", disposition);
    }

    const reply = await req.send(Buffer.from("x"));

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "the file has no name" });
    expect(calledOut()).toEqual([]);
  });

  it.each([
    [
      "the extended name first",
      "attachment; filename=\"Otchet.docx\"; filename*=UTF-8''%D0%9E%D1%82%D1%87%D1%91%D1%82.docx",
      "Отчёт.docx",
    ],
    ["a lower-case charset", "attachment; filename*=utf-8''Plan%20B.xlsx", "Plan B.xlsx"],
    [
      "the plain name when the extended one is malformed",
      "attachment; filename*=UTF-8''%E0%A4%A; filename=\"fallback.pdf\"",
      "fallback.pdf",
    ],
    ["an unquoted name", "attachment; filename=notes.md; size=3", "notes.md"],
    ["the last segment of a path", 'attachment; filename="../../etc/passwd.txt"', "passwd.txt"],
    ["the last segment of a Windows path", 'attachment; filename="C:\\\\tmp\\\\a.csv"', "a.csv"],
    ["a trimmed name", 'attachment; filename="  spaced.docx  "', "spaced.docx"],
  ])("takes %s", async (_what, disposition, name) => {
    inserted();

    const reply = await deliver("?folder=5", { "Content-Disposition": disposition });

    expect(reply.status).toBe(201);
    expect(filePart(calledOut()[0]!.body)).toContain(`name="title"\r\n\r\n${name}\r\n`);
  });

  it("answers 413 to a declared length over the limit without reading the body", async () => {
    const reply = await deliver(
      "?folder=5",
      { "Content-Length": String(MAX_FILE_BYTES + 1) },
      Buffer.from("abc"),
    );

    expect(reply.status).toBe(413);
    expect(reply.body).toEqual({ error: `the file is over the limit of ${MAX_FILE_BYTES} bytes` });
    expect(calledOut()).toEqual([]);
  });
});
