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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Profile } from "@onlyoffice/ai-chat/core";
import {
  delay,
  docspace,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import {
  getChatContextMisses,
  getUpstreamCalls,
  getUpstreamReadCount,
  markForwardHeadersToProvider,
} from "../../requestContext.js";
import { AiServiceHttpError, aiServiceBaseUrl } from "../httpClient.js";
import {
  dtoToProfile,
  HttpProfilesStorage,
  listOnlyofficeGatewayModels,
  withOnlyofficeProviderOverrides,
} from "../profilesStorage.js";
import type { DocspaceProfile } from "../profilesStorage.js";
import { inRequest, snapshotWithFolders } from "../../__tests__/fixtures/filesApi.js";

// The profile storage over a stubbed AI service, with the real httpClient.js and
// requestContext.js. Routes: ProfileStorageController (products/ASC.AI/Server/Api/
// ProfileStorageController.cs) under `internal/ai/profiles`, and the gateway proxy
// `GET api/2.0/ai/gateway/models` of the AI service.
const upstream = useUpstream();
const PROFILES = "*/internal/ai/profiles";
const ID = "0f2c6d8e-1a2b-4c3d-9e8f-123456789abc";

/** `ProfileDto` (products/ASC.AI/Server/Models/ResponseDto/ProfileDto.cs), as the AI service serves it. */
function profileDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    id: ID,
    name: "GPT",
    providerType: "openai",
    baseUrl: "https://api.openai.com/v1",
    key: "sk-secret",
    modelId: "gpt-4o",
    reasoning: { thinks: true, canDisable: true, depths: ["low", "high"], defaultDepth: "low" },
    capabilities: 385,
    useResponsesApi: false,
    canUseTool: true,
    byokProvider: null,
    createdAt: 1763900000000,
    ...overrides,
  };
}

const profileOf = (overrides: Partial<DocspaceProfile> = {}): DocspaceProfile => ({
  id: ID,
  name: "GPT",
  providerType: "openai",
  baseUrl: "https://api.openai.com/v1",
  key: "sk-secret",
  modelId: "gpt-4o",
  reasoning: true,
  reasoningSupport: {
    thinks: true,
    canDisable: true,
    depths: ["low", "high"],
    defaultDepth: "low",
  },
  capabilities: 385,
  useResponsesApi: false,
  canUseTool: true,
  createdAt: 1763900000000,
  ...overrides,
});

const snapshotWith = (profiles: DocspaceProfile[], stale: string[] = []) => {
  const snapshot = snapshotWithFolders();
  snapshot.profiles = profiles;
  snapshot.stale = new Set(stale as never[]);
  return snapshot;
};

const failure = (p: Promise<unknown>): Promise<unknown> =>
  p.then(
    () => {
      throw new Error("expected a rejection");
    },
    (e: unknown) => e,
  );

beforeEach(() => {
  vi.mocked(logger.info).mockClear();
  vi.mocked(logger.warn).mockClear();
});

describe("dtoToProfile", () => {
  it("maps a full ProfileDto", () => {
    expect(dtoToProfile(profileDto())).toStrictEqual(profileOf());
  });

  it.each([null, "x", 5, [profileDto()], undefined])("answers undefined for %j", (raw) => {
    expect(dtoToProfile(raw)).toBeUndefined();
  });

  it.each(["id", "name", "providerType", "baseUrl", "modelId", "createdAt"])(
    "answers undefined when the required %s is missing, null or of the wrong type",
    (field) => {
      const wrong = field === "createdAt" ? "1763900000000" : 42;
      for (const value of [undefined, null, wrong]) {
        expect(dtoToProfile(profileDto({ [field]: value }))).toBeUndefined();
      }
    },
  );

  it("keeps empty strings of the required fields", () => {
    expect(
      dtoToProfile(profileDto({ name: "", baseUrl: "", modelId: "", createdAt: 0 })),
    ).toMatchObject({ name: "", baseUrl: "", modelId: "", createdAt: 0 });
  });

  const { key: _k, ...noKey } = profileOf();
  const { reasoning: _r, reasoningSupport: _s, ...noReasoning } = profileOf();
  const { capabilities: _c, ...noCapabilities } = profileOf();
  const { canUseTool: _t, ...noTool } = profileOf();
  const { useResponsesApi: _u, ...noResponses } = profileOf();
  it.each([
    [
      "byokProvider is a string",
      { byokProvider: "anthropic" },
      profileOf({ byokProvider: "anthropic" }),
    ],
    ["byokProvider is a number", { byokProvider: 1 }, profileOf()],
    ["key is missing", { key: undefined }, noKey],
    ["key is null", { key: null }, noKey],
    ["key is a number", { key: 1 }, noKey],
    ["key is empty", { key: "" }, profileOf({ key: "" })],
    ["reasoning is missing", { reasoning: undefined }, noReasoning],
    ["reasoning is null", { reasoning: null }, noReasoning],
    ["reasoning is a string", { reasoning: "high" }, noReasoning],
    ["reasoning is a list", { reasoning: [] }, noReasoning],
    ["reasoning has no thinks", { reasoning: { canDisable: true } }, noReasoning],
    [
      "reasoning.thinks is a string",
      { reasoning: { thinks: "true", canDisable: true } },
      noReasoning,
    ],
    [
      "reasoning is the legacy true",
      { reasoning: true },
      profileOf({
        reasoning: true,
        reasoningSupport: {
          thinks: true,
          canDisable: true,
          depths: ["low", "medium", "high", "max"],
        },
      }),
    ],
    [
      "reasoning is the legacy false",
      { reasoning: false },
      profileOf({
        reasoning: false,
        reasoningSupport: { thinks: false, canDisable: false, depths: [] },
      }),
    ],
    [
      "reasoning names xhigh, none, unknown depths and a none default",
      {
        reasoning: {
          thinks: true,
          canDisable: false,
          depths: ["xhigh", "none", "minimal", "LOW", 3],
          defaultDepth: "none",
        },
      },
      profileOf({ reasoningSupport: { thinks: true, canDisable: false, depths: ["low", "max"] } }),
    ],
    [
      "reasoning.depths is not a list",
      { reasoning: { thinks: false, canDisable: true, depths: "low", defaultDepth: null } },
      profileOf({
        reasoning: false,
        reasoningSupport: { thinks: false, canDisable: true, depths: [] },
      }),
    ],
    ["capabilities is missing", { capabilities: undefined }, noCapabilities],
    ["capabilities is a string", { capabilities: "Chat" }, noCapabilities],
    ["capabilities is null", { capabilities: null }, noCapabilities],
    ["capabilities is 0", { capabilities: 0 }, profileOf({ capabilities: 0 })],
    ["canUseTool is null", { canUseTool: null }, noTool],
    ["canUseTool is a string", { canUseTool: "true" }, noTool],
    ["useResponsesApi is null", { useResponsesApi: null }, noResponses],
    ["useResponsesApi is a number", { useResponsesApi: 1 }, noResponses],
    ["useProxy is given", { useProxy: true }, profileOf({ useProxy: true })],
    ["useProxy is a string", { useProxy: "true" }, profileOf()],
    ["isCloudProvider is given", { isCloudProvider: false }, profileOf({ isCloudProvider: false })],
    ["isCloudProvider is null", { isCloudProvider: null }, profileOf()],
    ["contextWindow is given", { contextWindow: 128000 }, profileOf({ contextWindow: 128000 })],
    ["contextWindow is zero", { contextWindow: 0 }, profileOf()],
    ["contextWindow is negative", { contextWindow: -1 }, profileOf()],
    ["contextWindow is a string", { contextWindow: "128000" }, profileOf()],
    ["an unknown field is added", { headers: { "x-a": "1" }, extra: 1 }, profileOf()],
  ])("maps the DTO when %s", (_case, overrides, expected) => {
    expect(dtoToProfile(profileDto(overrides))).toStrictEqual(expected);
  });
});

describe("withOnlyofficeProviderOverrides", () => {
  it("answers undefined for no profile", () => {
    expect(withOnlyofficeProviderOverrides(undefined)).toBeUndefined();
  });

  it("points an onlyoffice profile at the gateway and keeps its own headers outside a chat round", async () => {
    const stored = profileOf({ providerType: "onlyoffice", headers: { "x-own": "1" } });

    const resolved = await inRequest(async () => withOnlyofficeProviderOverrides(stored), {
      headers: { cookie: "c=1" },
    });

    expect(resolved).toStrictEqual({
      ...stored,
      baseUrl: `${aiServiceBaseUrl}/api/2.0/ai/gateway`,
    });
    expect(resolved).not.toBe(stored);
  });

  it("merges the caller's headers under the profile's own when the round forwards them", async () => {
    const stored = profileOf({
      providerType: "onlyoffice",
      byokProvider: "openai",
      headers: { cookie: "profile-wins", "x-own": "1" },
    });

    const resolved = await inRequest(
      async () => {
        markForwardHeadersToProvider();
        return withOnlyofficeProviderOverrides(stored);
      },
      { headers: { cookie: "c=1", authorization: "Bearer t", "x-session-id": "dropped" } },
    );

    expect(resolved?.baseUrl).toBe(`${aiServiceBaseUrl}/api/2.0/ai/gateway/byok/${ID}`);
    expect(resolved?.headers).toStrictEqual({
      cookie: "profile-wins",
      authorization: "Bearer t",
      "x-own": "1",
    });
  });

  it("forwards the caller's headers to a profile without headers of its own", async () => {
    const resolved = await inRequest(
      async () => {
        markForwardHeadersToProvider();
        return withOnlyofficeProviderOverrides(profileOf({ providerType: "onlyoffice" }));
      },
      { headers: { cookie: "c=1" } },
    );

    expect(resolved?.headers).toStrictEqual({ cookie: "c=1" });
  });

  it("leaves a provider other than onlyoffice as it is, even when the round forwards headers", async () => {
    const stored = profileOf();

    const resolved = await inRequest(
      async () => {
        markForwardHeadersToProvider();
        return withOnlyofficeProviderOverrides(stored);
      },
      { headers: { cookie: "c=1" } },
    );

    expect(resolved).toBe(stored);
  });
});

describe("HttpProfilesStorage.create", () => {
  const input: Omit<Profile, "id" | "createdAt"> = {
    name: "GPT",
    providerType: "openai",
    baseUrl: "https://api.openai.com/v1",
    modelId: "gpt-4o",
  };
  const nullBody = {
    name: "GPT",
    providerType: "openai",
    baseUrl: "https://api.openai.com/v1",
    key: null,
    modelId: "gpt-4o",
    reasoning: null,
    capabilities: null,
    canUseTool: null,
    useResponsesApi: null,
    useProxy: null,
    isCloudProvider: null,
  };

  it("posts the profile with every absent field as null and answers the stored one", async () => {
    upstream.use(http.post(PROFILES, () => docspace(profileDto())));

    await expect(new HttpProfilesStorage().create(input)).resolves.toStrictEqual(profileOf());

    expect(upstream.seen).toEqual([
      expect.objectContaining({ method: "POST", path: "/internal/ai/profiles", body: nullBody }),
    ]);
  });

  it.each([
    [
      "the fields it is given, without headers or an id",
      {
        key: "k",
        capabilities: 1,
        canUseTool: false,
        useResponsesApi: true,
        useProxy: true,
        isCloudProvider: false,
        headers: { a: "1" },
        contextWindow: 5,
      },
      {
        key: "k",
        capabilities: 1,
        canUseTool: false,
        useResponsesApi: true,
        useProxy: true,
        isCloudProvider: false,
      },
    ],
    [
      "the reasoning support it is given, as a ReasoningConfig",
      {
        reasoning: false,
        reasoningSupport: {
          thinks: true,
          canDisable: false,
          depths: ["low", "max"],
          defaultDepth: "max",
        },
      },
      {
        reasoning: { thinks: true, canDisable: false, depths: ["low", "max"], defaultDepth: "max" },
      },
    ],
    [
      "reasoning false as no reasoning",
      { reasoning: false },
      { reasoning: { thinks: false, canDisable: false, depths: [], defaultDepth: null } },
    ],
    [
      "reasoning true as the library's answer for the model",
      { reasoning: true, modelId: "o3" },
      {
        modelId: "o3",
        reasoning: {
          thinks: true,
          canDisable: false,
          depths: ["low", "medium", "high"],
          defaultDepth: null,
        },
      },
    ],
    [
      "reasoning true of an external profile as the answer for the provider it is based on",
      { reasoning: true, providerType: "external", basedOn: "openai", modelId: "o3" },
      {
        providerType: "external",
        modelId: "o3",
        reasoning: {
          thinks: true,
          canDisable: false,
          depths: ["low", "medium", "high"],
          defaultDepth: null,
        },
      },
    ],
    [
      "reasoning true of an external profile based on nothing as the answer for external",
      { reasoning: true, providerType: "external", modelId: "o3" },
      {
        providerType: "external",
        modelId: "o3",
        reasoning: {
          thinks: true,
          canDisable: true,
          depths: ["low", "medium", "high", "max"],
          defaultDepth: null,
        },
      },
    ],
  ])("sends %s", async (_case, extra, expected) => {
    upstream.use(http.post(PROFILES, () => docspace(profileDto())));

    await new HttpProfilesStorage().create({ ...input, ...(extra as Partial<Profile>) });

    expect(upstream.seen[0]?.body).toStrictEqual({ ...nullBody, ...expected });
  });

  it("refuses an answer that is not a profile, after the write", async () => {
    upstream.use(http.post(PROFILES, () => docspace({ id: ID })));

    expect(await failure(new HttpProfilesStorage().create(input))).toMatchObject({
      message: "ai service returned invalid profile",
    });
    expect(upstream.seen).toHaveLength(1);
  });

  it("marks the profiles of a primed round stale", async () => {
    upstream.use(http.post(PROFILES, () => docspace(profileDto())));
    const snapshot = snapshotWith([]);

    await inRequest(() => new HttpProfilesStorage().create(input), { snapshot });

    expect([...snapshot.stale]).toEqual(["profiles"]);
  });

  it("passes a refusal on as an AiServiceHttpError", async () => {
    upstream.use(http.post(PROFILES, () => HttpResponse.text("bad", { status: 400 })));

    expect(await failure(new HttpProfilesStorage().create(input))).toBeInstanceOf(
      AiServiceHttpError,
    );
  });
});

describe("HttpProfilesStorage.createMany", () => {
  const input: Omit<Profile, "id" | "createdAt"> = {
    name: "A",
    providerType: "openai",
    baseUrl: "u",
    modelId: "m",
  };

  it("posts the profiles in a batch and answers the stored ones it can read", async () => {
    upstream.use(
      http.post(`${PROFILES}/batch`, () =>
        docspace([profileDto(), profileDto({ id: undefined }), "x", profileDto({ id: "p2" })]),
      ),
    );

    await expect(new HttpProfilesStorage().createMany([input, input])).resolves.toStrictEqual([
      profileOf(),
      profileOf({ id: "p2" }),
    ]);
    expect(upstream.seen[0]).toMatchObject({
      method: "POST",
      path: "/internal/ai/profiles/batch",
      body: {
        profiles: [
          expect.objectContaining({ name: "A", key: null }),
          expect.objectContaining({ name: "A", key: null }),
        ],
      },
    });
  });

  // DEFECT stor1-11: an answer of `POST profiles/batch` that is not a list is answered as "nothing created", although the profiles were written — expected the unknown shape to be rethrown (ts-ai-failure-paths.md §1, §6)
  it.each([
    ["an object", { id: ID }],
    ["null", null],
    ["a string", "ok"],
  ])("answers an empty list when the answer is %s", async (_case, response) => {
    upstream.use(http.post(`${PROFILES}/batch`, () => docspace(response)));

    await expect(new HttpProfilesStorage().createMany([input])).resolves.toEqual([]);
  });

  it("marks the profiles of a primed round stale", async () => {
    upstream.use(http.post(`${PROFILES}/batch`, () => docspace([])));
    const snapshot = snapshotWith([]);

    await inRequest(() => new HttpProfilesStorage().createMany([]), { snapshot });

    expect(snapshot.stale.has("profiles")).toBe(true);
    expect(upstream.seen[0]?.body).toEqual({ profiles: [] });
  });
});

describe("HttpProfilesStorage.readById", () => {
  it("reads the profile with its secrets and logs what it resolved without them", async () => {
    upstream.use(http.get(`${PROFILES}/:id`, () => docspace(profileDto())));

    await expect(new HttpProfilesStorage().readById(ID)).resolves.toStrictEqual(profileOf());
    expect(upstream.seen[0]?.path).toBe(`/internal/ai/profiles/${ID}`);
    expect(logger.info).toHaveBeenCalledWith(
      `HttpProfilesStorage.readById(${ID}) -> providerType=openai model=gpt-4o ` +
        "baseUrl=https://api.openai.com/v1 hasKey=true capabilities=385 canUseTool=true " +
        "reasoning=thinks/off/[low,high] useProxy=- isCloud=- headers=[]",
    );
    expect(JSON.stringify(vi.mocked(logger.info).mock.calls)).not.toContain("sk-secret");
  });

  it.each([
    [
      { reasoning: undefined, capabilities: undefined, canUseTool: undefined },
      "capabilities=- canUseTool=- reasoning=- ",
    ],
    [{ reasoning: false }, "reasoning=no/always/[] "],
  ])("logs a profile with %j", async (overrides, part) => {
    upstream.use(http.get(`${PROFILES}/:id`, () => docspace(profileDto(overrides))));

    await new HttpProfilesStorage().readById(ID);

    expect(vi.mocked(logger.info).mock.calls[0]?.[0]).toContain(part);
  });

  it("logs a profile whose reasoning is only the legacy flag", async () => {
    const snapshot = snapshotWith([
      profileOf({ reasoning: true, reasoningSupport: undefined, headers: { b: "1", a: "2" } }),
    ]);

    await inRequest(() => new HttpProfilesStorage().readById(ID), { snapshot });

    expect(logger.info).toHaveBeenCalledWith(
      `HttpProfilesStorage.readById(${ID}) via chat-context -> providerType=openai model=gpt-4o ` +
        "baseUrl=https://api.openai.com/v1 hasKey=true capabilities=385 canUseTool=true " +
        "reasoning=true useProxy=- isCloud=- headers=[a,b]",
    );
  });

  it("encodes the id into the path", async () => {
    upstream.use(http.get(`${PROFILES}/*`, () => docspace(profileDto())));

    await new HttpProfilesStorage().readById("a/b");

    expect(upstream.seen[0]?.path).toBe("/internal/ai/profiles/a%2Fb");
  });

  it("points an onlyoffice profile at the gateway of the AI service", async () => {
    upstream.use(
      http.get(`${PROFILES}/:id`, () =>
        docspace(profileDto({ providerType: "onlyoffice", baseUrl: "https://public/ai" })),
      ),
    );

    await expect(new HttpProfilesStorage().readById(ID)).resolves.toMatchObject({
      providerType: "onlyoffice",
      baseUrl: `${aiServiceBaseUrl}/api/2.0/ai/gateway`,
    });
  });

  it("answers undefined and warns for a 404", async () => {
    upstream.use(http.get(`${PROFILES}/:id`, () => HttpResponse.json({}, { status: 404 })));

    await expect(new HttpProfilesStorage().readById(ID)).resolves.toBeUndefined();
    expect(logger.warn).toHaveBeenCalledWith(
      `HttpProfilesStorage.readById(${ID}) -> 404 NOT FOUND`,
    );
  });

  it.each([400, 403, 500])("rejects a %i", async (status) => {
    upstream.use(http.get(`${PROFILES}/:id`, () => HttpResponse.json({}, { status })));

    expect(await failure(new HttpProfilesStorage().readById(ID))).toMatchObject({ status });
  });

  describe("on fake timers", () => {
    beforeEach(() => {
      vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    });
    afterEach(() => {
      vi.useRealTimers();
    });

    it("rejects once the AI service has not answered for 300000 ms", async () => {
      upstream.use(
        http.get(`${PROFILES}/:id`, async () => {
          await delay("infinite");
          return docspace(null);
        }),
      );
      const pending = failure(new HttpProfilesStorage().readById(ID));

      await vi.advanceTimersByTimeAsync(300_000);
      vi.useRealTimers();

      expect(String(await pending)).toContain("upstream timeout after 300000ms");
    });
  });

  // DEFECT stor1-12: a 200 whose body is not a ProfileDto is answered undefined, the value of a 404, which the routes turn into "the AI profile does not exist" — expected the unknown shape to be rethrown (ts-ai-failure-paths.md §1)
  it.each([
    [
      "a field is missing",
      profileDto({ modelId: undefined }),
      "fields=[baseUrl,byokProvider,canUseTool,capabilities,createdAt,id,key,name,providerType,reasoning,useResponsesApi]",
    ],
    ["the answer is null", null, "fields=[object]"],
    ["the answer is a string", "x", "fields=[string]"],
  ])("answers undefined and logs the field names when %s", async (_case, response, fields) => {
    upstream.use(http.get(`${PROFILES}/:id`, () => docspace(response)));

    await expect(new HttpProfilesStorage().readById(ID)).resolves.toBeUndefined();
    expect(logger.info).toHaveBeenCalledWith(
      `HttpProfilesStorage.readById(${ID}) -> UNUSABLE payload (a required field is missing): ${fields}`,
    );
    expect(JSON.stringify(vi.mocked(logger.info).mock.calls)).not.toContain("sk-secret");
  });

  it("serves a primed round from the snapshot, with the gateway override, without a read", async () => {
    const stored = profileOf({ providerType: "onlyoffice" });
    const snapshot = snapshotWith([profileOf({ id: "other" }), stored]);

    const resolved = await inRequest(() => new HttpProfilesStorage().readById(ID), { snapshot });

    expect(resolved).toStrictEqual({
      ...stored,
      baseUrl: `${aiServiceBaseUrl}/api/2.0/ai/gateway`,
    });
    expect(upstream.seen).toHaveLength(0);
  });

  it("answers undefined for an id the snapshot does not hold, without a read", async () => {
    const snapshot = snapshotWith([profileOf({ id: "other" })]);

    await expect(
      inRequest(() => new HttpProfilesStorage().readById(ID), { snapshot }),
    ).resolves.toBeUndefined();
    expect(upstream.seen).toHaveLength(0);
    expect(logger.info).toHaveBeenCalledWith(
      `HttpProfilesStorage.readById(${ID}) via chat-context -> NOT FOUND`,
    );
  });

  it("reads the AI service and records the miss once the profiles of the round are stale", async () => {
    upstream.use(http.get(`${PROFILES}/:id`, () => docspace(profileDto())));
    const snapshot = snapshotWith([], ["profiles"]);

    const result = await inRequest(
      async () => ({
        profile: await new HttpProfilesStorage().readById(ID),
        misses: getChatContextMisses(),
        reads: getUpstreamReadCount(),
      }),
      { snapshot },
    );

    expect(result).toStrictEqual({
      profile: profileOf(),
      misses: [`profiles.readById(${ID})`],
      reads: 1,
    });
  });
});

describe("HttpProfilesStorage.readByIdRaw", () => {
  it("reads the profile for a client, with forClient, without the gateway override", async () => {
    upstream.use(
      http.get(`${PROFILES}/:id`, () =>
        docspace(
          profileDto({ providerType: "onlyoffice", baseUrl: "https://public/ai", key: null }),
        ),
      ),
    );

    const { key: _key, ...expected } = profileOf({
      providerType: "onlyoffice",
      baseUrl: "https://public/ai",
    });
    await expect(new HttpProfilesStorage().readByIdRaw(ID)).resolves.toStrictEqual(expected);
    expect(upstream.seen[0]?.path).toBe(`/internal/ai/profiles/${ID}?forClient=true`);
  });

  it("answers undefined and warns for a 404", async () => {
    upstream.use(http.get(`${PROFILES}/:id`, () => HttpResponse.json({}, { status: 404 })));

    await expect(new HttpProfilesStorage().readByIdRaw(ID)).resolves.toBeUndefined();
    expect(logger.warn).toHaveBeenCalledWith(
      `HttpProfilesStorage.readByIdRaw(${ID}) -> 404 NOT FOUND`,
    );
  });

  it.each([403, 500])("rejects a %i", async (status) => {
    upstream.use(http.get(`${PROFILES}/:id`, () => HttpResponse.json({}, { status })));

    expect(await failure(new HttpProfilesStorage().readByIdRaw(ID))).toMatchObject({ status });
  });

  // DEFECT stor1-13: a 200 whose body is not a ProfileDto is answered undefined, the value of a 404 — expected the unknown shape to be rethrown (ts-ai-failure-paths.md §1)
  it("answers undefined when the answer is not a profile", async () => {
    upstream.use(http.get(`${PROFILES}/:id`, () => docspace({ id: ID })));

    await expect(new HttpProfilesStorage().readByIdRaw(ID)).resolves.toBeUndefined();
  });

  it("serves a primed round from the snapshot as it is stored, without a read and without a miss", async () => {
    const stored = profileOf({ providerType: "onlyoffice" });
    const snapshot = snapshotWith([stored]);

    const result = await inRequest(
      async () => ({
        hit: await new HttpProfilesStorage().readByIdRaw(ID),
        miss: await new HttpProfilesStorage().readByIdRaw("nope"),
      }),
      { snapshot },
    );

    expect(result.hit).toBe(stored);
    expect(result.miss).toBeUndefined();
    expect(upstream.seen).toHaveLength(0);
  });

  it("reads the AI service without recording a miss once the profiles are stale", async () => {
    upstream.use(http.get(`${PROFILES}/:id`, () => docspace(profileDto())));
    const snapshot = snapshotWith([], ["profiles"]);

    const misses = await inRequest(
      async () => {
        await new HttpProfilesStorage().readByIdRaw(ID);
        return getChatContextMisses();
      },
      { snapshot },
    );

    expect(misses).toEqual([]);
    expect(upstream.seen).toHaveLength(1);
  });
});

describe("HttpProfilesStorage.readAllForClient and readAll", () => {
  it("reads the list for a client with forClient and keeps the profiles it can read", async () => {
    upstream.use(
      http.get(PROFILES, () => docspace([profileDto({ key: null }), { id: "broken" }, null])),
    );

    const { key: _key, ...expected } = profileOf();
    await expect(new HttpProfilesStorage().readAllForClient()).resolves.toStrictEqual([expected]);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/profiles?forClient=true");
  });

  it("reads the client list from the AI service even in a primed round", async () => {
    upstream.use(http.get(PROFILES, () => docspace([])));

    await inRequest(() => new HttpProfilesStorage().readAllForClient(), {
      snapshot: snapshotWith([profileOf()]),
    });

    expect(upstream.seen).toHaveLength(1);
  });

  it("reads the full list without forClient", async () => {
    upstream.use(http.get(PROFILES, () => docspace([profileDto()])));

    await expect(new HttpProfilesStorage().readAll()).resolves.toStrictEqual([profileOf()]);
    expect(upstream.seen[0]?.path).toBe("/internal/ai/profiles");
  });

  // DEFECT stor1-14: a list answered in a shape that is not a list reads as "no profiles" — expected the unknown shape to be rethrown (ts-ai-failure-paths.md §1)
  it.each([
    ["an object", { id: ID }],
    ["null", null],
  ])("answers an empty list when the answer is %s", async (_case, response) => {
    upstream.use(http.get(PROFILES, () => docspace(response)));

    await expect(new HttpProfilesStorage().readAll()).resolves.toEqual([]);
    await expect(new HttpProfilesStorage().readAllForClient()).resolves.toEqual([]);
  });

  it.each([500, 403])("rejects a %i", async (status) => {
    upstream.use(http.get(PROFILES, () => HttpResponse.json({}, { status })));

    expect(await failure(new HttpProfilesStorage().readAll())).toMatchObject({ status });
    expect(await failure(new HttpProfilesStorage().readAllForClient())).toMatchObject({ status });
  });

  it("serves the full list of a primed round from the snapshot", async () => {
    const profiles = [profileOf()];

    await expect(
      inRequest(() => new HttpProfilesStorage().readAll(), { snapshot: snapshotWith(profiles) }),
    ).resolves.toBe(profiles);
    expect(upstream.seen).toHaveLength(0);
  });

  it("reads the full list and records the miss once the profiles are stale", async () => {
    upstream.use(http.get(PROFILES, () => docspace([])));

    const misses = await inRequest(
      async () => {
        await new HttpProfilesStorage().readAll();
        return getChatContextMisses();
      },
      { snapshot: snapshotWith([], ["profiles"]) },
    );

    expect(misses).toEqual(["profiles.readAll"]);
  });
});

describe("HttpProfilesStorage.update and delete", () => {
  it("puts the profile under its encoded id and marks the round's profiles stale", async () => {
    upstream.use(http.put(`${PROFILES}/*`, () => docspace(profileDto())));
    const snapshot = snapshotWith([]);

    await inRequest(
      () => new HttpProfilesStorage().update(profileOf({ id: "a/b", key: undefined })),
      { snapshot },
    );

    expect(upstream.seen[0]).toMatchObject({
      method: "PUT",
      path: "/internal/ai/profiles/a%2Fb",
      body: {
        name: "GPT",
        providerType: "openai",
        baseUrl: "https://api.openai.com/v1",
        key: null,
        modelId: "gpt-4o",
        reasoning: { thinks: true, canDisable: true, depths: ["low", "high"], defaultDepth: "low" },
        capabilities: 385,
        canUseTool: true,
        useResponsesApi: false,
        useProxy: null,
        isCloudProvider: null,
      },
    });
    expect(snapshot.stale.has("profiles")).toBe(true);
  });

  it("does not mark the profiles stale when the update is refused", async () => {
    upstream.use(http.put(`${PROFILES}/:id`, () => HttpResponse.json({}, { status: 500 })));
    const snapshot = snapshotWith([]);

    const error = await inRequest(() => failure(new HttpProfilesStorage().update(profileOf())), {
      snapshot,
    });

    expect(error).toMatchObject({ status: 500 });
    expect(snapshot.stale.size).toBe(0);
  });

  it("deletes the profile under its encoded id", async () => {
    upstream.use(http.delete(`${PROFILES}/*`, () => new HttpResponse(null, { status: 204 })));

    await expect(new HttpProfilesStorage().delete("a/b")).resolves.toBeUndefined();
    expect(upstream.seen[0]).toMatchObject({
      method: "DELETE",
      path: "/internal/ai/profiles/a%2Fb",
    });
  });

  it("takes a 404 as deleted", async () => {
    upstream.use(http.delete(`${PROFILES}/:id`, () => HttpResponse.json({}, { status: 404 })));

    await expect(new HttpProfilesStorage().delete(ID)).resolves.toBeUndefined();
  });

  it("marks the profiles stale even when the delete is refused", async () => {
    upstream.use(http.delete(`${PROFILES}/:id`, () => HttpResponse.json({}, { status: 403 })));
    const snapshot = snapshotWith([]);

    const error = await inRequest(() => failure(new HttpProfilesStorage().delete(ID)), {
      snapshot,
    });

    expect(error).toMatchObject({ status: 403 });
    expect(snapshot.stale.has("profiles")).toBe(true);
  });
});

describe("listOnlyofficeGatewayModels", () => {
  const GATEWAY = `${aiServiceBaseUrl}/api/2.0/ai/gateway/models`;

  /** One entry of the gateway catalogue (OpenRouter's `/models` shape, proxied by the AI service). */
  const entry = (overrides: Record<string, unknown> = {}) => ({
    id: "anthropic/claude-sonnet-4.5",
    alias: "Claude Sonnet 4.5",
    type: "chat",
    capabilities: ["tools", "reasoning"],
    input_modalities: ["text", "image"],
    output_modalities: ["text"],
    context_length: 200000,
    reasoning: {
      mandatory: false,
      default_enabled: true,
      supported_efforts: ["low", "medium", "high", "minimal"],
      default_effort: "medium",
    },
    ...overrides,
  });

  const list = async (data: unknown[]): Promise<unknown> => {
    upstream.use(http.get(GATEWAY, () => HttpResponse.json({ object: "list", data })));
    return listOnlyofficeGatewayModels();
  };

  it("reads the catalogue with the caller's headers and counts the read", async () => {
    upstream.use(http.get(GATEWAY, () => HttpResponse.json({ data: [entry()] })));

    const result = await inRequest(
      async () => ({
        models: await listOnlyofficeGatewayModels(),
        reads: getUpstreamReadCount(),
        calls: getUpstreamCalls(),
      }),
      { headers: { cookie: "c=1" } },
    );

    expect(result).toStrictEqual({
      models: [
        {
          id: "anthropic/claude-sonnet-4.5",
          name: "Claude Sonnet 4.5",
          provider: "onlyoffice",
          reasoning: true,
          capabilities: 1 | 128 | 256,
          reasoningSupport: {
            thinks: true,
            canDisable: true,
            depths: ["low", "medium", "high"],
            defaultDepth: "medium",
          },
          contextWindow: 200000,
        },
      ],
      reads: 1,
      calls: { GET: 1 },
    });
    expect(upstream.seen[0]?.path).toBe("/api/2.0/ai/gateway/models");
    expect(upstream.seen[0]?.headers["cookie"]).toBe("c=1");
  });

  it.each([
    [
      "an image model",
      { type: "IMAGE", capabilities: [], input_modalities: [], reasoning: undefined },
      { capabilities: 2 },
    ],
    [
      "an image output",
      { type: "chat", output_modalities: ["IMAGE"], capabilities: [] },
      { capabilities: 1 | 128 | 2 },
    ],
    [
      "an unknown type",
      { type: "audio", capabilities: [], input_modalities: [] },
      { capabilities: 0 },
    ],
    [
      "no type",
      { type: undefined, capabilities: ["TOOLS"], input_modalities: [] },
      { capabilities: 256 },
    ],
    [
      "capabilities that are not a list",
      { capabilities: "tools", input_modalities: null, output_modalities: 5 },
      { capabilities: 1 },
    ],
    [
      "capabilities that are not strings",
      { capabilities: [1, null, "tools"] },
      { capabilities: 1 | 128 | 256 },
    ],
    ["no alias", { alias: undefined }, { name: "anthropic/claude-sonnet-4.5" }],
    ["an alias that is not a string", { alias: 5 }, { name: "anthropic/claude-sonnet-4.5" }],
  ])("maps %s", async (_case, overrides, expected) => {
    const [model] = (await list([entry(overrides)])) as Record<string, unknown>[];

    expect(model).toMatchObject(expected);
  });

  it.each([
    [
      "no reasoning object and no reasoning capability",
      { reasoning: undefined, capabilities: [] },
      { reasoning: false, reasoningSupport: { thinks: false, canDisable: true, depths: [] } },
    ],
    [
      "no reasoning object but the reasoning capability",
      { reasoning: null, capabilities: ["Reasoning"] },
      { reasoning: true, reasoningSupport: { thinks: true, canDisable: true, depths: [] } },
    ],
    [
      "a mandatory reasoning that is off by default",
      {
        reasoning: {
          mandatory: true,
          default_enabled: false,
          supported_efforts: ["XHIGH", "none"],
        },
        capabilities: [],
      },
      { reasoning: true, reasoningSupport: { thinks: true, canDisable: false, depths: ["max"] } },
    ],
    [
      "a reasoning object whose fields have the wrong types",
      {
        reasoning: {
          mandatory: "yes",
          default_enabled: "no",
          supported_efforts: "low",
          default_effort: 3,
        },
        capabilities: [],
      },
      { reasoning: true, reasoningSupport: { thinks: true, canDisable: true, depths: [] } },
    ],
    [
      "a reasoning object with an unknown default effort",
      { reasoning: { default_effort: "minimal" }, capabilities: [] },
      { reasoning: true, reasoningSupport: { thinks: true, canDisable: true, depths: [] } },
    ],
  ])("maps the reasoning of %s", async (_case, overrides, expected) => {
    const [model] = (await list([entry(overrides)])) as Record<string, unknown>[];

    expect({
      reasoning: model?.["reasoning"],
      reasoningSupport: model?.["reasoningSupport"],
    }).toStrictEqual(expected);
  });

  it.each([
    ["zero", 0],
    ["negative", -5],
    ["a string", "200000"],
    ["null", null],
  ])("leaves the context window out when context_length is %s", async (_case, value) => {
    const [model] = (await list([entry({ context_length: value })])) as Record<string, unknown>[];

    expect(model).not.toHaveProperty("contextWindow");
  });

  it("skips embedding models, entries without a string id and entries that are not objects", async () => {
    const models = (await list([
      entry({ type: "Embedding" }),
      entry({ id: undefined }),
      entry({ id: 5 }),
      "x",
      null,
      entry({ id: "kept" }),
    ])) as Record<string, unknown>[];

    expect(models.map((m) => m["id"])).toEqual(["kept"]);
  });

  it.each([
    ["no data", { object: "list" }],
    ["data that is not a list", { data: {} }],
    ["a list", [entry()]],
    ["the DocSpace envelope", { response: { data: [] }, status: 0 }],
  ])("refuses an answer with %s", async (_case, body) => {
    upstream.use(http.get(GATEWAY, () => HttpResponse.json(body)));

    expect(await failure(listOnlyofficeGatewayModels())).toMatchObject({
      message: `gateway models listing returned no data array (${GATEWAY})`,
    });
  });

  it.each([401, 403, 502])(
    "rejects a %i with an AiServiceHttpError carrying the body",
    async (status) => {
      upstream.use(
        http.get(GATEWAY, () => HttpResponse.text("detail", { status, statusText: "R" })),
      );

      const error = await failure(listOnlyofficeGatewayModels());

      expect(error).toBeInstanceOf(AiServiceHttpError);
      expect(error).toMatchObject({ status, statusText: "R", body: "detail", url: GATEWAY });
    },
  );

  it("aborts when the caller's signal fires", async () => {
    upstream.use(
      http.get(GATEWAY, async () => {
        await delay("infinite");
        return HttpResponse.json({ data: [] });
      }),
    );
    const caller = new AbortController();
    const pending = failure(listOnlyofficeGatewayModels(caller.signal));

    caller.abort(new Error("client went away"));

    expect(await pending).toEqual(new Error("client went away"));
  });
});
