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

// Upstream DTOs of the Files and People services as the storage modules read them, and a
// primed request for the chat-context snapshot. Shared by the storage suites of
// docspaceFilesApi, attachmentsStorage, projectAttachments and docspacePeopleApi.

import { inRequestContext } from "../helpers/http.js";
import { setChatContextSnapshot } from "../../requestContext.js";
import type { ChatContextSnapshot } from "../../storage/chatContextSnapshot.js";
import type { DocspaceFolderInfo } from "../../storage/docspaceFilesApi.js";

/** A primed snapshot that describes `folders` as the round's entity and context entity. */
export function snapshotWithFolders(
  entity?: { id: string; folder: DocspaceFolderInfo | undefined },
  contextEntity?: { id: string; folder: DocspaceFolderInfo | undefined },
): ChatContextSnapshot {
  const scope = (
    s: { id: string; folder: DocspaceFolderInfo | undefined } | undefined,
  ): ChatContextSnapshot["entity"] =>
    s === undefined
      ? null
      : {
          entityId: s.id,
          folder: s.folder,
          assignments: {},
          reasoningLevel: null,
          toolPrefs: {},
          mcpServers: {},
        };
  return {
    requested: { threadId: undefined, entityId: entity?.id, contextEntityId: contextEntity?.id },
    aiReady: true,
    toolPermissionMode: null,
    profiles: [],
    global: {
      entityId: undefined,
      folder: undefined,
      assignments: {},
      reasoningLevel: null,
      toolPrefs: {},
      mcpServers: {},
    },
    entity: scope(entity),
    contextEntity: scope(contextEntity),
    thread: null,
    messages: null,
    webSearch: null,
    documentAgentEnabled: undefined,
    stale: new Set(),
  };
}

/** Runs `fn` in a real request context, primed with `snapshot` when one is given. */
export function inRequest<T>(
  fn: () => Promise<T>,
  options: { snapshot?: ChatContextSnapshot; headers?: Record<string, string> } = {},
): Promise<T> {
  return inRequestContext(options.headers ?? {}, () => {
    if (options.snapshot) {
      setChatContextSnapshot(options.snapshot);
    }
    return fn();
  });
}

const employee = {
  id: "66faa6e4-f133-11ea-b126-00ffeec8b4ef",
  displayName: "Administrator",
  avatarSmall: "/static/images/default_user_photo_size_32-32.png",
  profileUrl: "http://portal.example/accounts/view/administrator",
  hasAvatar: false,
  isAnonim: false,
};

/**
 * `FolderDto<int>` of a room as `GET api/2.0/files/folder/{folderId}` answers it
 * (products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs and FileEntryDto.cs,
 * FoldersController.GetFolderInfo), inside the DocSpace envelope.
 */
export function folderDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    response: {
      parentId: 6,
      filesCount: 3,
      foldersCount: 1,
      isShareable: true,
      new: 0,
      mute: false,
      tags: [],
      logo: { original: "", large: "", medium: "", small: "", color: "8BA4CA", cover: null },
      pinned: false,
      roomType: 5,
      private: false,
      indexing: false,
      denyDownload: false,
      inRoom: true,
      quotaLimit: -1,
      isCustomQuota: false,
      usedSpace: 1024,
      id: 30,
      rootFolderId: 6,
      canShare: true,
      security: { Read: true, Create: true, Delete: false, EditRoom: true, Rename: true },
      title: "Marketing",
      access: 0,
      shared: false,
      sharedForUser: false,
      sharedExternal: false,
      parentShared: false,
      created: "2026-11-01T10:00:00.0000000+00:00",
      createdBy: employee,
      updated: "2026-11-20T10:00:00.0000000+00:00",
      rootFolderType: 14,
      updatedBy: employee,
      fileEntryType: 1,
      ...overrides,
    },
    count: 1,
    links: [{ href: "http://portal.example/api/2.0/files/folder/30", action: "GET" }],
    status: 0,
    statusCode: 200,
  };
}

/**
 * `FileDto<int>` as the Files API answers it (products/ASC.Files/Core/ApiModels/ResponseDto/FileDto.cs
 * and FileEntryDto.cs: `GET api/2.0/files/file/{fileId}`, `POST api/2.0/files/{folderId}/insert`),
 * without the envelope.
 */
export function fileDtoBody(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    folderId: 30,
    version: 1,
    versionGroup: 1,
    contentLength: "6 bytes",
    pureContentLength: 6,
    fileStatus: 0,
    mute: false,
    viewUrl: "http://portal.example/filehandler.ashx?action=download&fileid=40",
    webUrl: "http://portal.example/doceditor?fileid=40&version=1",
    fileType: 7,
    fileExst: ".docx",
    comment: "Uploaded",
    thumbnailStatus: 0,
    isForm: false,
    id: 40,
    rootFolderId: 6,
    canShare: true,
    security: { Read: true, Edit: true },
    title: "Report.docx",
    access: 0,
    shared: false,
    created: "2026-11-01T10:00:00.0000000+00:00",
    createdBy: employee,
    updated: "2026-11-20T10:00:00.0000000+00:00",
    rootFolderType: 14,
    updatedBy: employee,
    fileEntryType: 2,
    ...overrides,
  };
}

/** {@link fileDtoBody} inside the DocSpace envelope. */
export function fileDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return { response: fileDtoBody(overrides), count: 1, status: 0, statusCode: 200 };
}
