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

vi.mock("../../storage/httpClient.js", () => ({ AiServiceHttpError: class extends Error {} }));
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: class extends Error {},
}));
const assertAiAccess = vi.fn(async () => {});
vi.mock("../aiAccess.js", () => ({ assertAiAccess }));
const mayCreateAiRooms = vi.fn(async () => true);
vi.mock("../../storage/docspacePeopleApi.js", () => ({ mayCreateAiRooms }));
const installTemplate = vi.fn();
vi.mock("../../templates/installTemplate.js", () => ({ installTemplate }));
const catalog = { agents: [], skillSets: [], knowledgeBases: [] };
vi.mock("../../templates/catalog.js", () => ({ getTemplateCatalog: async () => catalog }));

const { templatesController } = await import("../templatesController.js");
const { handlerSettled } = await import("../_helpers.js");

type Handler = (req: never, res: never, next: never) => unknown;

async function install(body: unknown) {
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
  };
  const req = { body, method: "POST", originalUrl: "/ai/templates/install" };
  (templatesController.install as Handler)(req as never, res as never, vi.fn() as never);
  await handlerSettled(res);
  return res;
}

describe("templatesController.install", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mayCreateAiRooms.mockResolvedValue(true);
    installTemplate.mockResolvedValue({ id: "103", kind: "agent" });
  });

  it("installs the template for a caller who may create agents", async () => {
    const res = await install({ id: "agents/tables/formula-helper" });
    expect(installTemplate).toHaveBeenCalledWith(catalog, "agents/tables/formula-helper");
    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual({ id: "103", kind: "agent" });
  });

  it("refuses a caller who may not create agents before any room is made", async () => {
    mayCreateAiRooms.mockResolvedValue(false);
    const res = await install({ id: "agents/tables/formula-helper" });
    expect(res.statusCode).toBe(403);
    expect(installTemplate).not.toHaveBeenCalled();
  });
});
