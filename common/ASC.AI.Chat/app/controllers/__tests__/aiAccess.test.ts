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

// The AI access gate a route borrows from the AI service: a throwaway read of the caller's
// tool preferences, which the AI service answers only for a caller who may use AI.

import { describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import {
  HttpResponse,
  delay,
  docspace,
  docspaceError,
  http,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { AiServiceHttpError } from "../../storage/httpClient.js";
import { assertAiAccess } from "../aiAccess.js";

const upstream = useUpstream();

// GET internal/ai/tool-prefs is ToolPreferenceStorageController.ReadAsync
// (products/ASC.AI/Server/Api/ToolPreferenceStorageController.cs), behind [AiFeature]: the
// caller's preferences by server type.
const PREFS = {
  "docspace-integration": { disabled: ["onlyoffice_generate_docx"], allowAlways: [] },
  "my-server": { disabled: [], allowAlways: ["search"] },
};

const prefs = (answer: () => Response | Promise<Response>) =>
  upstream.use(http.get("*/internal/ai/tool-prefs", answer));

describe("assertAiAccess", () => {
  it("lets the caller through when the AI service answers the read", async () => {
    prefs(() => docspace(PREFS));

    await expect(assertAiAccess()).resolves.toBeUndefined();

    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      "GET /internal/ai/tool-prefs",
    ]);
  });

  it("forwards the caller's credentials and leaves the entity out of the read", async () => {
    prefs(() => docspace(PREFS));

    await inRequestContext(
      { cookie: "asc_auth_key=abc", authorization: "Bearer t", "x-session-id": "s" },
      () => assertAiAccess("12"),
    );

    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/tool-prefs");
    expect(upstream.seen[0]?.headers).toMatchObject({
      cookie: "asc_auth_key=abc",
      authorization: "Bearer t",
    });
    expect(upstream.seen[0]?.headers["x-session-id"]).toBeUndefined();
  });

  it("lets the caller through when nothing is stored yet (404)", async () => {
    prefs(() => docspaceError(404, "not found"));

    await expect(assertAiAccess()).resolves.toBeUndefined();
  });

  it("lets the caller through on an empty answer", async () => {
    prefs(() => new HttpResponse(null, { status: 204 }));

    await expect(assertAiAccess()).resolves.toBeUndefined();
  });

  // A check that protects access fails closed (ts-ai-failure-paths.md §1): a 200 whose body the
  // read cannot take is unknown, not a pass.
  it("refuses with 502 on a body that is not an object", async () => {
    prefs(() => docspace("x"));

    await expect(assertAiAccess()).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
    });
  });

  it.each([
    ["403 for a guest or AI switched off", 403, "Forbidden"],
    ["401 without a session", 401, "Unauthorized"],
    ["500", 500, "Internal Server Error"],
    ["503", 503, "Service Unavailable"],
  ])("refuses with the AI service's status when it answers %s", async (_what, status, reason) => {
    prefs(() => docspaceError(status, "AI is not available"));

    const error = await assertAiAccess().then(
      () => undefined,
      (err: unknown) => err,
    );

    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect(error).toMatchObject({ status, statusText: reason });
  });

  it("fails the gate when the read times out after five minutes", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    prefs(async () => {
      await delay("infinite");
      return docspace(PREFS);
    });

    try {
      const outcome = expect(assertAiAccess()).rejects.toThrow("upstream timeout after 300000ms");
      await vi.advanceTimersByTimeAsync(300_000);
      await outcome;
    } finally {
      vi.useRealTimers();
    }
  });
});
