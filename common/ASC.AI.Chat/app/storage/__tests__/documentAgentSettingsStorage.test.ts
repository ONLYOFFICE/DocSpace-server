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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { beforeEach, describe, expect, it, vi } from "vitest";
import logger from "../../log.js";

// The logger is the stub of app/__tests__/setup.ts.
const warn = vi.mocked(logger.warn);

const state = vi.hoisted(() => ({
  gets: [] as string[],
  answer: undefined as unknown,
  failure: undefined as Error | undefined,
  snapshot: undefined as { documentAgentEnabled: boolean | undefined } | undefined,
}));

vi.mock("../../requestContext.js", () => ({
  getChatContextSnapshot: () => state.snapshot,
}));
vi.mock("../httpClient.js", () => ({
  aiService: {
    get: async (path: string) => {
      state.gets.push(path);
      if (state.failure) {
        throw state.failure;
      }
      return state.answer;
    },
  },
}));

const {
  DOCUMENT_AGENT_DEFAULTS,
  DOCUMENT_AGENT_UNREADABLE,
  isDocumentAgentEnabled,
  parseDocumentAgentSettings,
  readDocumentAgentSettings,
  readDocumentAgentSettingsForTask,
} = await import("../documentAgentSettingsStorage.js");

const SAVED = {
  ...DOCUMENT_AGENT_DEFAULTS,
  enabled: false,
  profileId: "p-1",
  reasoning: "high" as const,
  maxToolCalls: 40,
  taskLog: true,
  taskLogFolderId: "9",
};

beforeEach(() => {
  state.gets = [];
  state.answer = undefined;
  state.failure = undefined;
  state.snapshot = undefined;
  warn.mockClear();
});

describe("parseDocumentAgentSettings", () => {
  it("takes the settings as the AI service answers them", () => {
    expect(parseDocumentAgentSettings(SAVED)).toEqual(SAVED);
  });

  it("reads a reference the answer leaves out as none, and a numeric folder id as its text", () => {
    const { profileId: _profileId, taskLogFolderId: _folderId, ...bare } = SAVED;

    expect(parseDocumentAgentSettings(bare)).toEqual({
      ...SAVED,
      profileId: null,
      taskLogFolderId: null,
    });
    expect(parseDocumentAgentSettings({ ...SAVED, taskLogFolderId: 9 }).taskLogFolderId).toBe("9");
  });

  it("lowers a limit above the agent's ceiling to the ceiling", () => {
    const parsed = parseDocumentAgentSettings({
      ...SAVED,
      maxToolCalls: 2_147_483_647,
      maxFileBytes: 2_147_483_647,
    });

    expect(parsed.maxToolCalls).toBe(DOCUMENT_AGENT_DEFAULTS.maxToolCalls);
    expect(parsed.maxFileBytes).toBe(DOCUMENT_AGENT_DEFAULTS.maxFileBytes);
  });

  it("throws on an answer it cannot read", () => {
    for (const raw of [
      null,
      "nonsense",
      { ...SAVED, reasoning: "xhigh" },
      { ...SAVED, enabled: 1 },
    ]) {
      expect(() => parseDocumentAgentSettings(raw), JSON.stringify(raw)).toThrow();
    }
  });
});

describe("readDocumentAgentSettings", () => {
  it("asks the AI service", async () => {
    state.answer = SAVED;

    expect(await readDocumentAgentSettings()).toEqual(SAVED);
    expect(state.gets).toEqual(["/document-agent/settings"]);
  });
});

describe("readDocumentAgentSettingsForTask", () => {
  it("is the portal's saved settings", async () => {
    state.answer = SAVED;

    expect(await readDocumentAgentSettingsForTask()).toEqual(SAVED);
    expect(warn).not.toHaveBeenCalled();
  });

  it("turns the agent off while the settings cannot be read", async () => {
    for (const broken of [
      () => (state.failure = new Error("connect ECONNREFUSED")),
      () => (state.answer = { nonsense: true }),
    ]) {
      state.answer = undefined;
      state.failure = undefined;
      warn.mockClear();
      broken();

      const settings = await readDocumentAgentSettingsForTask();

      expect(settings).toEqual(DOCUMENT_AGENT_UNREADABLE);
      expect(settings.enabled).toBe(false);
      expect(warn).toHaveBeenCalledTimes(1);
    }
  });
});

describe("isDocumentAgentEnabled", () => {
  it("is what the round's aggregate says, with no request", async () => {
    state.answer = { ...SAVED, enabled: true };

    state.snapshot = { documentAgentEnabled: false };
    expect(await isDocumentAgentEnabled()).toBe(false);
    state.snapshot = { documentAgentEnabled: true };
    expect(await isDocumentAgentEnabled()).toBe(true);
    expect(state.gets).toEqual([]);
  });

  it("reads the settings outside a round, and under an aggregate that does not say", async () => {
    state.answer = { ...SAVED, enabled: true };
    expect(await isDocumentAgentEnabled()).toBe(true);

    state.snapshot = { documentAgentEnabled: undefined };
    state.answer = SAVED;
    expect(await isDocumentAgentEnabled()).toBe(false);
    expect(state.gets).toHaveLength(2);
  });

  it("is off while the settings cannot be read", async () => {
    state.failure = new Error("boom");

    expect(await isDocumentAgentEnabled()).toBe(false);
  });
});
