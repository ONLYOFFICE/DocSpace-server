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

import { randomUUID } from "node:crypto";

import date from "date-and-time";
import { aiService, AiServiceHttpError, proxyBaseUrl, withTimeout } from "./httpClient.js";
import {
  getFolderInfo,
  getAgentResultStorageId,
  getMyDocumentsFolderId,
  canTakeUpload,
} from "./docspaceFilesApi.js";
import { getForwardedHeaders } from "../requestContext.js";
import { getNumber, getString, isObject } from "../narrow.js";
import { getOnlyofficeFileType } from "./onlyofficeFileType.js";
import logger from "../log.js";
import type { AttachmentsStorage, Attachment } from "@onlyoffice/ai-chat/core";

const PATH = "/attachments";

// DocSpace pre-signed URLs come back as host-relative paths
// (`/storage/files/...`). `fetch()` in Node refuses relative URLs, so
// resolve them against the DocSpace portal root (same host the AI service
// is proxied through).
function resolveAbsoluteUrl(url: string): string {
  if (/^https?:\/\//i.test(url)) {
    return url;
  }
  return new URL(url, proxyBaseUrl.endsWith("/") ? proxyBaseUrl : `${proxyBaseUrl}/`).toString();
}

// Quick mime sniff by magic bytes; falls back to the `Content-Type` response
// header. Providers reject `data:application/octet-stream;…` for image_url so
// we want a real image/* mime when possible.
function detectImageMime(bytes: Uint8Array, fallback: string | null): string {
  if (
    bytes.length >= 8 &&
    bytes[0] === 0x89 &&
    bytes[1] === 0x50 &&
    bytes[2] === 0x4e &&
    bytes[3] === 0x47
  ) {
    return "image/png";
  }
  if (bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) {
    return "image/jpeg";
  }
  if (
    bytes.length >= 4 &&
    bytes[0] === 0x47 &&
    bytes[1] === 0x49 &&
    bytes[2] === 0x46 &&
    bytes[3] === 0x38
  ) {
    return "image/gif";
  }
  if (
    bytes.length >= 12 &&
    bytes[0] === 0x52 &&
    bytes[1] === 0x49 &&
    bytes[2] === 0x46 &&
    bytes[3] === 0x46 &&
    bytes[8] === 0x57 &&
    bytes[9] === 0x45 &&
    bytes[10] === 0x42 &&
    bytes[11] === 0x50
  ) {
    return "image/webp";
  }
  if (fallback && fallback.startsWith("image/")) {
    return fallback;
  }
  return "image/png";
}

// Decode a tool-image payload into raw bytes + mime. The model returns either
// a bare base64 string or a `data:image/*;base64,…` data URL depending on the
// provider; handle both and sniff the mime from magic bytes when absent.
function decodeImagePayload(base64: string): { bytes: Uint8Array; mime: string } {
  const match = /^data:([^;]+);base64,(.*)$/s.exec(base64);
  const data = match ? (match[2] ?? "") : base64;
  const bytes = new Uint8Array(Buffer.from(data, "base64"));
  const mime = match?.[1] ?? detectImageMime(bytes, null);
  return { bytes, mime };
}

function extensionForMime(mime: string): string {
  switch (mime) {
    case "image/jpeg":
      return ".jpg";
    case "image/gif":
      return ".gif";
    case "image/webp":
      return ".webp";
    default:
      return ".png";
  }
}

// Upload a tool-generated image into the chat's `entityId` folder using the
// public DocSpace Files API (`POST api/2.0/files/{folderId}/insert`), acting on
// behalf of the current user (forwarded auth cookies). Returns the new entry id
// (internal int or thirdparty string, serialized as string).
async function insertGeneratedImage(folderId: string, base64: string): Promise<string> {
  const { bytes, mime } = decodeImagePayload(base64);
  // The engine only ever passes the tool name ("generate_image") as the
  // title, so a timestamp is the most meaningful name we can produce here.
  // `createNewIfExist` still dedupes same-second collisions server-side.
  const stamp = date.format(new Date(), "YYYY-MM-DD_HH-mm-ss");
  const fileName = `generated_image_${stamp}${extensionForMime(mime)}`;

  const form = new FormData();
  form.append("title", fileName);
  form.append("createNewIfExist", "true");
  form.append("file", new Blob([bytes], { type: mime }), fileName);

  const url = `${proxyBaseUrl}/api/2.0/files/${encodeURIComponent(folderId)}/insert`;
  // Don't set Content-Type — `fetch` derives the multipart boundary from the
  // FormData body, and `getForwardedHeaders` strips content-type anyway.
  const headers = getForwardedHeaders();
  // Diagnostics for a failing generated-image upload: the request as it
  // actually goes out. Header NAMES only (a value would leak the session
  // cookie) — what matters is whether `cookie` / `authorization` is there at
  // all, since a lost request context uploads anonymously and answers 401.
  logger.info(
    `insertGeneratedImage: POST ${url} title=${fileName} mime=${mime} bytes=${bytes.length} ` +
      `auth=[${Object.keys(headers).filter((h) => h === "cookie" || h === "authorization").join(",") || "NONE"}] ` +
      `forwarded=[${Object.keys(headers).sort().join(",")}]`,
  );
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, {
      method: "POST",
      headers,
      body: form,
      signal,
    });
    if (!res.ok) {
      const text = await res.text().catch(() => "");
      logger.error(
        `insertGeneratedImage: POST ${url} -> ${res.status} ${res.statusText}; ` +
          `body=${text.slice(0, 1000)}`,
      );
      throw new AiServiceHttpError(res.status, res.statusText, text, url);
    }
    const json: unknown = await res.json();
    const file = isObject(json) && "response" in json ? json.response : json;
    if (!isObject(file)) {
      logger.error(
        `insertGeneratedImage: POST ${url} -> 200 but unexpected payload=${JSON.stringify(json).slice(0, 1000)}`,
      );
      throw new Error("DocSpace insert returned an unexpected payload");
    }
    const numId = getNumber(file, "id");
    const id = numId !== undefined ? String(numId) : getString(file, "id");
    if (id === undefined) {
      logger.error(
        `insertGeneratedImage: POST ${url} -> 200 but no file id in payload=${JSON.stringify(file).slice(0, 1000)}`,
      );
      throw new Error("DocSpace insert returned a file without an id");
    }
    logger.info(
      `insertGeneratedImage: POST ${url} -> 200 entryId=${id} title=${getString(file, "title") ?? "?"} ` +
        `folderId=${String(getNumber(file, "folderId") ?? getString(file, "folderId") ?? "?")}`,
    );
    return id;
  } finally {
    cancel();
  }
}

async function fetchImageAsDataUrl(url: string): Promise<string | null> {
  try {
    // DocSpace pre-signed URLs are usually host-relative (`/storage/...`);
    // resolve against the portal root. Forward auth cookies in case the URL
    // still requires the caller's session (some DocSpace setups gate file
    // streams on the user, not the signature).
    const absolute = resolveAbsoluteUrl(url);
    const res = await fetch(absolute, { headers: getForwardedHeaders() });
    if (!res.ok) {
      logger.warn(`fetchImageAsDataUrl: ${absolute} → ${res.status} ${res.statusText}`);
      return null;
    }
    const buf = new Uint8Array(await res.arrayBuffer());
    const mime = detectImageMime(buf, res.headers.get("content-type"));
    const b64 = Buffer.from(buf).toString("base64");
    return `data:${mime};base64,${b64}`;
  } catch (err) {
    logger.error(
      `fetchImageAsDataUrl: ${url} failed: ${err instanceof Error ? err.message : String(err)}`,
    );
    return null;
  }
}

// Providers (OpenAI / Anthropic / …) require image_url to be either a public
// URL the model can fetch or a `data:image/*;base64,…` payload. C# returns a
// DocSpace pre-signed URL — fine for previews, useless for the LLM. Inline
// the bytes here so `Attachment.base64` is always a real data URL.
async function inlineImagesAsync(attachments: (Attachment | null)[]): Promise<void> {
  const tasks: Promise<void>[] = [];
  for (const a of attachments) {
    if (!a || a.kind !== "image") {
      continue;
    }
    const src = a.base64;
    if (!src || src.startsWith("data:")) {
      continue;
    }
    tasks.push(
      (async () => {
        const dataUrl = await fetchImageAsDataUrl(src);
        if (dataUrl) {
          a.base64 = dataUrl;
        }
      })(),
    );
  }
  if (tasks.length > 0) {
    await Promise.all(tasks);
  }
}

// The C# `AttachmentsStorageController` exposes a DocSpace-specific shape
// (`POST /attachments { entryIds: [...] }`) and does not provide
// `update`, `deleteByMessage`, or `deleteByThread`. The fields `messageId`,
// `threadId`, and `entityId` aren't carried in `AttachmentDto`.
// Cascade-on-message/thread cleanup is expected to happen server-side.
// Methods missing from the backend are no-ops here with a warning log.

function dtoToAttachment(raw: unknown): Attachment | null {
  if (!isObject(raw)) {
    return null;
  }
  const id = getString(raw, "id");
  const title = getString(raw, "title");
  const kindRaw = getString(raw, "kind");
  const createdAt = getNumber(raw, "createdAt");
  if (id === undefined || title === undefined || kindRaw === undefined) {
    return null;
  }
  const kind = kindRaw.toLowerCase() === "image" ? "image" : "file";
  const result: Attachment = {
    id,
    kind,
    title,
    createdAt: createdAt ?? Date.now(),
  };
  const content = getString(raw, "content");
  if (content !== undefined) {
    result.content = content;
  }
  const base64 = getString(raw, "dataUrl") ?? getString(raw, "base64");
  if (base64 !== undefined) {
    result.base64 = base64;
  }
  // C# echoes the DocSpace entry id (internal int or thirdparty string,
  // both serialized as string) in `entryId`. The chat widget's history
  // chip renders the displayed name via `basename(path)`, so compose the
  // path as `${entryId}/${title}` — keeps the entry id available for
  // openFile/cascade lookups (split on "/") while making basename yield
  // the file title.
  const entryId = getString(raw, "entryId");
  if (entryId !== undefined) {
    result.path = title ? `${entryId}/${title}` : entryId;
  }
  // `type` is derived from the title here, NOT taken from the DTO: C#
  // added `AttachmentDto.Type` in Bug 83003, but it carries the DocSpace
  // `FileType` category enum (Document = 7, Spreadsheet = 5, ...), while
  // `Attachment.type` is an ONLYOFFICE `c_oAscFileType` code (docx = 65)
  // — the scale the widget's chip resolves its icon from. Trusting the DTO
  // value left every chip on the "unknown format" icon, and the category
  // cannot be widened back into a code anyway, so recompute it from the
  // file name (which is exactly what C# maps its own value from). Fresh
  // attaches still prefer the caller's own code, see `createMany`.
  const type = getOnlyofficeFileType(title);
  if (type !== 0) {
    result.type = type;
  }
  // Origin marker (0.4.132+). C# doesn't echo it today, so it's normally
  // back-filled by the caller (tool uploads); read it forward-compat.
  const source = getString(raw, "source");
  if (source === "user" || source === "tool") {
    result.source = source;
  }
  return result;
}

export class HttpAttachmentsStorage implements AttachmentsStorage {
  async create(input: Omit<Attachment, "id" | "createdAt">): Promise<Attachment> {
    const [result] = await this.createMany([input]);
    if (!result) {
      throw new Error("ai service returned no attachment");
    }
    return result;
  }

  async createMany(inputs: Omit<Attachment, "id" | "createdAt">[]): Promise<Attachment[]> {
    if (inputs.length === 0) {
      return [];
    }

    const result: (Attachment | null)[] = new Array(inputs.length).fill(null);

    // One line per create batch: the shape the engine handed us. A
    // `source=tool` row with `entityId=-` is the silent no-scope case (the
    // upload cannot run at all); `source=user` rows carry a DocSpace `path`.
    logger.info(
      `HttpAttachmentsStorage.createMany: ${inputs.length} input(s) ` +
        inputs
          .map(
            (input, i) =>
              `#${i}{kind=${input.kind} source=${input.source ?? "user"} ` +
              `entityId=${input.entityId ?? "-"} path=${input.path ?? "-"} ` +
              `title=${input.title ?? "-"} base64=${input.base64?.length ?? 0}B ` +
              `messageId=${input.messageId ?? "-"} threadId=${input.threadId ?? "-"}}`,
          )
          .join(" "),
    );

    // 0.4.132+: tool-generated images (`generate_image`) arrive as raw base64
    // with `source === "tool"` and no DocSpace entry. Upload the bytes as a
    // real file into the chat's `entityId` folder so they're persisted (and
    // reclaimed by the message/thread cascade), rather than cached in-memory.
    const toolTasks: Promise<void>[] = [];
    inputs.forEach((input, i) => {
      if (input.source !== "tool") return;
      toolTasks.push(
        (async () => {
          result[i] = await this.uploadToolImage(input);
        })(),
      );
    });
    if (toolTasks.length > 0) {
      await Promise.all(toolTasks);
    }

    // Raw-payload drafts (device upload, dnd — no `input.path`, not a tool
    // upload) are rejected: the C# backend has no endpoint for raw content,
    // and the former in-process cache that held them served one user's
    // payload to any other caller (`readById` hit the shared map before any
    // tenant/user check on the backend). Fail loudly instead of leaking.
    inputs.forEach((input, i) => {
      if (result[i] || input.path || input.source === "tool") return;
      throw new Error(
        "raw-payload attachments (no DocSpace entry id) are not supported by the backend",
      );
    });

    const docspaceIndices: number[] = [];
    const entryIds: string[] = [];
    inputs.forEach((input, i) => {
      if (result[i]) return;
      docspaceIndices.push(i);
      entryIds.push(input.path ?? "");
    });

    if (entryIds.length === 0) {
      // No DocSpace-entry inputs — everything was handled above.
      return result.map((rec, i) => {
        if (!rec) {
          throw new Error(`createMany: missing record at index ${i}`);
        }
        return rec;
      });
    }

    const raw = await aiService.post(PATH, { entryIds });
    logger.debug(
      `HttpAttachmentsStorage.createMany: POST ${PATH} entryIds=${JSON.stringify(entryIds)} ` +
        `raw response=${JSON.stringify(raw)}`,
    );
    if (!Array.isArray(raw)) {
      logger.error(
        `HttpAttachmentsStorage.createMany: backend returned non-array payload ` +
          `(type=${raw === null ? "null" : typeof raw}); raw=${JSON.stringify(raw)}`,
      );
      throw new Error("ai service returned a non-array response for attachments createMany");
    }
    // C# `AttachmentsStorageService` groups output by file kind (internal then
    // thirdparty) and so doesn't preserve input order in mixed-kind batches.
    // `dtoToAttachment` composes `path = "${entryId}/${title}"`; split on the
    // first slash to get the original entry id for the order-aware match.
    const byEntryId = new Map<string, Attachment>();
    const skipped: unknown[] = [];
    for (const item of raw) {
      const a = dtoToAttachment(item);
      if (!a || !a.path) {
        skipped.push(item);
        continue;
      }
      const entryIdKey = a.path.split("/", 1)[0] ?? "";
      byEntryId.set(entryIdKey, a);
    }
    if (skipped.length > 0) {
      logger.warn(
        `HttpAttachmentsStorage.createMany: ${skipped.length} item(s) skipped ` +
          `(missing id/title/kind/entryId); skipped=${JSON.stringify(skipped)}`,
      );
    }
    docspaceIndices.forEach((i, docspaceIdx) => {
      const input = inputs[i]!;
      const entryId = entryIds[docspaceIdx] ?? "";
      const matched = byEntryId.get(entryId);
      if (!matched) {
        logger.error(
          `HttpAttachmentsStorage.createMany: no match for entryId=${entryId}. ` +
            `requested=${JSON.stringify(entryIds)} ` +
            `backend entryIds=${JSON.stringify([...byEntryId.keys()])} ` +
            `raw=${JSON.stringify(raw)}`,
        );
        throw new Error(`ai service did not return attachment for entryId=${entryId}`);
      }
      // The caller's own `type` wins over the title-derived one from
      // `dtoToAttachment`: it comes from the same `c_oAscFileType` table but
      // is authoritative for the entry being attached (the host knows the
      // extension it resolved), so an unmapped title can still carry a code.
      if (input.type !== undefined) {
        matched.type = input.type;
      }
      result[i] = matched;
    });

    const finalResult = result.map((rec, i) => {
      if (!rec) {
        throw new Error(`createMany: missing record at index ${i}`);
      }
      return rec;
    });
    await inlineImagesAsync(finalResult);
    return finalResult;
  }

  // Persist a tool-generated image (raw base64, `source === "tool"`). The bytes
  // are uploaded as a real DocSpace file into the chat's `entityId` folder via
  // the public Files API; the resulting entry is then recorded as a normal
  // image attachment through the existing entry-based flow, so reads serve a
  // pre-signed URL exactly like a user upload. The lib keeps only the returned
  // attachment `id` as a lightweight ref.
  private async uploadToolImage(input: Omit<Attachment, "id" | "createdAt">): Promise<Attachment> {
    if (!input.base64) {
      // Logged here, not only thrown: both of these throws are OUTSIDE the
      // try below, so the engine catches them into its own console-only
      // logger and the file log would show nothing at all.
      logger.error(
        `uploadToolImage: no base64 payload (entityId=${input.entityId ?? "-"} title=${input.title ?? "-"}) ` +
          "— the generated image cannot be persisted",
      );
      throw new Error("tool image attachment is missing its base64 payload");
    }
    if (!input.entityId) {
      logger.error(
        "uploadToolImage: no entityId (target folder) — the chat round was sent without an entity scope, " +
          "so there is nowhere to upload the generated image; the engine keeps the raw base64 in the tool result " +
          `(title=${input.title ?? "-"} base64=${input.base64.length}B)`,
      );
      throw new Error("tool image attachment is missing entityId (target folder)");
    }
    logger.info(
      `uploadToolImage: begin entityId=${input.entityId} title=${input.title ?? "-"} ` +
        `base64=${input.base64.length}B payload=${input.base64.startsWith("data:") ? "data-url" : "bare-base64"}`,
    );

    // Agent rooms must not hold generated files directly — artifacts belong
    // in the agent's Result Storage system subfolder. A plain folder takes the
    // upload as-is when it can hold one at all: the chat's scope follows the
    // user's location, so it is regularly somewhere no file may be created —
    // the "Rooms" root, an archived room, a room the user only reads. The
    // Files API answers those with `Access denied`
    // (`FileUploader.GetFolderIdAsync`), so such a scope falls back to the
    // user's own "My documents" instead of losing the image.
    //
    // Every step is logged and `stage` names the one that failed for the catch
    // below: the engine swallows persist failures into its console-only
    // logger, so without this the file log shows nothing when an upload dies,
    // and a 404 from the folder read reads exactly like a 404 from the insert.
    let stage = "folder-info";
    try {
      let targetFolderId = input.entityId;
      const folderInfo = await getFolderInfo(input.entityId);
      logger.info(
        `uploadToolImage: folder-info entityId=${input.entityId} -> ` +
          (folderInfo
            ? `isAgent=${folderInfo.isAgent} title=${folderInfo.title ?? "-"} ` +
                `folderType=${folderInfo.folderType ?? "?"} canCreate=${folderInfo.canCreate ?? "?"}`
            : "NOT FOUND (404 or unparseable) — the scope is not a folder this user can read"),
      );
      if (folderInfo?.isAgent) {
        stage = "result-storage";
        const resultStorageId = await getAgentResultStorageId(input.entityId);
        if (!resultStorageId) {
          throw new Error(
            `agent ${input.entityId} has no accessible Result Storage folder for the generated image`,
          );
        }
        targetFolderId = resultStorageId;
      } else if (!canTakeUpload(folderInfo)) {
        stage = "my-documents";
        const myDocumentsId = await getMyDocumentsFolderId();
        if (!myDocumentsId) {
          throw new Error(
            `the chat scope ${input.entityId} cannot take an upload and "My documents" could not be resolved`,
          );
        }
        logger.warn(
          `uploadToolImage: scope ${input.entityId} cannot take an upload ` +
            `(folderType=${folderInfo?.folderType ?? "?"} canCreate=${folderInfo?.canCreate ?? "?"}) ` +
            `-> falling back to My documents ${myDocumentsId}`,
        );
        targetFolderId = myDocumentsId;
      }
      logger.info(
        `uploadToolImage: entityId=${input.entityId} isAgent=${folderInfo?.isAgent ?? "unknown"} -> target folder ${targetFolderId}`,
      );

      stage = "insert";
      let entryId: string;
      try {
        entryId = await insertGeneratedImage(targetFolderId, input.base64);
      } catch (err) {
        // `FolderDto.Security` is computed for the folder view-model while
        // `FileUploader` re-checks the right server-side, so the preflight
        // above can pass on a target the upload still refuses. Give the image
        // the same second chance a refused scope gets.
        if (!(err instanceof AiServiceHttpError) || err.status !== 403) {
          throw err;
        }
        stage = "my-documents";
        const myDocumentsId = await getMyDocumentsFolderId();
        if (!myDocumentsId || myDocumentsId === targetFolderId) {
          throw err;
        }
        logger.warn(
          `uploadToolImage: insert into folder ${targetFolderId} was refused (403) ` +
            `-> retrying into My documents ${myDocumentsId}`,
        );
        stage = "insert-retry";
        entryId = await insertGeneratedImage(myDocumentsId, input.base64);
        targetFolderId = myDocumentsId;
      }
      stage = "attachment-record";

      // Reuse the entry-based path: `path` routes this back through the
      // DocSpace branch of `createMany` (no `source`, so it won't recurse
      // here).
      const [attachment] = await this.createMany([
        { kind: "image", title: input.title, path: entryId },
      ]);
      if (!attachment) {
        throw new Error("failed to create attachment for uploaded tool image");
      }
      attachment.source = "tool";
      // The id logged here is the `ref` the engine splices into the tool
      // result and the widget then resolves through `GET /attachments/{id}`
      // — grep the same id in `readById` below to see whether the ref that
      // reached the browser resolved or 404'd.
      logger.info(
        `uploadToolImage: ok entityId=${input.entityId} folder=${targetFolderId} ` +
          `entryId=${entryId} ref=${attachment.id} ` +
          `title=${attachment.title} hasDataUrl=${attachment.base64 !== undefined}`,
      );
      return attachment;
    } catch (err) {
      logger.error(
        `uploadToolImage: entityId=${input.entityId} FAILED at stage=${stage}: ${
          err instanceof Error ? `${err.message}${err.stack ? `\n${err.stack}` : ""}` : String(err)
        }`,
      );
      // Don't rethrow: the engine's fallback would splice the raw base64
      // into the tool result, ballooning the persisted message (and the
      // model context) by megabytes. Return a synthetic record instead —
      // without the payload and without caching it, so nothing leaks: the
      // chat stores a lightweight dangling ref (`readById` returns null,
      // the preview shows as unavailable) and the failure stays visible
      // only in the log above.
      const placeholder: Attachment = {
        id: randomUUID(),
        kind: "image",
        title: input.title,
        createdAt: Date.now(),
        source: "tool",
      };
      logger.error(
        `uploadToolImage: returning an UNRESOLVABLE placeholder ref=${placeholder.id} — ` +
          "nothing was persisted, so the stream carries a ref the chat can never hydrate " +
          "(broken image in the message, no file in DocSpace)",
      );
      return placeholder;
    }
  }

  async readById(id: string): Promise<Attachment | null> {
    try {
      const raw = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      const a = dtoToAttachment(raw);
      if (!a) {
        logger.warn(
          `HttpAttachmentsStorage.readById(${id}): unusable payload (missing id/title/kind) ` +
            `raw=${JSON.stringify(raw).slice(0, 500)}`,
        );
        return a;
      }
      await inlineImagesAsync([a]);
      logger.info(
        `HttpAttachmentsStorage.readById(${id}) -> kind=${a.kind} title=${a.title} ` +
          `path=${a.path ?? "-"} hasDataUrl=${a.base64 !== undefined}`,
      );
      return a;
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        // The dangling-ref tell: the chat asked for an attachment that was
        // never persisted (see the placeholder log in `uploadToolImage`) or
        // has since been deleted — e.g. reaped by the AI worker's
        // orphan-attachment cleaner, which drops rows with no message id.
        logger.warn(
          `HttpAttachmentsStorage.readById(${id}): 404 — DANGLING REF, the chat cannot hydrate this attachment`,
        );
        return null;
      }
      throw err;
    }
  }

  async readManyByIds(ids: string[]): Promise<(Attachment | null)[]> {
    if (ids.length === 0) {
      return [];
    }

    const byId = new Map<string, Attachment>();
    const raw = await aiService.post(`${PATH}/read`, { ids });
    const list = Array.isArray(raw) ? raw : [];
    for (const item of list) {
      const a = dtoToAttachment(item);
      if (a) {
        byId.set(a.id, a);
      }
    }

    const result = ids.map((id) => byId.get(id) ?? null);
    const missing = ids.filter((id) => !byId.has(id));
    if (missing.length > 0) {
      logger.warn(
        `HttpAttachmentsStorage.readManyByIds: ${missing.length}/${ids.length} DANGLING REF(s) ` +
          `not returned by the backend: ${missing.join(", ")}`,
      );
    }
    await inlineImagesAsync(result);
    return result;
  }

  async update(id: string, patch: Partial<Attachment>): Promise<void> {
    await this.updateManyByIds([id], patch);
  }

  async updateManyByIds(ids: string[], patch: Partial<Attachment>): Promise<void> {
    if (ids.length === 0) {
      return;
    }
    // The C# side only supports message-binding via `PUT /attachments`
    // — `{ids, messageId}`. Other patches (threadId, entityId, content, etc.)
    // are not actionable on the backend and are silently skipped.
    if (patch.messageId === undefined) {
      logger.debug(
        `HttpAttachmentsStorage.updateManyByIds skipped: no messageId in patch; count=${ids.length}`,
      );
      return;
    }
    await aiService.put(PATH, { ids, messageId: patch.messageId });
  }

  async delete(id: string): Promise<void> {
    try {
      await aiService.delete(`${PATH}/${encodeURIComponent(id)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }

  async deleteMany(ids: string[]): Promise<void> {
    if (ids.length === 0) {
      return;
    }
    await aiService.delete(PATH, { body: { ids } });
  }

  async deleteByMessage(messageId: string): Promise<void> {
    // Cascade on message delete is handled server-side; no client-side action.
    logger.debug(
      `HttpAttachmentsStorage.deleteByMessage is a no-op (cascade is server-side); messageId=${messageId}`,
    );
  }

  async deleteByThread(threadId: string): Promise<void> {
    // Cascade on thread delete is handled server-side; no client-side action.
    logger.debug(
      `HttpAttachmentsStorage.deleteByThread is a no-op (cascade is server-side); threadId=${threadId}`,
    );
  }
}
