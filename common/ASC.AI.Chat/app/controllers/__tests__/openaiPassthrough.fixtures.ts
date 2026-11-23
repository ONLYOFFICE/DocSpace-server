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

import { docspace, http } from "../../__tests__/helpers/upstream.js";
import type { useUpstream } from "../../__tests__/helpers/upstream.js";
import { aiServiceBaseUrl, proxyBaseUrl } from "../../storage/httpClient.js";

// Fixtures of the OpenAI passthrough tests (openaiPassthrough*.test.ts).

type Upstream = ReturnType<typeof useUpstream>;

export const PROFILE_ID = "8d6a4c1e-2f0b-4d8e-9c37-5a1f0e2b7c44";
export const CHAT = `/api/2.0/ai/openai/${PROFILE_ID}/v1/chat/completions`;
export const IMAGES = `/api/2.0/ai/openai/${PROFILE_ID}/v1/images/generations`;
export const PROVIDER = "https://provider.test/v1";
/** Where an `onlyoffice` profile is sent: the gateway of the AI service (profilesStorage.ts). */
export const GATEWAY = `${aiServiceBaseUrl}/api/2.0/ai/gateway`;
export const CALLER = { Authorization: "Bearer caller-token", "Content-Type": "application/json" };
export const MB = 1024 * 1024;

// products/ASC.AI/Server/Models/ResponseDto/ProfileDto.cs (ProfileMapper.MapToDto), as
// `GET internal/ai/profiles/{id}` (ProfilesController) answers it.
export function profileDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    id: PROFILE_ID,
    name: "Provider",
    providerType: "openai",
    baseUrl: PROVIDER,
    key: "sk-profile-secret",
    modelId: "gpt-4o",
    reasoning: null,
    capabilities: null,
    useResponsesApi: null,
    canUseTool: true,
    byokProvider: null,
    createdAt: 1767225600000,
    ...overrides,
  };
}

export function useProfile(upstream: Upstream, dto: unknown = profileDto()): void {
  upstream.use(
    http.get(`${aiServiceBaseUrl}/internal/ai/profiles/:id`, () => docspace(dto as never)),
  );
}

// products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs, as
// `GET api/2.0/files/folder/{id}` (FoldersController.GetFolderInfo) answers it: a room of
// the Rooms section (RoomType.CustomRoom = 5).
export function useFolder(upstream: Upstream, id: string, title: string): void {
  upstream.use(
    http.get(`${proxyBaseUrl}/api/2.0/files/folder/${id}`, () =>
      docspace({ id: Number(id), title, roomType: 5, parentId: 1, rootFolderType: 14 }),
    ),
  );
}

// products/ASC.Files/Core/ApiModels/ResponseDto/FileDto.cs, as `GET api/2.0/files/file/{id}`
// (FilesController.GetFileInfo) answers it.
export function useFile(upstream: Upstream, id: string, title: string): void {
  upstream.use(
    http.get(`${proxyBaseUrl}/api/2.0/files/file/${id}`, () =>
      docspace({ id: Number(id), title, folderId: 3, fileExst: ".docx" }),
    ),
  );
}

export interface ProviderCall {
  path: string;
  headers: Record<string, string>;
  body: string;
  signal: AbortSignal;
}

/** The provider (or the gateway) at `url`: records each call and answers with `answer`. */
export function useProvider(
  upstream: Upstream,
  url: string,
  answer: () => Response | Promise<Response>,
): ProviderCall[] {
  const calls: ProviderCall[] = [];
  upstream.use(
    http.post(url, async ({ request }) => {
      const target = new URL(request.url);
      calls.push({
        path: target.pathname + target.search,
        headers: Object.fromEntries(request.headers),
        body: await request.text(),
        signal: request.signal,
      });
      return answer();
    }),
  );
  return calls;
}

export const completion = {
  id: "chatcmpl-1",
  object: "chat.completion",
  created: 1767225600,
  model: "gpt-4o",
  choices: [{ index: 0, message: { role: "assistant", content: "Hi" }, finish_reason: "stop" }],
};

export const imageResult = { created: 1767225600, data: [{ url: "https://cdn.test/cat.png" }] };

export function envelope(message: string, type: string): unknown {
  return { error: { message, type, code: null, param: null } };
}
