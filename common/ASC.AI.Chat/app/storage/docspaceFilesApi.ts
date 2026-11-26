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

import { proxyBaseUrl, readBodyCapped, readTextCapped, withTimeout } from "./httpClient.js";
import {
  countFilesApiRead,
  getForwardedHeaders,
  getFolderInfoCache,
  getFileInfoCache,
  setSourceMeta,
} from "../requestContext.js";
import { isObject, getNumber, getObject, getString, getEntityId } from "../narrow.js";
import logger from "../log.js";
import { lookupChatContextFolder } from "./chatContextSnapshot.js";
import { clientFault } from "../httpErrors.js";
import { errorBodyText } from "./errorBody.js";
import {
  assertEntityPath,
  jsonAnswer,
  mapped,
  namesNoEntity,
  UpstreamShapeError,
} from "./upstreamShape.js";

// Derived from the DocSpace `FolderDto<int>` (see
// products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs) returned by
// `GET /api/2.0/files/folder/{folderId}` (FoldersController.GetFolderInfo).
/** `FolderType.AiProjectRoom` on the C# side — an AI project. */
const AI_PROJECT_FOLDER_TYPE = 40;
/**
 * `RoomType.AiProjectRoom` on the C# side. `FolderDto.Type` is filled for a
 * few system folders only and not for an AI project, so the DTO of
 * `GET files/folder/{id}` names a project by its `roomType` alone.
 */
const AI_PROJECT_ROOM_TYPE = 12;

export interface DocspaceFolderInfo {
  /** The folder's display name (`FolderDto.Title`). */
  title?: string;
  /** `FolderDto.Type` — the raw `FolderType` value. */
  folderType?: number;
  /** `FolderDto.RoomType` — the raw `RoomType` value; only rooms carry one. */
  roomType?: number;
  /**
   * Whether the current user may create entries here (`FolderDto.Security`
   * -> `FilesSecurityActions.Create`). `undefined` when the DTO carries no
   * security block at all, which callers must not read as a denial.
   */
  canCreate?: boolean;
  /**
   * The folder is the root of a section that holds rooms (see
   * `SECTION_ROOT_FOLDER_TYPES`): a file is never placed in it directly.
   * `FolderDto` names no `type` for such a root, so it is told by having no
   * parent and a root type of that kind.
   */
  sectionRoot?: boolean;
}

// The form-filling room — the one room kind the "Forms" section shows. That
// section is virtual (anchored on an empty `FolderType.Forms` folder); its
// rooms physically live in the VirtualRooms tree and differ from the rooms
// of the "Rooms" section only by this type (`FileSecurity.MatchesFormsSplit`).
const FOLDER_TYPE_FORM_ROOM = 15;
const ROOM_TYPE_FORM_ROOM = 1;

// Every other member of `DocSpaceHelper.RoomTypes` — the rooms of the "Rooms"
// section: EditingRoom, CustomRoom, PublicRoom, VirtualDataRoom. Both the
// `FolderType` and the `RoomType` spelling, as the DTO carries both.
const FOLDER_TYPES_ROOM = new Set<number>([16, 19, 22, 29]);
const ROOM_TYPES_ROOM = new Set<number>([2, 5, 6, 8]);

// The roots of the sections that hold rooms (`DocSpaceHelper.IsRoomsSectionRoot`
// on the C# side). The Files API refuses an upload into one outright,
// whatever the caller's rights — see `FileUploader.GetFolderIdAsync` in
// products/ASC.Files/Core/Utils/FileUploader.cs, which throws
// `SecurityException` for these before it ever looks at `CanCreate`, and an
// admin reads `Create: true` on them all the same.
const SECTION_ROOT_FOLDER_TYPES = new Set<number>([
  14, // FolderType.VirtualRooms — the "Rooms" root
  20, // FolderType.Archive
  30, // FolderType.RoomTemplates
  34, // FolderType.AiAgents
  36, // FolderType.Forms
  41, // FolderType.AiHub — the "AI rooms" root
  42, // FolderType.PrivateRooms
]);

/**
 * Whether `POST api/2.0/files/{folderId}/insert` can succeed for this folder.
 * A missing `security` block leaves the decision to the server (treated as
 * allowed) — only an explicit `Create: false` or a refusing folder type is a
 * local "no".
 */
export function canTakeUpload(info: DocspaceFolderInfo | undefined): boolean {
  if (!info) {
    return false;
  }
  if (info.sectionRoot) {
    return false;
  }
  if (info.folderType !== undefined && SECTION_ROOT_FOLDER_TYPES.has(info.folderType)) {
    return false;
  }
  return info.canCreate !== false;
}

export class DocspaceApiHttpError extends Error {
  public readonly status: number;
  public readonly statusText: string;
  public readonly url: string;

  constructor(status: number, statusText: string, url: string) {
    super(`DocSpace API ${status} ${statusText} for ${url}`);
    this.status = status;
    this.statusText = statusText;
    this.url = url;
  }
}

/**
 * Ends an answer whose body nobody is going to use: a refusal, thrown or read as "absent". The
 * body is read to its end, or to the limit of {@link errorBodyText} and then cancelled, because
 * a body left unread keeps its connection out of the pool until the garbage collector finds
 * the response. The text is dropped: the body of a refusal goes into no error and no log line
 * here. Never throws, so a body that fails does not hide the status.
 */
export async function releaseBody(res: Response): Promise<void> {
  await errorBodyText(res);
}

/**
 * The error of a refused answer, made after its body has been released ({@link releaseBody}).
 * Every non-2xx branch of the Files and People clients that throws goes through here.
 */
export async function refusedAnswer(res: Response, url: string): Promise<DocspaceApiHttpError> {
  await releaseBody(res);
  return new DocspaceApiHttpError(res.status, res.statusText, url);
}

/** A query value of {@link filesApiGet}: an array repeats the parameter once per item. */
export type FilesApiQuery = Record<string, string | number | readonly (string | number)[]>;

/**
 * `GET api/2.0/files/{path}` on behalf of the current user (the caller's
 * credentials relayed via {@link getForwardedHeaders}), answering the
 * `response` of the DocSpace envelope. Any non-2xx answer throws
 * {@link DocspaceApiHttpError} with its status; an answer that is not JSON
 * or carries no `response` throws {@link UpstreamShapeError}. `path` is relative to `api/2.0/files/`
 * and must already be encoded. `signal` (the caller's own budget or its
 * client hanging up) ends the call as well as the timeout does.
 */
export async function filesApiGet(
  path: string,
  query?: FilesApiQuery,
  signal?: AbortSignal,
): Promise<unknown> {
  const params = new URLSearchParams();
  for (const [name, value] of Object.entries(query ?? {})) {
    for (const item of Array.isArray(value) ? value : [value]) {
      params.append(name, String(item));
    }
  }
  const qs = params.toString();
  const search = qs ? `?${qs}` : "";
  assertEntityPath(path);
  const url = `${proxyBaseUrl}/api/2.0/files/${path}${search}`;
  const timed = withTimeout(signal);
  countFilesApiRead();
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal: timed.signal });
    if (!res.ok) {
      throw await refusedAnswer(res, url);
    }
    const raw = await jsonAnswer(res, `GET files/${path}`);
    if (!isObject(raw) || !("response" in raw)) {
      throw new UpstreamShapeError(`GET files/${path}`);
    }
    return raw["response"];
  } finally {
    timed.cancel();
  }
}

/**
 * A write to `api/2.0/files/{path}` on behalf of the current user, with a
 * JSON body, answering the `response` of the DocSpace envelope (`undefined`
 * when the answer carries none). Any non-2xx answer throws
 * {@link DocspaceApiHttpError} with its status. `path` is relative to
 * `api/2.0/files/` and must already be encoded. `signal` ends the call as
 * well as the timeout does.
 */
export async function filesApiSend(
  method: "POST" | "PUT",
  path: string,
  body: unknown,
  signal?: AbortSignal,
): Promise<unknown> {
  assertEntityPath(path);
  const url = `${proxyBaseUrl}/api/2.0/files/${path}`;
  const timed = withTimeout(signal);
  try {
    const res = await fetch(url, {
      method,
      headers: { ...getForwardedHeaders(), "Content-Type": "application/json" },
      body: JSON.stringify(body),
      signal: timed.signal,
    });
    if (!res.ok) {
      throw await refusedAnswer(res, url);
    }
    return responseOfWrite(await res.text());
  } finally {
    timed.cancel();
  }
}

/**
 * The `response` of a write's answer. The 2xx has already said the write was
 * done, and the callers read the answer only for details of what was made
 * (they check those themselves), so an empty or unreadable body is "no
 * details", not a failed write.
 */
function responseOfWrite(text: string): unknown {
  let raw: unknown;
  try {
    raw = JSON.parse(text);
  } catch {
    return undefined;
  }
  return isObject(raw) ? raw["response"] : undefined;
}

/** The body of {@link filesApiRequest}: a form (an upload) or a JSON value. */
export type FilesApiBody = { form: FormData } | { json: unknown };

/**
 * A write to `api/2.0/files/{path}` on behalf of the current user, answering
 * the JSON of the answer as it came: the upload routes answer the new file
 * with or without the DocSpace envelope, so the caller unwraps it (see
 * {@link envelopeResponse}). Any non-2xx answer throws
 * {@link DocspaceApiHttpError} with its status, and an answer that is not
 * JSON throws {@link UpstreamShapeError}. `path` is relative to `api/2.0/files/` and
 * must already be encoded.
 */
export async function filesApiRequest(
  method: "POST" | "PUT" | "DELETE",
  path: string,
  body: FilesApiBody,
): Promise<unknown> {
  assertEntityPath(path);
  const url = `${proxyBaseUrl}/api/2.0/files/${path}`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    // A form carries no Content-Type here: `fetch` derives the multipart
    // boundary from the body.
    const res = await fetch(
      url,
      "form" in body
        ? { method, headers: getForwardedHeaders(), body: body.form, signal }
        : {
            method,
            headers: { ...getForwardedHeaders(), "Content-Type": "application/json" },
            body: JSON.stringify(body.json),
            signal,
          },
    );
    if (!res.ok) {
      throw await refusedAnswer(res, url);
    }
    return await jsonAnswer(res, `${method} files/${path}`);
  } finally {
    cancel();
  }
}

/**
 * A `GET` of a file's bytes from the address the Files API handed out for it
 * (a pre-signed link on the portal or on its storage's own host), with the
 * `headers` the caller chose for that host. `read` takes the answer and runs
 * inside the timeout, so a body that stalls is cut off as well. Any non-2xx
 * answer throws {@link DocspaceApiHttpError} with its status before `read`
 * is called; its message names the status and the address, never the body.
 * `signal` ends the call and the read as well as the timeout does.
 */
export async function filesApiDownload<T>(
  href: string,
  headers: Record<string, string>,
  read: (res: Response) => Promise<T>,
  signal?: AbortSignal,
): Promise<T> {
  const timed = withTimeout(signal);
  try {
    const res = await fetch(href, { headers, signal: timed.signal });
    if (!res.ok) {
      throw await refusedAnswer(res, href);
    }
    return await read(res);
  } finally {
    timed.cancel();
  }
}

/**
 * {@link filesApiDownload} of a text: the body decoded as UTF-8, cut at
 * `limit` bytes. The rest of a longer body is not downloaded.
 */
export function filesApiDownloadText(
  href: string,
  headers: Record<string, string>,
  limit: number,
  signal?: AbortSignal,
): Promise<string> {
  return filesApiDownload(href, headers, (res) => readTextCapped(res, limit), signal);
}

/**
 * {@link filesApiDownload} of the bytes as they are, or `null` once the body
 * proves larger than `limit` bytes (by its declared length or by what has
 * arrived); the rest of it is not downloaded. The caller names the file in
 * its own error.
 */
export function filesApiDownloadBytes(
  href: string,
  headers: Record<string, string>,
  limit: number,
  signal?: AbortSignal,
): Promise<Uint8Array | null> {
  return filesApiDownload(href, headers, (res) => readBodyCapped(res, limit), signal);
}

/** The `response` of a DocSpace envelope, or the answer itself when it has none. */
export function envelopeResponse(raw: unknown): unknown {
  return isObject(raw) && "response" in raw ? raw["response"] : raw;
}

function parseFolderInfo(raw: unknown): DocspaceFolderInfo | undefined {
  // DocSpace API responses are wrapped in a `{ response: ... }` envelope.
  const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
  if (!envelope) {
    return undefined;
  }
  if (getEntityId(envelope, "id") === undefined) {
    return undefined;
  }
  const folderType = getNumber(envelope, "type");
  const roomType = getNumber(envelope, "roomType");
  const result: DocspaceFolderInfo = {
    title: getString(envelope, "title"),
  };
  if (folderType !== undefined) {
    result.folderType = folderType;
  }
  if (roomType !== undefined) {
    result.roomType = roomType;
  }
  const rootFolderType = getNumber(envelope, "rootFolderType");
  if (
    getNumber(envelope, "parentId") === 0 &&
    rootFolderType !== undefined &&
    SECTION_ROOT_FOLDER_TYPES.has(rootFolderType)
  ) {
    result.sectionRoot = true;
  }
  // `FolderDto.Security` is a dictionary keyed by the `FilesSecurityActions`
  // enum name; the JSON casing has changed before, so match case-insensitively
  // rather than pinning "Create".
  const security = getObject(envelope, "security");
  if (security) {
    for (const [action, allowed] of Object.entries(security)) {
      if (action.toLowerCase() === "create" && typeof allowed === "boolean") {
        result.canCreate = allowed;
        break;
      }
    }
  }
  return result;
}

/**
 * Fetch folder details from the DocSpace Files API
 * (`GET /api/2.0/files/folder/{folderId}`) on behalf of the current user —
 * the caller's credentials are relayed via {@link getForwardedHeaders}.
 * Resolves to `undefined` when the folder does not exist (404); an answer
 * that cannot be parsed throws {@link UpstreamShapeError}, other HTTP
 * failures {@link DocspaceApiHttpError}.
 */
export async function getFolderInfo(folderId: string): Promise<DocspaceFolderInfo | undefined> {
  if (namesNoEntity(folderId)) {
    return undefined;
  }
  const url = `${proxyBaseUrl}/api/2.0/files/folder/${encodeURIComponent(folderId)}`;
  const { signal, cancel } = withTimeout(undefined);
  countFilesApiRead();
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (res.status === 404) {
      await releaseBody(res);
      return undefined;
    }
    if (!res.ok) {
      throw await refusedAnswer(res, url);
    }
    return mapped(
      parseFolderInfo(await jsonAnswer(res, "GET files/folder/{id}")),
      "GET files/folder/{id}",
    );
  } finally {
    cancel();
  }
}

/**
 * Request-scoped memoization of {@link getFolderInfo}. A single chat round
 * reads the same folder more than once — the AI-project detection, the
 * source metadata — and the DTO cannot change mid-request, so the second reader
 * joins the first fetch instead of issuing its own. Falls through to a plain
 * fetch outside a request context. Rejections are not cached: the entry is
 * dropped so a later reader can retry.
 */
function getFolderInfoOnce(folderId: string): Promise<DocspaceFolderInfo | undefined> {
  // A primed chat round already carries the folders it is scoped to (from
  // `GET internal/ai/chat-context`) — no Files API round-trip for those.
  const fromSnapshot = lookupChatContextFolder(folderId);
  if (fromSnapshot.hit) {
    return Promise.resolve(fromSnapshot.info);
  }
  const cache = getFolderInfoCache();
  if (!cache) {
    return getFolderInfo(folderId);
  }
  const cached = cache.get(folderId);
  if (cached) {
    return cached;
  }
  const pending = getFolderInfo(folderId);
  cache.set(folderId, pending);
  pending.catch(() => cache.delete(folderId));
  return pending;
}

/**
 * A folder's info for a reader that asks about several folders of one
 * round (the context folders): memoized per request, served from the
 * primed chat context when it carries the folder.
 */
export function getFolderInfoCached(folderId: string): Promise<DocspaceFolderInfo | undefined> {
  return getFolderInfoOnce(folderId);
}

/** What kind of DocSpace entry a client names as the round's source. */
export type SourceKind = "folder" | "file";

/**
 * The DocSpace entry a model request is attributed to, as the billing
 * backend classifies it. An AI project is an `Agent` (the backend's name for
 * it, shown as "AI agent"). Rooms split by the section they belong to: a
 * `Form` is a room of the "Forms" section, a `Room` one of the "Rooms"
 * section. Every other folder — a plain folder in any room or in "My
 * documents", the system subfolders of a form room, the section roots — is a
 * `Folder`; every file is a `File`.
 */
export type SourceType = "File" | "Folder" | "Room" | "Form" | "Agent";

/**
 * The resolved source of a round, sent by the ONLYOFFICE provider (and the
 * passthroughs) as the request's `metadata` object — structurally the
 * library's `SourceMeta`; `sourceMetadataFields` from `@onlyoffice/ai-chat`
 * spells the wire keys (`source_id` / `source_type` / `source_title`).
 */
export interface SourceMeta {
  id: string;
  type: SourceType;
  title?: string;
}

/**
 * Classify a folder by the DTO's `type` (`FolderType`) and `roomType`
 * (`RoomType`); either spelling suffices. Mirrors `DocSpaceHelper.RoomTypes`
 * on the C# side, split by section, with an AI project set apart.
 */
export function folderSourceType(info: DocspaceFolderInfo): SourceType {
  const { folderType, roomType } = info;
  if (folderType === AI_PROJECT_FOLDER_TYPE || roomType === AI_PROJECT_ROOM_TYPE) {
    return "Agent";
  }
  if (folderType === FOLDER_TYPE_FORM_ROOM || roomType === ROOM_TYPE_FORM_ROOM) {
    return "Form";
  }
  if (
    (folderType !== undefined && FOLDER_TYPES_ROOM.has(folderType)) ||
    (roomType !== undefined && ROOM_TYPES_ROOM.has(roomType))
  ) {
    return "Room";
  }
  return "Folder";
}

// Derived from the DocSpace `FileDto<int>` (see
// products/ASC.Files/Core/ApiModels/ResponseDto/FileDto.cs) returned by
// `GET /api/2.0/files/file/{fileId}` (FilesController.GetFileInfo). A file is
// always a `File` source, whatever its format, so the source needs only the
// title; the rest describes a file a tool created (see documentAgent.ts).
export interface DocspaceFileInfo {
  title?: string;
  /** `FileDto.FolderId`: the folder the file lies in. */
  folderId?: string;
  /** `FileDto.FileExst`: the extension, with its dot. */
  fileExst?: string;
  /** `FileDto.WebUrl`: the absolute link the portal opens the file by. */
  webUrl?: string;
}

function parseFileInfo(raw: unknown): DocspaceFileInfo | undefined {
  const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
  if (!envelope || getEntityId(envelope, "id") === undefined) {
    return undefined;
  }
  const info: DocspaceFileInfo = { title: getString(envelope, "title") };
  const folderId = getEntityId(envelope, "folderId");
  const fileExst = getString(envelope, "fileExst");
  const webUrl = getString(envelope, "webUrl");
  if (folderId !== undefined) {
    info.folderId = folderId;
  }
  if (fileExst) {
    info.fileExst = fileExst;
  }
  if (webUrl) {
    info.webUrl = webUrl;
  }
  return info;
}

/**
 * Fetch file details from the DocSpace Files API
 * (`GET /api/2.0/files/file/{fileId}`) on behalf of the current user. Same
 * contract as {@link getFolderInfo}: `undefined` on 404,
 * {@link UpstreamShapeError} for an answer that cannot be parsed,
 * {@link DocspaceApiHttpError} for other HTTP failures.
 */
export async function getFileInfo(fileId: string): Promise<DocspaceFileInfo | undefined> {
  if (namesNoEntity(fileId)) {
    return undefined;
  }
  const url = `${proxyBaseUrl}/api/2.0/files/file/${encodeURIComponent(fileId)}`;
  const { signal, cancel } = withTimeout(undefined);
  countFilesApiRead();
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (res.status === 404) {
      await releaseBody(res);
      return undefined;
    }
    if (!res.ok) {
      throw await refusedAnswer(res, url);
    }
    return mapped(
      parseFileInfo(await jsonAnswer(res, "GET files/file/{id}")),
      "GET files/file/{id}",
    );
  } finally {
    cancel();
  }
}

// Request-scoped memoization of {@link getFileInfo}, the file-side twin of
// `getFolderInfoOnce`. Files and folders have independent id spaces, so the
// two caches are kept apart.
function getFileInfoOnce(fileId: string): Promise<DocspaceFileInfo | undefined> {
  const cache = getFileInfoCache();
  if (!cache) {
    return getFileInfo(fileId);
  }
  const cached = cache.get(fileId);
  if (cached) {
    return cached;
  }
  const pending = getFileInfo(fileId);
  cache.set(fileId, pending);
  pending.catch(() => cache.delete(fileId));
  return pending;
}

/**
 * Best-effort resolution of the entry a round runs for. The client only ever
 * names the entry (`entityId` + which table it lives in, `kind`); the type
 * and the title come from the Files API under the caller's credentials, so
 * a client cannot describe an entry it cannot see. An absent id, a 404/403,
 * or an unparseable response all yield `undefined`, which the provider
 * renders as no `metadata` at all. Never throws (mirrors
 * {@link safeGetToolsPrompt}): metadata is telemetry for the backend,
 * never a reason to fail a chat.
 */
export async function safeResolveSource(
  entityId: string | undefined,
  kind: SourceKind = "folder",
): Promise<SourceMeta | undefined> {
  if (!entityId) {
    return undefined;
  }
  try {
    if (kind === "file") {
      const file = await getFileInfoOnce(entityId);
      return file ? { id: entityId, type: "File", title: file.title } : undefined;
    }
    const folder = await getFolderInfoOnce(entityId);
    return folder
      ? { id: entityId, type: folderSourceType(folder), title: folder.title }
      : undefined;
  } catch (err) {
    logger.warn(
      `source entity fetch failed (${kind} ${entityId}): ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
    return undefined;
  }
}

/**
 * Resolve the round's source and remember it on the request context, where
 * the engines' `resolveSource` dep reads it before the library builds the
 * request body (see aiController). Every model-bound handler
 * calls this before invoking the engine; a round with no resolvable source
 * leaves the context empty and the request carries no `metadata`.
 */
export async function primeSourceMeta(
  entityId: string | undefined,
  kind: SourceKind = "folder",
): Promise<SourceMeta | undefined> {
  const source = await safeResolveSource(entityId, kind);
  setSourceMeta(source);
  return source;
}

/**
 * Resolve the current user's "My documents" folder id
 * (`GET api/2.0/files/@my` -> `current.id`). The target for a generated
 * file when the round names no folder or the folder it names cannot take an
 * upload, and the folder context of a round inside an AI project. The
 * numeric id is resolved rather than posting to the `@my/insert` alias so the
 * upload goes through the same code path (and logs the real folder) as any
 * other target. Resolves to `undefined` when the Files API refuses the
 * section (403, 404); an answer that names no folder throws
 * {@link UpstreamShapeError}, any other refusal {@link DocspaceApiHttpError},
 * and a timeout or a dropped connection throws as well.
 */
export async function getMyDocumentsFolderId(): Promise<string | undefined> {
  // `count` is [Range(1, …)]-validated on the C# side — ask for the smallest
  // allowed page, only `current.id` is read.
  const url = `${proxyBaseUrl}/api/2.0/files/@my?count=1`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    // Only a refusal says the caller has no such section; any other failure
    // is unknown and is rethrown, so it is not read as "no My documents".
    if (res.status === 403 || res.status === 404) {
      logger.warn(`getMyDocumentsFolderId: ${url} -> ${res.status} ${res.statusText}`);
      await releaseBody(res);
      return undefined;
    }
    if (!res.ok) {
      throw await refusedAnswer(res, url);
    }
    const raw = await jsonAnswer(res, "GET files/@my");
    const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
    const current = envelope ? getObject(envelope, "current") : undefined;
    return mapped(current ? getEntityId(current, "id") : undefined, "GET files/@my");
  } finally {
    cancel();
  }
}

/**
 * The project id when `entityId` is an AI project (`FolderType.AiProjectRoom`),
 * `undefined` for any other folder, an unknown one, or no id at all. Served
 * from the primed chat-context snapshot when it describes the folder.
 */
export async function resolveAiProjectEntityId(
  entityId: string | undefined,
): Promise<string | undefined> {
  if (!entityId || !/^\d+$/.test(entityId)) {
    return undefined;
  }
  const folderInfo = await getFolderInfoOnce(entityId);
  // The chat-context snapshot carries the folder type, the Files DTO only
  // the room type (see AI_PROJECT_ROOM_TYPE): either one names a project.
  return folderInfo?.folderType === AI_PROJECT_FOLDER_TYPE ||
    folderInfo?.roomType === AI_PROJECT_ROOM_TYPE
    ? entityId
    : undefined;
}

/**
 * Gate a client-supplied `entityId` on *accessibility*. When an entityId is
 * present it must reference a folder the caller can actually reach: a
 * missing folder surfaces as 404, a folder the caller cannot open as 403 —
 * the status the Files API itself answers. Masking the 403 as 404 here hid
 * nothing and made a write to a room answer differently from a read of it
 * (Bug 83998). An absent entityId is the legitimate global scope and passes.
 * Any accessible folder is a valid id: the per-user AI data is global, so
 * the id only matters where it is read for what it is (an AI project, a
 * location, an upload target). Used to gate thread creation and the
 * settings writes that still take an `entityId`.
 */
export async function assertEntityAccessible(entityId: string | undefined): Promise<void> {
  if (typeof entityId !== "string" || entityId.length === 0) {
    return;
  }
  let accessible: boolean;
  try {
    accessible = (await getFolderInfoOnce(entityId)) !== undefined;
  } catch (err) {
    if (err instanceof DocspaceApiHttpError && err.status === 403) {
      throw clientFault(403, `Entity "${entityId}" is not accessible`);
    }
    throw err; // genuine upstream failure (5xx) → propagate as-is
  }
  if (!accessible) {
    throw clientFault(404, `Entity "${entityId}" not found`);
  }
}
