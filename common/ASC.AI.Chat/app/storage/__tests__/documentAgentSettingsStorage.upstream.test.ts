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

import { describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { docspace, docspaceError, http, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { setChatContextSnapshot } from "../../requestContext.js";
import {
  DOCUMENT_AGENT_UNREADABLE,
  isDocumentAgentEnabled,
  readDocumentAgentSettings,
  readDocumentAgentSettingsForTask,
} from "../documentAgentSettingsStorage.js";
import { snapshotOf } from "../../__tests__/fixtures/chatContext.js";

// GET internal/ai/document-agent/settings: products/ASC.AI/Server/Api/DocumentAgentController.cs.
const upstream = useUpstream();

/**
 * `DocumentAgentSettingsDto` (products/ASC.AI/Server/Models/ResponseDto/DocumentAgentSettingsDto.cs);
 * `reasoning` is `DocumentAgentReasoning`, written camelCase by its converter
 * (common/ASC.AI.Integration/DocumentAgent/DocumentAgentSettings.cs).
 */
const settingsDto = (overrides: Record<string, unknown> = {}) => ({
  enabled: true,
  profileId: "6f1c2a3b-0000-4000-8000-000000000001",
  reasoning: "medium",
  maxToolCalls: 40,
  maxFileBytes: 1_048_576,
  taskLog: true,
  taskLogFolderId: "9",
  ...overrides,
});

const settings = {
  enabled: true,
  profileId: "6f1c2a3b-0000-4000-8000-000000000001",
  reasoning: "medium",
  maxToolCalls: 40,
  maxFileBytes: 1_048_576,
  taskLog: true,
  taskLogFolderId: "9",
};

function answer(body: unknown) {
  upstream.use(http.get("*/internal/ai/document-agent/settings", () => docspace(body as never)));
}

describe("readDocumentAgentSettings", () => {
  it("maps a full DocumentAgentSettingsDto", async () => {
    answer(settingsDto());
    await expect(readDocumentAgentSettings()).resolves.toEqual(settings);
    expect(upstream.seen).toMatchObject([
      { method: "GET", path: "/internal/ai/document-agent/settings" },
    ]);
  });

  it.each([
    ["profileId", undefined, { profileId: null }],
    ["profileId", null, { profileId: null }],
    ["taskLogFolderId", undefined, { taskLogFolderId: null }],
    ["taskLogFolderId", null, { taskLogFolderId: null }],
    ["taskLogFolderId", 12, { taskLogFolderId: "12" }],
    ["maxToolCalls", 500, { maxToolCalls: 150 }],
    ["maxFileBytes", 64 * 1024 * 1024, { maxFileBytes: 32 * 1024 * 1024 }],
    ["reasoning", "default", { reasoning: "default" }],
    ["reasoning", "off", { reasoning: "off" }],
  ])("reads %s %j", async (field, value, expected) => {
    const dto: Record<string, unknown> = settingsDto({ [field]: value });
    if (value === undefined) {
      delete dto[field];
    }
    answer(dto);
    await expect(readDocumentAgentSettings()).resolves.toEqual({ ...settings, ...expected });
  });

  it.each([
    ["enabled", undefined],
    ["enabled", null],
    ["enabled", "true"],
    ["profileId", ""],
    ["profileId", 5],
    ["reasoning", undefined],
    ["reasoning", null],
    ["reasoning", "xhigh"],
    ["reasoning", "Medium"],
    ["maxToolCalls", undefined],
    ["maxToolCalls", 0],
    ["maxToolCalls", 1.5],
    ["maxToolCalls", "40"],
    ["maxFileBytes", null],
    ["maxFileBytes", -1],
    ["taskLog", undefined],
    ["taskLog", 1],
    ["taskLogFolderId", ""],
    ["taskLogFolderId", 1.5],
    ["taskLogFolderId", true],
  ])("throws when %s is %j", async (field, value) => {
    const dto: Record<string, unknown> = settingsDto({ [field]: value });
    if (value === undefined) {
      delete dto[field];
    }
    answer(dto);
    await expect(readDocumentAgentSettings()).rejects.toThrow();
  });

  it.each([null, "settings", []])("throws on an answer %j", async (body) => {
    answer(body);
    await expect(readDocumentAgentSettings()).rejects.toThrow();
  });

  it.each([403, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/document-agent/settings", () => docspaceError(status)));
    await expect(readDocumentAgentSettings()).rejects.toMatchObject({ status });
  });
});

describe("readDocumentAgentSettingsForTask", () => {
  it("turns the agent off when the AI service answers 500, with a warning", async () => {
    upstream.use(http.get("*/internal/ai/document-agent/settings", () => docspaceError(500)));
    await expect(readDocumentAgentSettingsForTask()).resolves.toEqual(DOCUMENT_AGENT_UNREADABLE);
    expect(String(vi.mocked(logger.warn).mock.calls[0]?.[0])).toMatch(
      /^documentAgentSettings: not read, the agent is off: AI service 500 /,
    );
  });

  it("turns the agent off for a read that rejects with something other than an Error", async () => {
    await expect(
      readDocumentAgentSettingsForTask(() => Promise.reject("down" as unknown as Error)),
    ).resolves.toEqual(DOCUMENT_AGENT_UNREADABLE);
    expect(vi.mocked(logger.warn).mock.calls).toEqual([
      ["documentAgentSettings: not read, the agent is off: down"],
    ]);
  });
});

describe("isDocumentAgentEnabled in the real request context", () => {
  it("answers the aggregate's value without a request", async () => {
    const enabled = await inRequestContext({}, async () => {
      setChatContextSnapshot(snapshotOf({ documentAgentEnabled: false }));
      return isDocumentAgentEnabled();
    });
    expect(enabled).toBe(false);
    expect(upstream.seen).toEqual([]);
  });

  it("reads the settings when the aggregate does not say", async () => {
    answer(settingsDto({ enabled: false }));
    const enabled = await inRequestContext({}, async () => {
      setChatContextSnapshot(snapshotOf());
      return isDocumentAgentEnabled();
    });
    expect(enabled).toBe(false);
    expect(upstream.seen).toHaveLength(1);
  });
});
