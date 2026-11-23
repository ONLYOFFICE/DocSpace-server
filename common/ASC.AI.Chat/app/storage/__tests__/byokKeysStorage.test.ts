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

import { describe, expect, it } from "vitest";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { HttpByokKeysStorage } from "../byokKeysStorage.js";
import { inRound, snapshotOf } from "./chatContext.fixtures.js";

// products/ASC.AI/Server/Api/ByokKeysController.cs: GET byok/providers, GET byok, PUT byok/{id},
// DELETE byok/{id}.
const upstream = useUpstream();
const storage = new HttpByokKeysStorage();
const noContent = () => new HttpResponse(null, { status: 204 });

const MODEL_ID = "d1d2d3d4-0000-4000-8000-000000000030";

/** `ByokKeyDto` (products/ASC.AI/Server/Models/ResponseDto/ByokProviderDto.cs). */
const keyDto = (overrides: Record<string, unknown> = {}) => ({
  id: MODEL_ID,
  name: "Claude Sonnet",
  provider: "anthropic",
  image: "<svg/>",
  ...overrides,
});

/** `ByokProviderDto` with its `ByokModelDto` list (same file). */
const providerDto = (overrides: Record<string, unknown> = {}) => ({
  id: "anthropic",
  name: "Anthropic",
  image: "<svg/>",
  models: [{ id: MODEL_ID, name: "Claude Sonnet" }],
  ...overrides,
});

describe("readAll", () => {
  it("maps a full ByokKeyDto list", async () => {
    upstream.use(http.get("*/internal/ai/byok", () => docspace([keyDto()])));
    await expect(storage.readAll()).resolves.toEqual([
      { id: MODEL_ID, name: "Claude Sonnet", provider: "anthropic", image: "<svg/>" },
    ]);
    expect(upstream.seen).toMatchObject([{ method: "GET", path: "/internal/ai/byok" }]);
  });

  it.each([
    ["id missing", keyDto({ id: undefined })],
    ["id null", keyDto({ id: null })],
    ["id a number", keyDto({ id: 1 })],
    ["provider missing", keyDto({ provider: undefined })],
    ["provider null", keyDto({ provider: null })],
    ["provider a number", keyDto({ provider: 1 })],
    ["not an object", "key"],
    ["null", null],
  ])("drops a key with %s", async (_name, item) => {
    upstream.use(http.get("*/internal/ai/byok", () => docspace([item, keyDto({ id: "k2" })])));
    const keys = await storage.readAll();
    expect(keys.map((k) => k.id)).toEqual(["k2"]);
  });

  it.each([
    ["name", null],
    ["name", 5],
    ["image", null],
    ["image", false],
  ])("leaves %s %j out", async (field, value) => {
    upstream.use(http.get("*/internal/ai/byok", () => docspace([keyDto({ [field]: value })])));
    const [key] = await storage.readAll();
    expect(key).not.toHaveProperty(field);
  });

  it("keeps the named fields only", async () => {
    upstream.use(http.get("*/internal/ai/byok", () => docspace([keyDto({ key: "sk-secret" })])));
    const [key] = await storage.readAll();
    expect(Object.keys(key ?? {}).sort()).toEqual(["id", "image", "name", "provider"]);
  });

  // DEFECT stor2-9: a GET byok or GET byok/providers answer that is not a list (an object, null, an empty body) is read as "no key" / "no provider" — expected it to be rethrown as unknown, since the C# actions always answer a list (ts-ai-failure-paths.md §1)
  it.each([
    ["an object", () => docspace({ items: [keyDto()] })],
    ["null", () => docspace(null)],
    ["an empty answer", noContent],
  ])("reads %s as no key", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/byok", answer));
    await expect(storage.readAll()).resolves.toEqual([]);
  });

  it.each([403, 500])("rethrows a %i", async (status) => {
    upstream.use(http.get("*/internal/ai/byok", () => docspaceError(status)));
    await expect(storage.readAll()).rejects.toMatchObject({ status });
  });
});

describe("readProviders", () => {
  it("maps a full ByokProviderDto list", async () => {
    upstream.use(http.get("*/internal/ai/byok/providers", () => docspace([providerDto()])));
    await expect(storage.readProviders()).resolves.toEqual([
      {
        id: "anthropic",
        name: "Anthropic",
        image: "<svg/>",
        models: [{ id: MODEL_ID, name: "Claude Sonnet" }],
      },
    ]);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/byok/providers");
  });

  it.each([
    ["id missing", providerDto({ id: undefined })],
    ["id null", providerDto({ id: null })],
    ["name missing", providerDto({ name: undefined })],
    ["name a number", providerDto({ name: 1 })],
    ["not an object", 7],
  ])("drops a provider with %s", async (_name, item) => {
    upstream.use(
      http.get("*/internal/ai/byok/providers", () => docspace([item, providerDto({ id: "p2" })])),
    );
    const providers = await storage.readProviders();
    expect(providers.map((p) => p.id)).toEqual(["p2"]);
  });

  it.each([
    ["missing", undefined, []],
    ["null", null, []],
    ["an object", { id: "m" }, []],
    [
      "with invalid entries",
      [null, "m", { id: "m1" }, { name: "n" }, { id: 1, name: "n" }, { id: "m2", name: "Model 2" }],
      [{ id: "m2", name: "Model 2" }],
    ],
  ])("reads models %s", async (_name, models, expected) => {
    upstream.use(
      http.get("*/internal/ai/byok/providers", () => docspace([providerDto({ models })])),
    );
    const [provider] = await storage.readProviders();
    expect(provider?.models).toEqual(expected);
  });

  it.each([null, 1])("leaves an image %j out", async (image) => {
    upstream.use(
      http.get("*/internal/ai/byok/providers", () => docspace([providerDto({ image })])),
    );
    const [provider] = await storage.readProviders();
    expect(provider).toEqual({
      id: "anthropic",
      name: "Anthropic",
      models: [{ id: MODEL_ID, name: "Claude Sonnet" }],
    });
  });

  it.each([
    ["an object", () => docspace({})],
    ["an empty answer", noContent],
  ])("reads %s as no provider", async (_name, answer) => {
    upstream.use(http.get("*/internal/ai/byok/providers", answer));
    await expect(storage.readProviders()).resolves.toEqual([]);
  });

  it("rethrows a 500", async () => {
    upstream.use(http.get("*/internal/ai/byok/providers", () => docspaceError(500)));
    await expect(storage.readProviders()).rejects.toMatchObject({ status: 500 });
  });
});

describe("set", () => {
  it("puts the provider and the key by the encoded id, answers the key without the secret and marks the profiles stale", async () => {
    upstream.use(http.put("*/internal/ai/byok/:id", () => docspace(keyDto())));
    const snapshot = snapshotOf();

    const key = await inRound(snapshot, () =>
      storage.set("m/1", { provider: "anthropic", key: "sk-ant-secret" }),
    );

    expect(key).toEqual({
      id: MODEL_ID,
      name: "Claude Sonnet",
      provider: "anthropic",
      image: "<svg/>",
    });
    expect(upstream.seen).toMatchObject([
      {
        method: "PUT",
        path: "/internal/ai/byok/m%2F1",
        body: { provider: "anthropic", key: "sk-ant-secret" },
      },
    ]);
    expect([...snapshot.stale]).toEqual(["profiles"]);
  });

  it.each([
    ["an empty answer", noContent],
    ["a key without provider", () => docspace(keyDto({ provider: undefined }))],
    ["a list", () => docspace([keyDto()])],
  ])("throws on %s, after marking the profiles stale", async (_name, answer) => {
    upstream.use(http.put("*/internal/ai/byok/:id", answer));
    const snapshot = snapshotOf();
    await expect(
      inRound(snapshot, () => storage.set("m", { provider: "anthropic", key: "k" })),
    ).rejects.toThrow("ai service returned invalid byok key");
    expect([...snapshot.stale]).toEqual(["profiles"]);
  });

  it("rethrows a refusal and leaves the profiles fresh", async () => {
    upstream.use(http.put("*/internal/ai/byok/:id", () => docspaceError(400)));
    const snapshot = snapshotOf();
    await expect(
      inRound(snapshot, () => storage.set("m", { provider: "anthropic", key: "k" })),
    ).rejects.toMatchObject({ status: 400 });
    expect(snapshot.stale.size).toBe(0);
  });
});

describe("delete", () => {
  it("deletes by the encoded id and marks the profiles stale afterwards", async () => {
    upstream.use(http.delete("*/internal/ai/byok/:id", noContent));
    const snapshot = snapshotOf();
    await inRound(snapshot, () => storage.delete("m/1"));
    expect(upstream.seen).toMatchObject([{ method: "DELETE", path: "/internal/ai/byok/m%2F1" }]);
    expect([...snapshot.stale]).toEqual(["profiles"]);
  });

  it.each([404, 500])("rethrows a %i and leaves the profiles fresh", async (status) => {
    upstream.use(http.delete("*/internal/ai/byok/:id", () => docspaceError(status)));
    const snapshot = snapshotOf();
    await expect(inRound(snapshot, () => storage.delete("m"))).rejects.toMatchObject({ status });
    expect(snapshot.stale.size).toBe(0);
  });
});
