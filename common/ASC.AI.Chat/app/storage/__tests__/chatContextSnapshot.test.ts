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
import logger from "../../log.js";
import { getChatContextMisses, getChatContextSnapshot } from "../../requestContext.js";
import {
  hasChatContext,
  invalidateChatContext,
  lookupChatContextFolder,
  readChatContext,
  reportChatContextMiss,
} from "../chatContextSnapshot.js";
import { inRound, scopeOf, snapshotOf } from "../../__tests__/fixtures/chatContext.js";

// The per-request snapshot of the chat-context aggregate, run in the real request context.

describe("readChatContext", () => {
  it("answers undefined outside a request context", () => {
    expect(readChatContext("profiles")).toBeUndefined();
  });

  it("answers undefined in a request that was not primed", async () => {
    await expect(inRound(undefined, async () => readChatContext("profiles"))).resolves.toBe(
      undefined,
    );
  });

  it("answers the snapshot while the slice is fresh", async () => {
    const snapshot = snapshotOf();
    await expect(inRound(snapshot, async () => readChatContext("thread"))).resolves.toBe(snapshot);
  });

  it("answers undefined for a stale slice and still serves the others", async () => {
    const snapshot = snapshotOf({ stale: new Set(["thread"]) });
    const [thread, messages] = await inRound(snapshot, async () => [
      readChatContext("thread"),
      readChatContext("messages"),
    ]);
    expect(thread).toBeUndefined();
    expect(messages).toBe(snapshot);
  });
});

describe("hasChatContext", () => {
  it("is false outside a request context and in a request that was not primed", async () => {
    expect(hasChatContext()).toBe(false);
    await expect(inRound(undefined, async () => hasChatContext())).resolves.toBe(false);
  });

  it("is true in a primed request even when every slice is stale", async () => {
    const snapshot = snapshotOf({ stale: new Set(["profiles", "thread", "messages"]) });
    await expect(inRound(snapshot, async () => hasChatContext())).resolves.toBe(true);
  });
});

describe("invalidateChatContext", () => {
  it("does nothing outside a request context", () => {
    expect(() => invalidateChatContext("profiles")).not.toThrow();
  });

  it("does nothing in a request that was not primed", async () => {
    await inRound(undefined, async () => {
      invalidateChatContext("profiles");
      expect(getChatContextSnapshot()).toBeUndefined();
    });
  });

  it("marks the slice stale so the next read of it in the request is a miss", async () => {
    const snapshot = snapshotOf();
    const after = await inRound(snapshot, async () => {
      invalidateChatContext("webSearch");
      invalidateChatContext("webSearch");
      return [readChatContext("webSearch"), readChatContext("profiles")];
    });
    expect([...snapshot.stale]).toEqual(["webSearch"]);
    expect(after).toEqual([undefined, snapshot]);
  });
});

describe("lookupChatContextFolder", () => {
  const entityFolder = { title: "Launch plan", folderType: 40, canCreate: true };
  const contextFolder = { title: "Docs", folderType: 1, canCreate: false };
  const primed = snapshotOf({
    entity: scopeOf({ entityId: "42", folder: entityFolder }),
    contextEntity: scopeOf({ entityId: "43", folder: contextFolder }),
  });

  it("misses outside a request context", () => {
    expect(lookupChatContextFolder("42")).toEqual({ hit: false });
  });

  it("misses in a request that was not primed", async () => {
    await expect(inRound(undefined, async () => lookupChatContextFolder("42"))).resolves.toEqual({
      hit: false,
    });
  });

  it.each([
    ["42", { hit: true, info: entityFolder }],
    ["43", { hit: true, info: contextFolder }],
    ["44", { hit: false }],
  ])("answers folder %s from the entity and context-entity scopes", async (id, expected) => {
    await expect(inRound(primed, async () => lookupChatContextFolder(id))).resolves.toEqual(
      expected,
    );
  });

  it("serves the folder itself, not a copy", async () => {
    const found = await inRound(primed, async () => lookupChatContextFolder("42"));
    expect(found.hit && found.info).toBe(entityFolder);
  });

  it("misses a scope that carries no folder and a snapshot without scopes", async () => {
    const noFolder = snapshotOf({ entity: scopeOf({ entityId: "42" }), contextEntity: null });
    await expect(inRound(noFolder, async () => lookupChatContextFolder("42"))).resolves.toEqual({
      hit: false,
    });
    await expect(inRound(snapshotOf(), async () => lookupChatContextFolder("42"))).resolves.toEqual(
      { hit: false },
    );
  });

  it("misses the global scope even when it names the id", async () => {
    const global = snapshotOf({ global: scopeOf({ entityId: "42", folder: entityFolder }) });
    await expect(inRound(global, async () => lookupChatContextFolder("42"))).resolves.toEqual({
      hit: false,
    });
  });

  it("serves a folder whose scope is stale: folders have no slice", async () => {
    const stale = snapshotOf({
      entity: scopeOf({ entityId: "42", folder: entityFolder }),
      stale: new Set(["profiles", "assignments", "preferences", "toolPrefs", "mcpServers"]),
    });
    await expect(inRound(stale, async () => lookupChatContextFolder("42"))).resolves.toEqual({
      hit: true,
      info: entityFolder,
    });
  });
});

describe("reportChatContextMiss", () => {
  it("records nothing and logs nothing outside a primed round", async () => {
    reportChatContextMiss("outside");
    const misses = await inRound(undefined, async () => {
      reportChatContextMiss("not primed");
      return getChatContextMisses();
    });
    expect(misses).toEqual([]);
    expect(logger.debug).not.toHaveBeenCalled();
  });

  it("records each miss of a primed round in order and logs it at debug level", async () => {
    const misses = await inRound(snapshotOf(), async () => {
      reportChatContextMiss("profiles.readAll");
      reportChatContextMiss("threads.readById(t1)");
      return getChatContextMisses();
    });
    expect(misses).toEqual(["profiles.readAll", "threads.readById(t1)"]);
    expect(vi.mocked(logger.debug).mock.calls).toEqual([
      ["chat-context: miss profiles.readAll -> HTTP"],
      ["chat-context: miss threads.readById(t1) -> HTTP"],
    ]);
    expect(logger.info).not.toHaveBeenCalled();
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("keeps the misses of one request out of another", async () => {
    await inRound(snapshotOf(), async () => reportChatContextMiss("first"));
    const second = await inRequestContext({}, async () => getChatContextMisses());
    expect(second).toEqual([]);
  });
});
