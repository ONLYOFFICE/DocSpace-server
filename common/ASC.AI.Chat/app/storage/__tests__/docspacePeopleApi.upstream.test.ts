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

import { UpstreamShapeError } from "../upstreamShape.js";
import type { JsonBodyType } from "msw";
import { describe, expect, it } from "vitest";
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import { DocspaceApiHttpError } from "../docspaceFilesApi.js";
import { getSelf, getSelfId, isPortalAdmin, mayCreateAiRooms } from "../docspacePeopleApi.js";
import { proxyBaseUrl } from "../httpClient.js";
import { inRequest } from "../../__tests__/fixtures/filesApi.js";
import { selfDto } from "../../__tests__/fixtures/peopleApi.js";

// The People API client over a stubbed network, with the real httpClient.js and requestContext.js.
const upstream = useUpstream();
const SELF = `${proxyBaseUrl}/api/2.0/people/@self`;

const answer = (body: JsonBodyType) => upstream.use(http.get(SELF, () => HttpResponse.json(body)));

const failure = (p: Promise<unknown>): Promise<unknown> =>
  p.then(
    () => {
      throw new Error("expected a rejection");
    },
    (e: unknown) => e,
  );

describe("getSelfId", () => {
  it("reads the caller's id", async () => {
    answer(selfDto());

    await expect(getSelfId()).resolves.toBe("a0d6bc40-2b3c-4c1a-9a7d-1f1b2c3d4e5f");
  });

  it.each([undefined, null, "", 5])("refuses an answer whose id is %j", async (id) => {
    answer(selfDto({ id }));

    expect(await failure(getSelfId())).toBeInstanceOf(UpstreamShapeError);
  });

  it("passes on a refusal of the portal", async () => {
    upstream.use(http.get(SELF, () => HttpResponse.json({}, { status: 500 })));

    expect(await failure(getSelfId())).toMatchObject({ status: 500 });
  });
});

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
    expect(error).toEqual(new UpstreamShapeError("GET people/@self"));
  });

  it("refuses an answer that is not JSON", async () => {
    upstream.use(http.get(SELF, () => HttpResponse.text("<html>")));

    expect(await failure(getSelf())).toEqual(new UpstreamShapeError("GET people/@self"));
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
