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

import { inRequestContext } from "../helpers/http.js";
import { setChatContextSnapshot } from "../../requestContext.js";
import type { ChatContextScope, ChatContextSnapshot } from "../../storage/chatContextSnapshot.js";

/** A global scope with nothing in it; the fields a test needs are overridden. */
export function scopeOf(overrides: Partial<ChatContextScope> = {}): ChatContextScope {
  return {
    entityId: undefined,
    folder: undefined,
    assignments: {},
    reasoningLevel: null,
    toolPrefs: {},
    mcpServers: {},
    ...overrides,
  };
}

/** A primed snapshot of a round with nothing in it; the fields a test needs are overridden. */
export function snapshotOf(overrides: Partial<ChatContextSnapshot> = {}): ChatContextSnapshot {
  return {
    requested: { threadId: undefined, entityId: undefined, contextEntityId: undefined },
    aiReady: true,
    toolPermissionMode: null,
    profiles: [],
    global: scopeOf(),
    entity: null,
    contextEntity: null,
    thread: null,
    messages: null,
    webSearch: null,
    documentAgentEnabled: undefined,
    stale: new Set(),
    ...overrides,
  };
}

/** Runs `fn` in a real request context whose snapshot is `snapshot` (none when undefined). */
export function inRound<T>(
  snapshot: ChatContextSnapshot | undefined,
  fn: () => Promise<T>,
  headers: Record<string, string> = {},
): Promise<T> {
  return inRequestContext(headers, () => {
    setChatContextSnapshot(snapshot);
    return fn();
  });
}

export const PROFILE_ID = "6f1c2a3b-0000-4000-8000-000000000001";
export const THREAD_ID = "b1d2e3f4-0000-4000-8000-000000000002";
export const MESSAGE_ID = "c1d2e3f4-0000-4000-8000-000000000003";

/**
 * `ChatContextDto` as `GET internal/ai/chat-context` answers it
 * (products/ASC.AI/Server/Api/ChatContextController.cs ReadAsync,
 * products/ASC.AI/Server/Models/ResponseDto/ChatContextDto.cs; `config` is
 * products/ASC.AI/Server/Models/ResponseDto/AiSettingsDto.cs, a profile
 * ProfileDto.cs, the thread ThreadDto.cs, a message MessageDto.cs, the web
 * search WebSearchConfigDto.cs, a tool preference
 * common/ASC.AI.Integration/ToolPrefs/ToolPreference.cs).
 */
export function chatContextDto(): Record<string, unknown> {
  return {
    config: {
      vectorizationEnabled: true,
      vectorizationNeedReset: false,
      aiReady: true,
      embeddingModel: "text-embedding-3-small",
      systemAiEnabled: true,
      recommendedModelForForms: null,
      toolPermissionMode: 1,
    },
    profiles: [
      {
        id: PROFILE_ID,
        name: "GPT-4o",
        providerType: "OpenAi",
        baseUrl: "https://api.openai.com/v1",
        key: null,
        modelId: "gpt-4o",
        reasoning: null,
        capabilities: 257,
        useResponsesApi: null,
        canUseTool: true,
        byokProvider: null,
        createdAt: 1_700_000_000_000,
      },
    ],
    global: {
      entityId: null,
      folder: null,
      assignments: { Chat: PROFILE_ID },
      preferences: { depth: "high" },
      toolPrefs: { "docspace-integration": { disabled: ["generate_docx"], allowAlways: null } },
      mcpServers: [{ name: "jira", config: '{"url":"https://mcp.example.com/jira"}' }],
    },
    entity: {
      entityId: "42",
      folder: {
        id: 42,
        title: "Launch plan",
        folderType: 40,
        isAgent: false,
        prompt: null,
        canCreate: true,
      },
      assignments: {},
      preferences: null,
      toolPrefs: {},
      mcpServers: [],
    },
    contextEntity: {
      entityId: "43",
      folder: {
        id: 43,
        title: "Docs",
        folderType: 1,
        isAgent: false,
        prompt: null,
        canCreate: false,
      },
      assignments: {},
      preferences: null,
      toolPrefs: {},
      mcpServers: [],
    },
    thread: {
      id: THREAD_ID,
      title: "Quarterly report",
      profileId: PROFILE_ID,
      entityId: "42",
      lastEditDate: 1_700_000_001_000,
      createdAt: 1_700_000_000_500,
    },
    messages: [
      {
        id: MESSAGE_ID,
        threadId: THREAD_ID,
        contents: '{"role":"user","content":[{"type":"text","text":"hi"}]}',
        timestamp: 1_700_000_002_000,
      },
    ],
    webSearch: { provider: "exa", key: "exa-key", baseUrl: null },
    documentAgentEnabled: true,
  };
}
