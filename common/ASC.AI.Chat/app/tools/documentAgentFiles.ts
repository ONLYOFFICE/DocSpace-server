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

// A small MCP server of this service for the document agent, with one tool
// and one resource. `get_file_link` turns a workspace file id into an MCP
// `resource_link` with a `workspace://files/<id>` uri; the agent reads such a
// link back from this server with `resources/read`, in the same session, and
// the file lands in its sandbox, which Document Builder can then open. So a
// task can edit an existing document or fill a template. The bytes come from
// the Files API's pre-signed download address, fetched here under the user's
// session: fetched without one, as the agent would fetch an http link, the
// portal takes the request for Document Server's and wants its JWT (403).
//
// The gate is the shared token of the document agent configuration plus the
// user's session, both of which this service hands the agent with the
// server (see documentAgent.ts collectServers). The path sits outside the
// API prefix, which is all the portal's router forwards to this service,
// and the Docker install publishes no port of it, so in practice only the
// internal network sees it; the token is what is relied on. One MCP session
// per request (the SDK's stateless mode): nothing is kept between calls, and
// a GET, which would open a server-to-client stream this server has no use
// for, is answered 405 as the protocol allows.

import { timingSafeEqual } from "crypto";
import { McpServer, ResourceTemplate } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import type { Application, Request, Response } from "express";
import { z } from "zod";
import { getDocumentAgent } from "../../config/index.js";
import type { DocumentAgentConfig } from "../../config/index.js";
import { getForwardedHeaders } from "../requestContext.js";
import { DocspaceApiHttpError, getFileInfo } from "../storage/docspaceFilesApi.js";
import { mediaTypeOf } from "../storage/docspaceFileTransfer.js";
import { FileTooLargeError, readFileBytes } from "../storage/roomSkills.js";
import { authorizationValue } from "./systemTools.js";
import logger from "../log.js";

/** The server's name in the agent's `mcpServers`: its tools reach the model as `workspace_*`. */
export const DOCUMENT_AGENT_FILES_SERVER = "workspace";
/** Where this service serves the server; the agent reaches it under `filesUrl`. */
export const DOCUMENT_AGENT_FILES_PATH = "/document-agent/files/mcp";
/** The header the agent presents the shared token in; `Authorization` is left to the user's session. */
export const DOCUMENT_AGENT_TOKEN_HEADER = "x-docagent-token";

export const GET_FILE_LINK_TOOL = "get_file_link";

/** The most a workspace file read for the agent may weigh; the agent's own `maxFileBytes` too. */
export const MAX_FILE_BYTES = 32 * 1024 * 1024;

// The uri of a workspace file as the agent sees it. Not http(s) on purpose:
// the agent reads any other scheme from the server that handed it over,
// which keeps the user's session on the read.
const FILE_URI_SCHEME = "workspace";
const FILE_URI_TEMPLATE = `${FILE_URI_SCHEME}://files/{fileId}`;

// Headers of the agent's own request to this server, which describe that
// request and not the user: kept from the Files API calls made on its
// behalf. The token above all, a secret of this service and the agent that
// the portal has no business seeing.
const NOT_FOR_THE_PORTAL = [DOCUMENT_AGENT_TOKEN_HEADER, "accept", "mcp-protocol-version"];

export function registerDocumentAgentFiles(app: Application): void {
  app.all(DOCUMENT_AGENT_FILES_PATH, (req, res) => {
    void serve(req, res);
  });
}

async function serve(req: Request, res: Response): Promise<void> {
  const config = getDocumentAgent();
  if (!config) {
    res.status(404).json({ error: "not found" });
    return;
  }
  if (!presentsToken(req, config)) {
    logger.warn(`documentAgentFiles: request without the agent's token from ${req.ip ?? "?"}`);
    res.status(401).json({ error: "Unauthorized" });
    return;
  }
  const forwarded = getForwardedHeaders();
  if (authorizationValue(forwarded) === undefined) {
    res.status(401).json({ error: "Unauthorized: no user session" });
    return;
  }
  if (req.method === "GET") {
    res.setHeader("Allow", "POST, DELETE");
    res.status(405).json({ error: "method not allowed" });
    return;
  }
  for (const name of NOT_FOR_THE_PORTAL) {
    delete forwarded[name];
  }

  const server = new McpServer({ name: "docspace-ai-chat", version: "1.0.0" });
  server.registerTool(
    GET_FILE_LINK_TOOL,
    {
      title: "Get a file's download link",
      description:
        "Returns a link to a workspace file by its id. The link brings the file itself into " +
        "your sandbox, as it is in the workspace (docx, xlsx, pptx, pdf, ...), so you can open " +
        "or edit it with Document Builder. Use it when a task is about an existing file; " +
        "`docspace_download_file_as_text` gives only its text.",
      inputSchema: { fileId: z.string().min(1).describe("The workspace file id") },
      annotations: { readOnlyHint: true, destructiveHint: false, openWorldHint: false },
    },
    async ({ fileId }) => fileLink(fileId),
  );
  server.registerResource(
    "workspace-file",
    new ResourceTemplate(FILE_URI_TEMPLATE, { list: undefined }),
    {
      title: "Workspace file",
      description: "A workspace file, as it is, by the id in its uri.",
    },
    async (uri, { fileId }) => readFile(uri, Array.isArray(fileId) ? fileId[0] : fileId),
  );
  const transport = new StreamableHTTPServerTransport({ sessionIdGenerator: undefined });
  res.on("close", () => {
    void server.close();
  });
  try {
    await server.connect(transport);
    await transport.handleRequest(req, res, req.body);
  } catch (err) {
    logger.error(
      `documentAgentFiles: request failed: ${err instanceof Error ? err.message : String(err)}`,
    );
    if (!res.headersSent) {
      res.status(500).json({ error: "internal error" });
    }
  }
}

function presentsToken(req: Request, config: DocumentAgentConfig): boolean {
  const given = req.header(DOCUMENT_AGENT_TOKEN_HEADER);
  if (typeof given !== "string") {
    return false;
  }
  const a = Buffer.from(given);
  const b = Buffer.from(config.token);
  return a.length === b.length && timingSafeEqual(a, b);
}

// The tool: the file's title for the name the agent will save it under, and
// the uri the agent reads it back by. The file is looked up here so that a
// missing or unreadable one is a result the model reads, never a throw, and
// names no address of ours.
async function fileLink(fileId: string) {
  try {
    const info = await getFileInfo(fileId);
    if (!info) {
      return failure(`file ${fileId} not found`);
    }
    const title = info.title ?? `file-${fileId}`;
    logger.info(`documentAgentFiles: link for file ${fileId} (${title}) minted`);
    return {
      content: [
        {
          type: "resource_link" as const,
          uri: `${FILE_URI_SCHEME}://files/${encodeURIComponent(fileId)}`,
          name: title,
          mimeType: mediaTypeOf(title),
          description: `Workspace file ${fileId}`,
        },
      ],
    };
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    logger.warn(`documentAgentFiles: no link for file ${fileId}: ${message}`);
    return failure(unreadable(fileId, err));
  }
}

// The resource: the file's bytes under the user's session, as one blob. A
// failure is thrown: the SDK answers the read with an error the agent shows
// the model as the reason the file was not saved.
async function readFile(uri: URL, fileId: string | undefined) {
  const id = fileId === undefined ? "" : decodeURIComponent(fileId);
  if (id === "") {
    throw new Error("the uri names no file");
  }
  try {
    const info = await getFileInfo(id);
    if (!info) {
      throw new Error(`file ${id} not found`);
    }
    const title = info.title ?? `file-${id}`;
    const bytes = await readFileBytes(id, MAX_FILE_BYTES);
    logger.info(`documentAgentFiles: file ${id} (${title}) read, ${bytes.byteLength} bytes`);
    return {
      contents: [
        {
          uri: uri.href,
          mimeType: mediaTypeOf(title),
          blob: Buffer.from(bytes.buffer, bytes.byteOffset, bytes.byteLength).toString("base64"),
        },
      ],
    };
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    logger.warn(`documentAgentFiles: file ${id} not read: ${message}`);
    throw new Error(unreadable(id, err), { cause: err });
  }
}

/** Why a file could not be read, for the model: its status, never our addresses. */
function unreadable(fileId: string, err: unknown): string {
  if (err instanceof DocspaceApiHttpError) {
    return err.status === 403
      ? `file ${fileId} cannot be read by the current user`
      : `file ${fileId}: the workspace answered ${err.status}`;
  }
  if (err instanceof FileTooLargeError) {
    return err.message;
  }
  if (err instanceof Error && err.message.startsWith(`file ${fileId}`)) {
    return err.message;
  }
  return `file ${fileId} could not be read`;
}

function failure(text: string) {
  return { content: [{ type: "text" as const, text }], isError: true };
}
