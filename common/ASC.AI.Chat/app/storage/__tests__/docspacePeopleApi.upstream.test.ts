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

import type { JsonBodyType } from "msw";
import { describe, expect, it } from "vitest";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import { DocspaceApiHttpError } from "../docspaceFilesApi.js";
import { getSelf, isPortalAdmin, mayCreateAiRooms } from "../docspacePeopleApi.js";
import { proxyBaseUrl } from "../httpClient.js";
import { inRequest } from "../../__tests__/fixtures/filesApi.js";

// The People API client over a stubbed network, with the real httpClient.js and requestContext.js.
const upstream = useUpstream();
const SELF = `${proxyBaseUrl}/api/2.0/people/@self`;

/**
 * `EmployeeFullDto` of a User as `GET api/2.0/people/@self` answers it
 * (common/ASC.Api.Core/Model/EmployeeFullDto.cs, PeopleController.SelfAsync), in the envelope.
 */
function selfDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    response: {
      firstName: "Jane",
      lastName: "Doe",
      userName: "jane",
      email: "jane@portal.example",
      status: 1,
      activationStatus: 1,
      department: "",
      groups: [],
      isAdmin: false,
      isRoomAdmin: false,
      isLDAP: false,
      listAdminModules: [],
      isOwner: false,
      isVisitor: false,
      isCollaborator: true,
      cultureName: "en-US",
      isSSO: false,
      quotaLimit: -1,
      usedSpace: 0,
      hasPersonalFolder: true,
      id: "a0d6bc40-2b3c-4c1a-9a7d-1f1b2c3d4e5f",
      displayName: "Jane Doe",
      hasAvatar: false,
      isAnonim: false,
      ...overrides,
    },
    count: 1,
    status: 0,
    statusCode: 200,
  };
}

const answer = (body: JsonBodyType) => upstream.use(http.get(SELF, () => HttpResponse.json(body)));

const failure = (p: Promise<unknown>): Promise<unknown> =>
  p.then(
    () => {
      throw new Error("expected a rejection");
    },
    (e: unknown) => e,
  );

describe("getSelf", () => {
  it("reads the role flags of the caller with the caller's credentials", async () => {
    answer(selfDto({ isAdmin: true, isRoomAdmin: true }));

    await expect(
      inRequest(() => getSelf(), { headers: { cookie: "asc_auth_key=1", authorization: "B t" } }),
    ).resolves.toStrictEqual({ isAdmin: true, isOwner: false, isRoomAdmin: true });
    expect(upstream.seen).toEqual([
      expect.objectContaining({ method: "GET", path: "/api/2.0/people/@self" }),
    ]);
    expect(upstream.seen[0]?.headers).toMatchObject({
      cookie: "asc_auth_key=1",
      authorization: "B t",
    });
  });

  it.each(["isAdmin", "isOwner", "isRoomAdmin"])(
    "reads %s as false when it is missing, null, a string or a number",
    async (flag) => {
      for (const value of [undefined, null, "true", 1]) {
        answer(selfDto({ isAdmin: true, isOwner: true, isRoomAdmin: true, [flag]: value }));
        expect(await getSelf()).toStrictEqual({
          isAdmin: true,
          isOwner: true,
          isRoomAdmin: true,
          [flag]: false,
        });
      }
    },
  );

  it.each([
    ["response is missing", { status: 0 }],
    ["response is null", { response: null }],
    ["response is a list", { response: [selfDto()["response"]] }],
    ["response is a string", { response: "me" }],
    ["the body is a list", [selfDto()]],
  ])("refuses an answer when %s", async (_case, body) => {
    answer(body);

    const error = await failure(getSelf());

    expect(error).not.toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toMatchObject({ message: `DocSpace API: unparseable response from ${SELF}` });
  });

  it("refuses an answer that is not JSON", async () => {
    upstream.use(http.get(SELF, () => HttpResponse.text("<html>")));

    expect(await failure(getSelf())).toBeInstanceOf(SyntaxError);
  });

  it.each([401, 403, 500, 503])("rejects a %i with a DocspaceApiHttpError", async (status) => {
    upstream.use(http.get(SELF, () => HttpResponse.json({}, { status, statusText: "Reason" })));

    const error = await failure(getSelf());

    expect(error).toBeInstanceOf(DocspaceApiHttpError);
    expect(error).toMatchObject({ status, statusText: "Reason", url: SELF });
  });
});

describe("isPortalAdmin", () => {
  it.each([
    ["the owner", { isOwner: true }, true],
    ["a DocSpace admin", { isAdmin: true }, true],
    ["a room admin", { isRoomAdmin: true }, false],
    ["a User", {}, false],
    ["a Guest", { isVisitor: true, isCollaborator: false }, false],
  ])("answers for %s", async (_case, flags, expected) => {
    answer(selfDto(flags));

    await expect(isPortalAdmin()).resolves.toBe(expected);
  });

  it("fails when the caller cannot be read", async () => {
    upstream.use(http.get(SELF, () => HttpResponse.json({}, { status: 500 })));

    expect(await failure(isPortalAdmin())).toMatchObject({ status: 500 });
  });
});

describe("mayCreateAiRooms", () => {
  it.each([
    ["the owner", { isOwner: true }, true],
    ["a DocSpace admin", { isAdmin: true }, true],
    ["a room admin", { isRoomAdmin: true }, true],
    ["a User", {}, false],
  ])("answers for %s", async (_case, flags, expected) => {
    answer(selfDto(flags));

    await expect(mayCreateAiRooms()).resolves.toBe(expected);
  });

  it("fails when the caller cannot be read", async () => {
    upstream.use(http.get(SELF, () => HttpResponse.error()));

    expect(await failure(mayCreateAiRooms())).toBeInstanceOf(TypeError);
  });
});
