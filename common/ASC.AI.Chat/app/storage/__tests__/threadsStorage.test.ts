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

vi.mock("../httpClient.js", () => {
  class AiServiceHttpError extends Error {
    constructor(public readonly status: number) {
      super(`AI service ${status}`);
    }
  }
  return {
    AiServiceHttpError,
    aiService: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn() },
  };
});
vi.mock("../chatContextSnapshot.js", () => ({
  invalidateChatContext: vi.fn(),
  readChatContext: () => undefined,
  reportChatContextMiss: vi.fn(),
}));

// Folder 12 is an AI project, any other folder is not.
const resolveAiProjectEntityId = vi.fn(async (id: string | undefined) =>
  id === "12" ? id : undefined,
);
vi.mock("../docspaceFilesApi.js", () => ({ resolveAiProjectEntityId }));

const setRoundThreadId = vi.fn();
vi.mock("../../requestContext.js", () => ({ setRoundThreadId }));

const { aiService } = await import("../httpClient.js");
const { HttpThreadsStorage } = await import("../threadsStorage.js");

const thread = { id: "t-1", title: "Chat", lastEditDate: 1 };
const storage = new HttpThreadsStorage();

beforeEach(() => {
  setRoundThreadId.mockReset();
  vi.mocked(aiService.post).mockReset().mockResolvedValue(thread);
  vi.mocked(aiService.get)
    .mockReset()
    .mockResolvedValue({ items: [thread], cursor: null });
});

describe("the project of a thread", () => {
  it("is stored with a thread started in an AI project", async () => {
    await storage.create("Chat", undefined, "12");
    expect(aiService.post).toHaveBeenCalledWith("/threads", {
      title: "Chat",
      profileId: null,
      entityId: "12",
    });
  });

  it.each([undefined, "", "abc", "0", "12/3"])("is none for the scope %j", async (entityId) => {
    await storage.create("Chat", undefined, entityId);
    expect(aiService.post).toHaveBeenCalledWith("/threads", {
      title: "Chat",
      profileId: null,
      entityId: null,
    });
  });

  // The document agent keeps its results with the thread of the round, and a
  // round that starts without one learns it here.
  it("names the thread it created as the thread of the round", async () => {
    await storage.create("Chat");
    expect(setRoundThreadId).toHaveBeenCalledExactlyOnceWith("t-1");
  });

  it("is none for a folder that is not an AI project", async () => {
    await storage.create("Chat", undefined, "30");
    expect(aiService.post).toHaveBeenCalledWith("/threads", {
      title: "Chat",
      profileId: null,
      entityId: null,
    });

    await storage.readAll("30", 20);
    expect(aiService.get).toHaveBeenCalledWith("/threads", { query: { count: 20 } });
  });

  it("narrows the list to the threads of that project", async () => {
    await storage.readAll("12", 20);
    expect(aiService.get).toHaveBeenCalledWith("/threads", {
      query: { count: 20, entityId: "12" },
    });
  });

  it("lists the threads of no project without a scope", async () => {
    await storage.readAll(undefined, 20);
    expect(aiService.get).toHaveBeenCalledWith("/threads", { query: { count: 20 } });
  });

  it("keeps the scope on every page of a whole-list read", async () => {
    vi.mocked(aiService.get)
      .mockResolvedValueOnce({ items: [thread], cursor: { lastEditDate: 1, id: "t-1" } })
      .mockResolvedValueOnce({ items: [], cursor: null });
    await storage.readAll("12");
    for (const [, options] of vi.mocked(aiService.get).mock.calls) {
      expect((options as { query: Record<string, unknown> }).query["entityId"]).toBe("12");
    }
    expect(aiService.get).toHaveBeenCalledTimes(2);
  });
});
