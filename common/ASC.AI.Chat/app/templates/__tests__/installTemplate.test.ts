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

// installTemplate over catalogs the shipped one cannot produce: a template without a look,
// an agent linked to a template that is missing or is an agent, failures that are not
// errors. The Files client and the AI service are the boundary and are stubbed.

import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../storage/docspaceFileTransfer.js", () => ({
  uploadFile: vi.fn(),
  mediaTypeOf: () => "text/markdown",
}));
vi.mock("../../storage/docspaceFilesApi.js", () => {
  class DocspaceApiHttpError extends Error {
    constructor(public readonly status: number) {
      super(`DocSpace API ${status}`);
    }
  }
  return { DocspaceApiHttpError, filesApiSend: vi.fn() };
});
const aiConfig = vi.hoisted(() => vi.fn());
vi.mock("../../storage/httpClient.js", () => ({ aiService: { get: aiConfig } }));
const portalChatProfileId = vi.hoisted(() => vi.fn());
vi.mock("../../storage/aiProjectChatContext.js", () => ({ portalChatProfileId }));

const logger = (await import("../../log.js")).default;
const { uploadFile } = await import("../../storage/docspaceFileTransfer.js");
const { filesApiSend } = await import("../../storage/docspaceFilesApi.js");
const { readTemplateCatalog } = await import("../catalog.js");
const { installTemplate, TemplateInstallIncompleteError } = await import("../installTemplate.js");
import type { AgentTemplate, TemplateCatalog } from "../catalog.js";

const shipped = await readTemplateCatalog();
const skillSet = shipped.skillSets.find((s) => s.id === "skill-sets/budgeting")!;

beforeEach(() => {
  let nextId = 100;
  vi.mocked(uploadFile).mockReset().mockResolvedValue({ id: "1", title: "file" });
  vi.mocked(filesApiSend)
    .mockReset()
    .mockImplementation(async (_method, apiPath, body) =>
      apiPath === "rooms" ? { id: ++nextId, title: (body as { title: string }).title } : undefined,
    );
  portalChatProfileId.mockReset().mockResolvedValue("p1");
  aiConfig.mockReset().mockResolvedValue({ vectorizationEnabled: true });
});

function catalogWith(agent: Partial<AgentTemplate>): TemplateCatalog {
  return {
    ...shipped,
    agents: [
      {
        id: "agents/test/agent",
        kind: "agent",
        name: "Agent",
        description: "d",
        category: "Test",
        instructions: "Do it",
        skills: [],
        knowledges: [],
        ...agent,
      },
    ],
  };
}

describe("installTemplate", () => {
  it("creates a room with no colour, cover or tags for a template that has none", async () => {
    const plain = { ...skillSet, color: undefined, cover: undefined };
    const catalog = { ...shipped, skillSets: [plain] };

    const result = await installTemplate(catalog, skillSet.id);

    expect(vi.mocked(filesApiSend).mock.calls[0]).toEqual([
      "POST",
      "rooms",
      { title: skillSet.name, roomType: 10 },
    ]);
    expect(result.id).toBe("101");
  });

  it("installs an agent with no skills or knowledge: no sources link, no vectorization check", async () => {
    const result = await installTemplate(catalogWith({}), "agents/test/agent");

    expect(vi.mocked(filesApiSend).mock.calls).toEqual([
      ["POST", "rooms", { title: "Agent", roomType: 12, tags: ["Test"] }],
      ["PUT", "rooms/101/model", { profileId: "p1" }],
    ]);
    expect(aiConfig).not.toHaveBeenCalled();
    expect(result).toEqual({
      templateId: "agents/test/agent",
      id: "101",
      title: "Agent",
      kind: "agent",
      skillSets: [],
      knowledgeBases: [],
      warnings: ["instructionsNotSaved"],
    });
  });

  it("puts the agent's id into its sources and model paths encoded", async () => {
    vi.mocked(filesApiSend).mockImplementation(async (_method, apiPath, body) =>
      apiPath === "rooms" ? { id: "a/b", title: (body as { title: string }).title } : undefined,
    );

    await installTemplate(catalogWith({ skills: [skillSet.id] }), "agents/test/agent");

    expect(vi.mocked(filesApiSend).mock.calls.map(([method, apiPath]) => `${method} ${apiPath}`)).toEqual([
      "POST rooms",
      "POST rooms",
      "POST rooms/a%2Fb/sources",
      "PUT rooms/a%2Fb/model",
    ]);
  });

  it.each([
    ["a missing template", "skill-sets/none"],
    ["an agent", "agents/document/contract-reviewer"],
  ])("fails with a plain error before any room when a skill link is %s", async (_what, id) => {
    await expect(
      installTemplate(catalogWith({ skills: [id] }), "agents/test/agent"),
    ).rejects.toThrow(new Error(`Template agents/test/agent links an unknown template: ${id}`));
    expect(filesApiSend).not.toHaveBeenCalled();
  });

  it("makes no room when a knowledge link is broken, the skill links being sound", async () => {
    await expect(
      installTemplate(
        catalogWith({ skills: [skillSet.id], knowledges: ["knowledge-bases/none"] }),
        "agents/test/agent",
      ),
    ).rejects.toThrow(
      new Error("Template agents/test/agent links an unknown template: knowledge-bases/none"),
    );
    expect(filesApiSend).not.toHaveBeenCalled();
    expect(uploadFile).not.toHaveBeenCalled();
  });

  it("carries the step's own error and the rooms made when a later step fails", async () => {
    const failure = new Error("socket hang up");
    vi.mocked(uploadFile).mockRejectedValueOnce(failure);

    const thrown: unknown = await installTemplate(
      catalogWith({ skills: [skillSet.id] }),
      "agents/test/agent",
    ).catch((err: unknown) => err);

    expect(thrown).toBeInstanceOf(TemplateInstallIncompleteError);
    expect(thrown).toMatchObject({
      cause: failure,
      made: {
        skillSets: [{ templateId: skillSet.id, id: "101", title: skillSet.name }],
        knowledgeBases: [],
      },
    });
  });

  it("warns with the text of a model read that fails with something other than an error", async () => {
    portalChatProfileId.mockRejectedValue("AI service gone");

    const result = await installTemplate(catalogWith({}), "agents/test/agent");

    expect(result.warnings).toEqual(["modelNotSaved", "instructionsNotSaved"]);
    // The cause of the failed read, not a 404 made up for it.
    expect(vi.mocked(logger.warn).mock.calls.map(([line]) => line)).toEqual([
      "installTemplate: agents/test/agent: model not saved (AI service gone)",
    ]);
  });

  it("warns that the portal has no chat model when nothing is assigned", async () => {
    portalChatProfileId.mockResolvedValue(null);

    const result = await installTemplate(catalogWith({}), "agents/test/agent");

    expect(result.warnings).toEqual(["modelNotSaved", "instructionsNotSaved"]);
    expect(vi.mocked(logger.warn).mock.calls.map(([line]) => line)).toEqual([
      "installTemplate: agents/test/agent: model not saved (the portal has no chat model)",
    ]);
  });

  it.each([
    ["an error", new Error("socket hang up"), "socket hang up"],
    ["something other than an error", "dropped", "dropped"],
  ])("warns with the text of a model write that fails with %s", async (_what, failure, text) => {
    // The agent's room, then the model write.
    vi.mocked(filesApiSend)
      .mockReset()
      .mockResolvedValueOnce({ id: 5, title: "Agent" })
      .mockRejectedValueOnce(failure);

    const result = await installTemplate(catalogWith({}), "agents/test/agent");

    expect(result.warnings).toEqual(["modelNotSaved", "instructionsNotSaved"]);
    expect(vi.mocked(logger.warn).mock.calls.map(([line]) => line)).toEqual([
      `installTemplate: agents/test/agent: model not saved (${text})`,
    ]);
  });

  it.each([
    ["a null config", null],
    ["a config that is a string", "enabled"],
    ["a config that is a list", [{ vectorizationEnabled: true }]],
  ])("fails with 502 for %s, as an answer of an unknown shape, not as vectorization off", async (_what, config) => {
    aiConfig.mockResolvedValue(config);

    await expect(installTemplate(shipped, shipped.knowledgeBases[0]!.id)).rejects.toMatchObject({
      name: "UpstreamShapeError",
      status: 502,
    });
    expect(aiConfig).toHaveBeenCalledWith("/config");
    expect(filesApiSend).not.toHaveBeenCalled();
  });
});
