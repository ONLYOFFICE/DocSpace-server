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

import { beforeEach, describe, expect, it, vi } from "vitest";

const state = vi.hoisted(() => ({
  calls: [] as { method: string; path: string; body?: unknown; raw?: boolean }[],
  answer: undefined as unknown,
  failure: undefined as Error | undefined,
}));

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
  logDir: "/nowhere",
}));
vi.mock("../../../config/index.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../../config/index.js")>()),
  default: { get: () => undefined },
  getDocumentAgent: () => undefined,
  getAppConfig: () => ({ port: 1 }),
}));
vi.mock("../../requestContext.js", () => ({
  getForwardedHeaders: () => ({ host: "portal.example.com" }),
  getChatContextSnapshot: () => undefined,
}));
vi.mock("../../storage/httpClient.js", async (importOriginal) => {
  const original = await importOriginal<typeof import("../../storage/httpClient.js")>();
  const answer = (call: (typeof state.calls)[number]) => {
    state.calls.push(call);
    if (state.failure) {
      throw state.failure;
    }
    return state.answer;
  };
  return {
    ...original,
    aiService: {
      get: async (path: string, opts?: { raw?: boolean }) =>
        answer({ method: "GET", path, raw: opts?.raw }),
      put: async (path: string, body: unknown, opts?: { raw?: boolean }) =>
        answer({ method: "PUT", path, body, raw: opts?.raw }),
    },
  };
});

const { AiServiceHttpError } = await import("../../storage/httpClient.js");
const { DOCUMENT_AGENT_DEFAULTS } = await import("../../storage/documentAgentSettingsStorage.js");
const { documentAgentController } = await import("../documentAgentController.js");
const { handlerSettled } = await import("../_helpers.js");

type Handler = (req: unknown, res: unknown, next: unknown) => void;

async function run(handler: unknown, body?: unknown) {
  const res = {
    statusCode: 200,
    body: undefined as unknown,
    headersSent: false,
    status(code: number) {
      this.statusCode = code;
      return this;
    },
    json(payload: unknown) {
      this.body = payload;
      this.headersSent = true;
      return this;
    },
    end() {
      this.headersSent = true;
    },
  };
  (handler as Handler)({ body, method: "PUT", originalUrl: "/x" }, res, () => {});
  await handlerSettled(res);
  return res;
}

const SETTINGS = {
  ...DOCUMENT_AGENT_DEFAULTS,
  enabled: false,
  profileId: "p-1",
  reasoning: "low",
  maxToolCalls: 10,
  maxFileBytes: 1024,
  taskLog: true,
  taskLogFolderId: "17",
};

const envelope = (response: unknown) => ({ response, count: 1, status: 0, statusCode: 200 });

beforeEach(() => {
  state.calls = [];
  state.answer = undefined;
  state.failure = undefined;
});

describe("documentAgentController", () => {
  it("reads the AI service's settings, envelope and all", async () => {
    state.answer = envelope(DOCUMENT_AGENT_DEFAULTS);

    const res = await run(documentAgentController.getSettings);

    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual(envelope(DOCUMENT_AGENT_DEFAULTS));
    expect(state.calls).toEqual([{ method: "GET", path: "/document-agent/settings", raw: true }]);
  });

  it("hands what the tab sends to the AI service and answers what it stored", async () => {
    state.answer = envelope(SETTINGS);

    const res = await run(documentAgentController.setSettings, SETTINGS);

    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual(envelope(SETTINGS));
    expect(state.calls).toEqual([
      { method: "PUT", path: "/document-agent/settings", body: SETTINGS, raw: true },
    ]);
  });

  it("answers the AI service's refusal with its status", async () => {
    state.failure = new AiServiceHttpError(403, "Forbidden", "", "http://ai/x");

    const res = await run(documentAgentController.setSettings, SETTINGS);

    expect(res.statusCode).toBe(403);
  });

  it("refuses a limit over the agent's ceiling, an unknown level, a missing field", async () => {
    for (const body of [
      { ...SETTINGS, maxToolCalls: 151 },
      { ...SETTINGS, maxFileBytes: 32 * 1024 * 1024 + 1 },
      { ...SETTINGS, reasoning: "xhigh" },
      { ...SETTINGS, taskLog: undefined },
      { ...SETTINGS, profileId: "" },
      "nonsense",
    ]) {
      const res = await run(documentAgentController.setSettings, body);
      expect(res.statusCode, JSON.stringify(body)).toBe(400);
      expect(String((res.body as { error: string }).error)).toMatch(/^Invalid settings: /);
    }
    expect(state.calls).toEqual([]);
  });
});
