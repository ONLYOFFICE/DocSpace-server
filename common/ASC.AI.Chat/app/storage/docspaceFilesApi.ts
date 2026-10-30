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

import { proxyBaseUrl, withTimeout } from "./httpClient.js";
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

// Derived from the DocSpace `FolderDto<int>` (see
// products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs) returned by
// `GET /api/2.0/files/folder/{folderId}` (FoldersController.GetFolderInfo).
/** `FolderType.AiProjectRoom` on the C# side — an AI project. */
const AI_PROJECT_FOLDER_TYPE = 40;

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

// Folder types the Files API refuses an upload into outright, whatever the
// caller's rights — see `FileUploader.GetFolderIdAsync` in
// products/ASC.Files/Core/Utils/FileUploader.cs, which throws
// `SecurityException` for these before it ever looks at `CanCreate`.
const UPLOAD_REFUSING_FOLDER_TYPES = new Set<number>([
  14, // FolderType.VirtualRooms — the "Rooms" root
  20, // FolderType.Archive
  30, // FolderType.RoomTemplates
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
  if (info.folderType !== undefined && UPLOAD_REFUSING_FOLDER_TYPES.has(info.folderType)) {
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
 * Resolves to `undefined` when the folder does not exist (404) or the
 * response cannot be parsed; propagates other HTTP failures as
 * {@link DocspaceApiHttpError}.
 */
export async function getFolderInfo(
  folderId: string,
): Promise<DocspaceFolderInfo | undefined> {
  const url = `${proxyBaseUrl}/api/2.0/files/folder/${encodeURIComponent(folderId)}`;
  const { signal, cancel } = withTimeout(undefined);
  countFilesApiRead();
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (res.status === 404) {
      return undefined;
    }
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const parsed = parseFolderInfo(await res.json());
    if (!parsed) {
      logger.warn(`getFolderInfo(${folderId}) -> unparseable response from ${url}`);
    }
    return parsed;
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

/** What kind of DocSpace entry a client names as the round's source. */
export type SourceKind = "folder" | "file";

/**
 * The DocSpace entry a model request is attributed to, as the billing
 * backend classifies it. Rooms split by the section they belong to: a
 * `Form` is a room of the "Forms" section, a `Room` one of the "Rooms"
 * section. Every other folder — a plain folder in any room or in "My
 * documents", the system subfolders of a form room, the section roots, an
 * AI project — is a `Folder`; every file is a `File`.
 */
export type SourceType = "File" | "Folder" | "Room" | "Form";

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
 * on the C# side, split by section.
 */
export function folderSourceType(info: DocspaceFolderInfo): SourceType {
  const { folderType, roomType } = info;
  if (folderType === FOLDER_TYPE_FORM_ROOM || roomType === ROOM_TYPE_FORM_ROOM) {
    return "Form";
  }
  if (
    (folderType !== undefined && FOLDER_TYPES_ROOM.has(folderType))
    || (roomType !== undefined && ROOM_TYPES_ROOM.has(roomType))
  ) {
    return "Room";
  }
  return "Folder";
}

// Derived from the DocSpace `FileDto<int>` (see
// products/ASC.Files/Core/ApiModels/ResponseDto/FileDto.cs) returned by
// `GET /api/2.0/files/file/{fileId}` (FilesController.GetFileInfo). Only the
// title is read: a file is always a `File` source, whatever its format.
export interface DocspaceFileInfo {
  title?: string;
}

function parseFileInfo(raw: unknown): DocspaceFileInfo | undefined {
  const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
  if (!envelope || getEntityId(envelope, "id") === undefined) {
    return undefined;
  }
  return { title: getString(envelope, "title") };
}

/**
 * Fetch file details from the DocSpace Files API
 * (`GET /api/2.0/files/file/{fileId}`) on behalf of the current user. Same
 * contract as {@link getFolderInfo}: `undefined` on 404 or an unparseable
 * response, {@link DocspaceApiHttpError} for other HTTP failures.
 */
export async function getFileInfo(fileId: string): Promise<DocspaceFileInfo | undefined> {
  const url = `${proxyBaseUrl}/api/2.0/files/file/${encodeURIComponent(fileId)}`;
  const { signal, cancel } = withTimeout(undefined);
  countFilesApiRead();
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (res.status === 404) {
      return undefined;
    }
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const parsed = parseFileInfo(await res.json());
    if (!parsed) {
      logger.warn(`getFileInfo(${fileId}) -> unparseable response from ${url}`);
    }
    return parsed;
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
 * other target. Resolves to `undefined` when the section is unavailable.
 */
export async function getMyDocumentsFolderId(): Promise<string | undefined> {
  // `count` is [Range(1, …)]-validated on the C# side — ask for the smallest
  // allowed page, only `current.id` is read.
  const url = `${proxyBaseUrl}/api/2.0/files/@my?count=1`;
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal });
    if (!res.ok) {
      logger.warn(`getMyDocumentsFolderId: ${url} -> ${res.status} ${res.statusText}`);
      return undefined;
    }
    const raw: unknown = await res.json();
    const envelope = isObject(raw) ? getObject(raw, "response") : undefined;
    const current = envelope ? getObject(envelope, "current") : undefined;
    const id = current ? getEntityId(current, "id") : undefined;
    if (id === undefined) {
      logger.warn(`getMyDocumentsFolderId: no current.id in the response from ${url}`);
      return undefined;
    }
    return id;
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
  return folderInfo?.folderType === AI_PROJECT_FOLDER_TYPE ? entityId : undefined;
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
export async function assertEntityAccessible(
  entityId: string | undefined,
): Promise<void> {
  if (typeof entityId !== "string" || entityId.length === 0) {
    return;
  }
  let accessible: boolean;
  try {
    accessible = (await getFolderInfoOnce(entityId)) !== undefined;
  } catch (err) {
    if (err instanceof DocspaceApiHttpError && err.status === 403) {
      throw Object.assign(new Error(`Entity "${entityId}" is not accessible`), {
        status: 403,
        expose: true,
      });
    }
    throw err; // genuine upstream failure (5xx) → propagate as-is
  }
  if (!accessible) {
    throw Object.assign(new Error(`Entity "${entityId}" not found`), {
      status: 404,
      expose: true,
    });
  }
}
