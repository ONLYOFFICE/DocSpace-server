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

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));

class FakeAiServiceHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
    public readonly body = "",
  ) {
    super(statusText);
  }
}
class FakeDocspaceApiHttpError extends Error {
  constructor(
    public readonly status: number,
    public readonly statusText: string,
  ) {
    super(statusText);
  }
}
vi.mock("../../storage/httpClient.js", () => ({ AiServiceHttpError: FakeAiServiceHttpError }));
const filesApiGet = vi.fn();
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  filesApiGet,
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
}));
const readProjectProfileId = vi.fn();
const writeProjectProfileId = vi.fn();
vi.mock("../../storage/aiProjectModel.js", async (importOriginal) => ({
  canChat: (await importOriginal<typeof import("../../storage/aiProjectModel.js")>()).canChat,
  readProjectProfileId,
  writeProjectProfileId,
}));
const readById = vi.fn();
vi.mock("../../storage/index.js", () => ({ storage: { profiles: { readById } } }));

const { aiProjectModelController } = await import("../aiProjectModelController.js");

type Handler = (req: unknown, res: unknown, next: (e?: unknown) => void) => unknown;

const P1 = "11111111-1111-1111-1111-111111111111";

const call = async (
  handler: keyof typeof aiProjectModelController,
  id: string,
  body?: unknown,
): Promise<{ status: number; json: unknown }> => {
  const res = { status: vi.fn(), json: vi.fn(), headersSent: false };
  res.status.mockReturnValue(res);
  await (aiProjectModelController[handler] as unknown as Handler)(
    { params: { id }, body, method: "GET", originalUrl: "/" },
    res,
    vi.fn(),
  );
  return {
    status: (res.status.mock.calls[0]?.[0] as number | undefined) ?? 200,
    json: res.json.mock.calls[0]?.[0],
  };
};

const projectRoom = (editRoom = true) => ({
  id: 42,
  roomType: 12,
  security: { EditRoom: editRoom },
});

describe("aiProjectModelController", () => {
  beforeEach(() => {
    filesApiGet.mockReset();
    readProjectProfileId.mockReset();
    writeProjectProfileId.mockReset();
    readById.mockReset();
    filesApiGet.mockResolvedValue(projectRoom());
  });

  it("answers the project's own model in the DocSpace envelope", async () => {
    readProjectProfileId.mockResolvedValue(P1);
    readById.mockResolvedValue({
      id: P1,
      name: "GPT",
      providerType: "openai",
      capabilities: 1,
      key: "secret",
    });
    const { status, json } = await call("getModel", "42");
    expect(status).toBe(200);
    expect(json).toMatchObject({
      response: { profile: { id: P1, name: "GPT", provider: "openai", available: true } },
    });
    expect(JSON.stringify(json)).not.toContain("secret");
    expect(filesApiGet).toHaveBeenCalledWith("rooms/42");
  });

  it("answers a null profile for the portal default, and an unusable one as unavailable", async () => {
    readProjectProfileId.mockResolvedValueOnce(null);
    expect((await call("getModel", "42")).json).toMatchObject({ response: { profile: null } });
    readProjectProfileId.mockResolvedValueOnce(P1);
    readById.mockResolvedValueOnce(undefined);
    expect((await call("getModel", "42")).json).toMatchObject({
      response: { profile: { id: P1, available: false } },
    });
  });

  it("refuses a non-numeric id with 400 and a room that is not a project with 404", async () => {
    expect((await call("getModel", "abc")).status).toBe(400);
    filesApiGet.mockResolvedValueOnce({ id: 42, roomType: 6 });
    expect((await call("getModel", "42")).status).toBe(404);
  });

  it("relays the Files API status of a project the caller cannot read", async () => {
    filesApiGet.mockRejectedValueOnce(new FakeDocspaceApiHttpError(403, "Forbidden"));
    expect((await call("getModel", "42")).status).toBe(403);
    expect(readProjectProfileId).not.toHaveBeenCalled();
  });

  it("writes the model for a caller who may edit the room", async () => {
    readProjectProfileId.mockResolvedValue(P1);
    readById.mockResolvedValue({ id: P1, name: "GPT", providerType: "openai" });
    const { status, json } = await call("setModel", "42", { profileId: P1 });
    expect(status).toBe(200);
    expect(writeProjectProfileId).toHaveBeenCalledWith("42", P1);
    expect(json).toMatchObject({ response: { profile: { id: P1 } } });
  });

  it("takes null for the portal default", async () => {
    readProjectProfileId.mockResolvedValue(null);
    await call("setModel", "42", { profileId: null });
    expect(writeProjectProfileId).toHaveBeenCalledWith("42", null);
  });

  it("refuses a caller without EditRoom with 403 and a bad body with 400", async () => {
    filesApiGet.mockResolvedValueOnce(projectRoom(false));
    expect((await call("setModel", "42", { profileId: P1 })).status).toBe(403);
    expect((await call("setModel", "42", {})).status).toBe(400);
    expect((await call("setModel", "42", { profileId: 7 })).status).toBe(400);
    expect(writeProjectProfileId).not.toHaveBeenCalled();
  });

  it("relays a refused write with the server's message", async () => {
    writeProjectProfileId.mockRejectedValueOnce(
      new FakeAiServiceHttpError(
        403,
        "Forbidden",
        JSON.stringify({ error: { message: "No access" } }),
      ),
    );
    expect(await call("setModel", "42", { profileId: P1 })).toEqual({
      status: 403,
      json: { error: "No access" },
    });
  });
});
