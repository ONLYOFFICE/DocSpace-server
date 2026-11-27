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

import { mkdtemp, mkdir, writeFile } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../storage/docspaceFileTransfer.js", () => ({
  uploadFile: vi.fn(),
  mediaTypeOf: (title: string) => (title.endsWith(".md") ? "text/markdown" : "application/docx"),
}));
vi.mock("../../storage/docspaceFilesApi.js", () => {
  class DocspaceApiHttpError extends Error {
    constructor(public readonly status: number) {
      super(`DocSpace API ${status}`);
    }
  }
  return { DocspaceApiHttpError, filesApiSend: vi.fn() };
});

const aiConfig = vi.fn();
vi.mock("../../storage/httpClient.js", () => ({
  aiService: { get: aiConfig },
  AiServiceHttpError: class AiServiceHttpError extends Error {},
}));

const portalChatProfileId = vi.fn();
vi.mock("../../storage/aiProjectChatContext.js", () => ({ portalChatProfileId }));

const { uploadFile } = await import("../../storage/docspaceFileTransfer.js");
const { DocspaceApiHttpError, filesApiSend } = await import("../../storage/docspaceFilesApi.js");
const { findTemplate, readTemplateCatalog, readTemplateFile } = await import("../catalog.js");
const { installTemplate } = await import("../installTemplate.js");

const catalog = await readTemplateCatalog();

/** Rooms get the ids 101, 102, ... in the order they are created. */
function answerRooms(): void {
  let nextId = 100;
  vi.mocked(filesApiSend).mockImplementation(async (_method, apiPath, body) =>
    apiPath === "rooms" ? { id: ++nextId, title: (body as { title: string }).title } : undefined,
  );
}

beforeEach(() => {
  vi.mocked(uploadFile).mockReset().mockResolvedValue({ id: "1", title: "file" });
  vi.mocked(filesApiSend).mockReset();
  portalChatProfileId.mockReset().mockResolvedValue("p1");
  aiConfig.mockReset().mockResolvedValue({ vectorizationEnabled: true });
  answerRooms();
});

describe("the shipped catalog", () => {
  it("holds three agents in each category, each with skills and knowledge", () => {
    const byCategory = new Map<string, number>();
    for (const agent of catalog.agents) {
      byCategory.set(agent.category, (byCategory.get(agent.category) ?? 0) + 1);
      expect(agent.skills.length).toBeGreaterThan(0);
      expect(agent.knowledges.length).toBeGreaterThan(0);
      for (const id of [...agent.skills, ...agent.knowledges]) {
        expect(findTemplate(catalog, id)?.kind).not.toBe("agent");
      }
    }
    expect(Object.fromEntries(byCategory)).toEqual({ Document: 3, Presentation: 3, Tables: 3 });
  });

  it("holds skills the Files service accepts: Markdown with name and description", async () => {
    for (const set of catalog.skillSets) {
      for (const file of set.files) {
        const text = new TextDecoder().decode(await readTemplateFile(set, file));
        expect(text, `${set.id}/${file}`).toMatch(/^---\nname: ".+"\ndescription: ".+"\n---\n\n/);
      }
    }
  });

  it("gives every template a colour and a cover, no two of them the same cover", () => {
    const all = [...catalog.agents, ...catalog.skillSets, ...catalog.knowledgeBases];
    for (const template of all) {
      expect(template.color, template.id).toMatch(/^[0-9A-F]{6}$/);
      expect(template.cover, template.id).toMatch(/^[a-z0-9_]+$/);
    }
    expect(new Set(all.map((template) => template.cover)).size).toBe(all.length);
  });

  it("reads only the files a template lists", () => {
    expect(() => readTemplateFile(catalog.skillSets[0]!, "../template.json")).toThrow();
  });
});

describe("readTemplateCatalog", () => {
  async function folder(files: Record<string, string>): Promise<string> {
    const root = await mkdtemp(path.join(os.tmpdir(), "templates-"));
    for (const [name, content] of Object.entries(files)) {
      await mkdir(path.dirname(path.join(root, name)), { recursive: true });
      await writeFile(path.join(root, name), content);
    }
    return root;
  }
  const set = (files: string[]) =>
    JSON.stringify({ name: "Set", description: "A set", category: "Document", files });
  const agent = (skills: string[]) =>
    JSON.stringify({
      name: "Agent",
      description: "An agent",
      category: "Document",
      instructions: "Do it",
      skills,
      knowledges: [],
    });

  it("reads an empty folder as an empty catalog", async () => {
    expect(await readTemplateCatalog(await folder({}))).toEqual({
      agents: [],
      skillSets: [],
      knowledgeBases: [],
    });
  });

  it.each([
    ["a file outside the folder", { "skill-sets/a/template.json": set(["../b.md"]) }],
    ["a missing file", { "skill-sets/a/template.json": set(["b.md"]) }],
    [
      "a skill that is not Markdown",
      { "skill-sets/a/template.json": set(["b.txt"]), "skill-sets/a/b.txt": "x" },
    ],
    ["an agent linked to an unknown set", { "agents/document/a.json": agent(["skill-sets/none"]) }],
    [
      "a colour that is not hexadecimal",
      { "agents/document/a.json": JSON.stringify({ ...JSON.parse(agent([])), color: "#FF0000" }) },
    ],
    [
      "a cover that is not a cover id",
      { "agents/document/a.json": JSON.stringify({ ...JSON.parse(agent([])), cover: "../x" }) },
    ],
  ])("refuses %s", async (_case, files) => {
    await expect(readTemplateCatalog(await folder(files))).rejects.toThrow();
  });
});

describe("installTemplate", () => {
  it("answers 404 for an id the catalog does not hold", async () => {
    await expect(installTemplate(catalog, "agents/none")).rejects.toMatchObject({ status: 404 });
    expect(filesApiSend).not.toHaveBeenCalled();
  });

  it("installs a skill set as an Instructions room with its skills", async () => {
    const template = catalog.skillSets[0]!;
    const result = await installTemplate(catalog, template.id);
    expect(filesApiSend).toHaveBeenCalledExactlyOnceWith("POST", "rooms", {
      title: template.name,
      roomType: 10,
      color: template.color,
      cover: template.cover,
    });
    expect(vi.mocked(uploadFile).mock.calls.map(([room, title]) => [room, title])).toEqual(
      template.files.map((file) => ["101", file]),
    );
    expect(result).toEqual({
      templateId: template.id,
      id: "101",
      title: template.name,
      kind: "skillSet",
      skillSets: [],
      knowledgeBases: [],
      warnings: [],
    });
  });

  it("installs a knowledge base as a Knowledge room with its files", async () => {
    const template = catalog.knowledgeBases[0]!;
    const result = await installTemplate(catalog, template.id);
    expect(filesApiSend).toHaveBeenCalledExactlyOnceWith("POST", "rooms", {
      title: template.name,
      roomType: 11,
      color: template.color,
      cover: template.cover,
    });
    expect(uploadFile).toHaveBeenCalledTimes(template.files.length);
    expect(result.kind).toBe("knowledgeBase");
  });

  it("installs an agent in its colour and cover, tagged with its category, links its sources and writes its model, and says its instructions were skipped", async () => {
    const template = catalog.agents[0]!;
    const result = await installTemplate(catalog, template.id);
    const room = (id: string, roomType: number) => {
      const { name: title, color, cover } = findTemplate(catalog, id)!;
      return { title, roomType, color, cover };
    };
    expect(vi.mocked(filesApiSend).mock.calls).toEqual([
      ["POST", "rooms", room(template.skills[0]!, 10)],
      ["POST", "rooms", room(template.knowledges[0]!, 11)],
      ["POST", "rooms", { ...room(template.id, 12), tags: [template.category] }],
      ["POST", "rooms/103/sources", { roomIds: [101, 102] }],
      ["PUT", "rooms/103/model", { profileId: "p1" }],
    ]);
    expect(result).toMatchObject({
      templateId: template.id,
      id: "103",
      kind: "agent",
      skillSets: [{ templateId: template.skills[0], id: "101" }],
      knowledgeBases: [{ templateId: template.knowledges[0], id: "102" }],
      warnings: ["instructionsNotSaved"],
    });
  });

  it("keeps the agent when the portal has no chat model, and says so", async () => {
    portalChatProfileId.mockResolvedValue(null);
    const result = await installTemplate(catalog, catalog.agents[0]!.id);
    expect(result.warnings).toEqual(["modelNotSaved", "instructionsNotSaved"]);
    expect(
      vi.mocked(filesApiSend).mock.calls.some(([, apiPath]) => apiPath.endsWith("/model")),
    ).toBe(false);
  });

  it("keeps the agent when the portal's chat model cannot be read, and says so", async () => {
    portalChatProfileId.mockRejectedValue(new Error("AI service down"));
    const result = await installTemplate(catalog, catalog.agents[0]!.id);
    expect(result.warnings).toEqual(["modelNotSaved", "instructionsNotSaved"]);
  });

  it("keeps the agent when its model is refused, and says so", async () => {
    vi.mocked(filesApiSend).mockImplementation(async (_method, apiPath, body) => {
      if (apiPath.endsWith("/model")) {
        throw new DocspaceApiHttpError(400, "Bad Request", "url");
      }
      return apiPath === "rooms" ? { id: 7, title: (body as { title: string }).title } : undefined;
    });
    const result = await installTemplate(catalog, catalog.agents[0]!.id);
    expect(result.warnings).toEqual(["modelNotSaved", "instructionsNotSaved"]);
  });

  it("keeps the agent when the write of its model times out or never connects, and says so", async () => {
    for (const failure of [new DOMException("timed out", "TimeoutError"), new TypeError("fetch failed")]) {
      vi.mocked(filesApiSend).mockImplementation(async (_method, apiPath, body) => {
        if (apiPath.endsWith("/model")) {
          throw failure;
        }
        return apiPath === "rooms" ? { id: 7, title: (body as { title: string }).title } : undefined;
      });
      const result = await installTemplate(catalog, catalog.agents[0]!.id);
      expect(result.id).toBe("7");
      // The write got no answer: the model may be stored.
      expect(result.warnings).toEqual(["modelUnconfirmed", "instructionsNotSaved"]);
    }
  });

  // Regression: the Files service refuses a Knowledge room on a portal
  // without vectorization, and an agent's Instructions rooms were made
  // before it, so every try left one more room behind.
  it("refuses a template with a knowledge base before any room where vectorization is off", async () => {
    aiConfig.mockResolvedValue({ vectorizationEnabled: false, vectorizationNeedReset: false });
    await expect(installTemplate(catalog, catalog.agents[0]!.id)).rejects.toMatchObject({
      status: 409,
    });
    await expect(installTemplate(catalog, catalog.knowledgeBases[0]!.id)).rejects.toMatchObject({
      status: 409,
    });
    expect(filesApiSend).not.toHaveBeenCalled();
    expect(uploadFile).not.toHaveBeenCalled();
  });

  it("installs a skill set where vectorization is off, without asking for it", async () => {
    aiConfig.mockResolvedValue({ vectorizationEnabled: false });
    const result = await installTemplate(catalog, catalog.skillSets[0]!.id);
    expect(result.kind).toBe("skillSet");
    expect(aiConfig).not.toHaveBeenCalled();
  });

  it("lets a knowledge base through while the vectorization settings wait for a reset", async () => {
    aiConfig.mockResolvedValue({ vectorizationEnabled: false, vectorizationNeedReset: true });
    const result = await installTemplate(catalog, catalog.knowledgeBases[0]!.id);
    expect(result.kind).toBe("knowledgeBase");
  });

  it("makes nothing when the vectorization settings cannot be read", async () => {
    aiConfig.mockRejectedValue(new Error("AI service down"));
    await expect(installTemplate(catalog, catalog.agents[0]!.id)).rejects.toThrow(
      "AI service down",
    );
    expect(filesApiSend).not.toHaveBeenCalled();
  });

  it("fails with the Files status when a room is refused, before anything is uploaded", async () => {
    vi.mocked(filesApiSend).mockRejectedValue(new DocspaceApiHttpError(403, "Forbidden", "url"));
    await expect(installTemplate(catalog, catalog.agents[0]!.id)).rejects.toMatchObject({
      status: 403,
    });
    expect(uploadFile).not.toHaveBeenCalled();
  });
});
