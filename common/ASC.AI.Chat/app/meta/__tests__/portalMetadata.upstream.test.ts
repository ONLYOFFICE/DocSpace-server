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

// The Files metadata API as `portalMetadata.ts` reads and writes it, stubbed at the network (msw):
// the real `httpClient.js` and `requestContext.js` run. The fixture is the full
// `EntryMetadataDto` of `GET api/2.0/files/metadata/{entryType}/{entryId}`
// (products/ASC.Files/Core/ApiModels/ResponseDto/MetadataDto.cs, served by
// products/ASC.Files/Server/Api/MetadataController.cs); each field of each DTO is then sent
// missing, null and of the wrong type, and the mapped result is pinned.

import { afterEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { delay, http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import { DocspaceApiHttpError } from "../../storage/docspaceFilesApi.js";
import { UpstreamShapeError } from "../../storage/upstreamShape.js";
import {
  FieldType,
  getEntryMetadata,
  parseEntryMetadata,
  setCustomFields,
  setValues,
} from "../portalMetadata.js";

const upstream = useUpstream();

afterEach(() => {
  vi.useRealTimers();
});

function envelope(response: unknown, status = 200): Response {
  return HttpResponse.json({ response, count: 1, status: 0, statusCode: status }, { status });
}

// MetadataValueDto: DateValue is an ApiDateTime, NumberValue a long?, OptionIds a List<Guid>.
const VALUE_DTO = {
  stringValue: "Acme",
  numberValue: 42,
  dateValue: "2026-11-01T00:00:00.0000000+00:00",
  optionIds: ["6f1c1f8e-0000-4000-8000-000000000001"],
};

// EntryFieldDto.
const FIELD_DTO = {
  id: 7,
  name: "Status",
  type: 3,
  options: [
    { id: "6f1c1f8e-0000-4000-8000-000000000001", value: "Draft" },
    { id: "6f1c1f8e-0000-4000-8000-000000000002", value: "Signed" },
  ],
  order: 1,
  value: VALUE_DTO,
};

// EntryTemplateDto.
const TEMPLATE_DTO = {
  id: 3,
  name: "Contract",
  visible: true,
  cascade: false,
  conflictResolveType: null,
  fields: [FIELD_DTO],
};

// EntryMetadataDto.
const METADATA_DTO = {
  templates: [TEMPLATE_DTO],
  customFields: [{ name: "Project", value: "Apollo" }],
  hasMetadataFields: true,
};

const FIELD = {
  id: 7,
  name: "Status",
  type: FieldType.SingleChoice,
  options: [
    { id: "6f1c1f8e-0000-4000-8000-000000000001", value: "Draft" },
    { id: "6f1c1f8e-0000-4000-8000-000000000002", value: "Signed" },
  ],
  value: {
    stringValue: "Acme",
    numberValue: 42,
    dateValue: "2026-11-01T00:00:00.0000000+00:00",
    optionIds: ["6f1c1f8e-0000-4000-8000-000000000001"],
  },
};

const METADATA = {
  templates: [{ id: 3, name: "Contract", visible: true, fields: [FIELD] }],
  customFields: [{ name: "Project", value: "Apollo" }],
};

function withField(patch: Record<string, unknown>) {
  return {
    ...METADATA_DTO,
    templates: [{ ...TEMPLATE_DTO, fields: [{ ...FIELD_DTO, ...patch }] }],
  };
}

function withValue(patch: Record<string, unknown>) {
  return withField({ value: { ...VALUE_DTO, ...patch } });
}

function expectField(field: unknown) {
  return { ...METADATA, templates: [{ ...METADATA.templates[0], fields: field ? [field] : [] }] };
}

describe("getEntryMetadata against the Files API", () => {
  it("reads the full DTO under the caller's credentials", async () => {
    upstream.use(http.get("*/api/2.0/files/metadata/:kind/:id", () => envelope(METADATA_DTO)));

    const metadata = await inRequestContext({ cookie: "asc_auth_key=k" }, () =>
      getEntryMetadata("file", "10"),
    );

    expect(metadata).toEqual(METADATA);
    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]).toMatchObject({
      method: "GET",
      path: "/api/2.0/files/metadata/file/10",
      body: undefined,
    });
    expect(upstream.seen[0]?.headers).toMatchObject({ cookie: "asc_auth_key=k" });
    expect(upstream.seen[0]?.headers).not.toHaveProperty("content-type");
  });

  it("encodes the id into the path", async () => {
    upstream.use(http.get("*/api/2.0/files/metadata/folder/*", () => envelope(METADATA_DTO)));

    await getEntryMetadata("folder", "a/b");

    expect(upstream.seen[0]?.path).toBe("/api/2.0/files/metadata/folder/a%2Fb");
  });

  it.each([
    [
      "templates missing",
      { ...METADATA_DTO, templates: undefined },
      { ...METADATA, templates: [] },
    ],
    ["templates null", { ...METADATA_DTO, templates: null }, { ...METADATA, templates: [] }],
    [
      "templates an object",
      { ...METADATA_DTO, templates: TEMPLATE_DTO },
      { ...METADATA, templates: [] },
    ],
    [
      "customFields missing",
      { ...METADATA_DTO, customFields: undefined },
      { ...METADATA, customFields: [] },
    ],
    [
      "customFields null",
      { ...METADATA_DTO, customFields: null },
      { ...METADATA, customFields: [] },
    ],
    [
      "customFields a string",
      { ...METADATA_DTO, customFields: "Project" },
      { ...METADATA, customFields: [] },
    ],
    [
      "a custom field without a name, with a null name and with a numeric name",
      {
        ...METADATA_DTO,
        customFields: [{ value: "x" }, { name: null }, { name: 5 }, { name: "" }],
      },
      { ...METADATA, customFields: [] },
    ],
    [
      "a custom field with a null value and with a numeric value",
      {
        ...METADATA_DTO,
        customFields: [
          { name: "A", value: null },
          { name: "B", value: 5 },
        ],
      },
      {
        ...METADATA,
        customFields: [
          { name: "A", value: "" },
          { name: "B", value: "" },
        ],
      },
    ],
    [
      "a template id missing, null and a string",
      {
        ...METADATA_DTO,
        templates: [
          { ...TEMPLATE_DTO, id: undefined },
          { ...TEMPLATE_DTO, id: null },
          { ...TEMPLATE_DTO, id: "3" },
        ],
      },
      { ...METADATA, templates: [] },
    ],
    [
      "a template name missing and a number",
      {
        ...METADATA_DTO,
        templates: [
          { ...TEMPLATE_DTO, name: undefined },
          { ...TEMPLATE_DTO, name: 5 },
        ],
      },
      {
        ...METADATA,
        templates: [
          { ...METADATA.templates[0], name: "" },
          { ...METADATA.templates[0], name: "" },
        ],
      },
    ],
    [
      "visible false, missing, null and a string",
      {
        ...METADATA_DTO,
        templates: [
          { ...TEMPLATE_DTO, visible: false },
          { ...TEMPLATE_DTO, visible: undefined },
          { ...TEMPLATE_DTO, visible: null },
          { ...TEMPLATE_DTO, visible: "false" },
        ],
      },
      {
        ...METADATA,
        templates: [
          { ...METADATA.templates[0], visible: false },
          { ...METADATA.templates[0], visible: true },
          { ...METADATA.templates[0], visible: true },
          { ...METADATA.templates[0], visible: true },
        ],
      },
    ],
    [
      "template fields missing, null and an object",
      {
        ...METADATA_DTO,
        templates: [
          { ...TEMPLATE_DTO, fields: undefined },
          { ...TEMPLATE_DTO, fields: null },
          { ...TEMPLATE_DTO, fields: FIELD_DTO },
        ],
      },
      {
        ...METADATA,
        templates: [
          { ...METADATA.templates[0], fields: [] },
          { ...METADATA.templates[0], fields: [] },
          { ...METADATA.templates[0], fields: [] },
        ],
      },
    ],
  ])("maps a metadata DTO with %s", async (_what, dto, expected) => {
    upstream.use(http.get("*/api/2.0/files/metadata/:kind/:id", () => envelope(dto)));

    expect(await getEntryMetadata("file", "10")).toEqual(expected);
  });

  it.each([
    ["id missing", { id: undefined }, undefined],
    ["id null", { id: null }, undefined],
    ["id a string", { id: "7" }, undefined],
    ["name missing", { name: undefined }, undefined],
    ["name null", { name: null }, undefined],
    ["name empty", { name: "" }, undefined],
    ["name a number", { name: 5 }, undefined],
    ["type missing", { type: undefined }, undefined],
    ["type null", { type: null }, undefined],
    ["type out of the enum", { type: 9 }, undefined],
    ["type a boolean", { type: true }, undefined],
    ["type an unknown name", { type: "Text" }, undefined],
    ["type a name in any case", { type: "SINGLECHOICE" }, FIELD],
    ["type the number 0", { type: 0 }, { ...FIELD, type: FieldType.String }],
    ["options missing", { options: undefined }, { ...FIELD, options: [] }],
    ["options null", { options: null }, { ...FIELD, options: [] }],
    ["options a string", { options: "Draft" }, { ...FIELD, options: [] }],
    [
      "options with a missing, null, numeric or empty id and a missing or null value",
      {
        options: [
          { value: "a" },
          { id: null, value: "b" },
          { id: 5, value: "c" },
          { id: "", value: "d" },
          { id: "e" },
          { id: "f", value: null },
          { id: "g", value: "" },
          "Draft",
        ],
      },
      { ...FIELD, options: [{ id: "g", value: "" }] },
    ],
    ["value missing", { value: undefined }, (({ value: _v, ...rest }) => rest)(FIELD)],
    ["value null", { value: null }, (({ value: _v, ...rest }) => rest)(FIELD)],
    ["value a string", { value: "Acme" }, (({ value: _v, ...rest }) => rest)(FIELD)],
    ["value empty", { value: {} }, { ...FIELD, value: {} }],
  ])("maps a field DTO with %s", async (_what, patch, expected) => {
    upstream.use(http.get("*/api/2.0/files/metadata/:kind/:id", () => envelope(withField(patch))));

    expect(await getEntryMetadata("file", "10")).toEqual(expectField(expected));
  });

  it.each([
    ["stringValue", null],
    ["stringValue", 5],
    ["numberValue", null],
    ["numberValue", "42"],
    ["numberValue", undefined],
    ["dateValue", null],
    ["dateValue", 20261101],
    ["optionIds", null],
    ["optionIds", "6f1c1f8e"],
  ])("leaves %s out of the value when it is %j", async (key, bad) => {
    upstream.use(
      http.get("*/api/2.0/files/metadata/:kind/:id", () => envelope(withValue({ [key]: bad }))),
    );

    const { [key as keyof typeof FIELD.value]: _dropped, ...value } = FIELD.value;
    expect(await getEntryMetadata("file", "10")).toEqual(expectField({ ...FIELD, value }));
  });

  it("keeps the string option ids of a mixed list and an empty string value", async () => {
    upstream.use(
      http.get("*/api/2.0/files/metadata/:kind/:id", () =>
        envelope(withValue({ optionIds: ["a", 5, null, "b"], stringValue: "" })),
      ),
    );

    expect(await getEntryMetadata("file", "10")).toEqual(
      expectField({ ...FIELD, value: { ...FIELD.value, optionIds: ["a", "b"], stringValue: "" } }),
    );
  });

  // The Files API answers an entry without metadata with empty lists: a response of another
  // shape is unknown, and meta/fill would answer it as "no fields to fill" (422).
  it.each([
    ["a response that is a list", () => envelope([METADATA_DTO])],
    ["a response that is null", () => envelope(null)],
  ])("rejects %s as an answer of an unknown shape", async (_what, answer) => {
    upstream.use(http.get("*/api/2.0/files/metadata/:kind/:id", answer));

    await expect(getEntryMetadata("file", "10")).rejects.toEqual(
      new UpstreamShapeError("GET files/metadata/file/10"),
    );
  });

  it.each([
    ["a body without a response", () => HttpResponse.json({ status: 0 })],
    ["a body that is a list", () => HttpResponse.json([METADATA_DTO])],
  ])("rejects %s instead of reading it as no metadata", async (_what, answer) => {
    upstream.use(http.get("*/api/2.0/files/metadata/:kind/:id", answer));

    await expect(getEntryMetadata("file", "10")).rejects.toEqual(
      new UpstreamShapeError("GET files/metadata/file/10"),
    );
  });

  it.each([
    [403, "Forbidden"],
    [404, "Not Found"],
    [500, "Server Error"],
  ])("rejects a read the portal answers %i for with its status", async (status, statusText) => {
    upstream.use(
      http.get("*/api/2.0/files/metadata/:kind/:id", () =>
        HttpResponse.json({ error: "secret" }, { status, statusText }),
      ),
    );

    const failure = await getEntryMetadata("file", "10").catch((err: unknown) => err);

    expect(failure).toBeInstanceOf(DocspaceApiHttpError);
    expect(failure).toMatchObject({
      status,
      statusText,
      url: expect.stringMatching(/\/api\/2\.0\/files\/metadata\/file\/10$/),
    });
  });

  it("rejects a read that does not answer within the upstream timeout", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    upstream.use(
      http.get("*/api/2.0/files/metadata/:kind/:id", async () => {
        await delay("infinite");
        return envelope(METADATA_DTO);
      }),
    );

    const read = getEntryMetadata("file", "10").catch((err: unknown) => err);
    await vi.advanceTimersByTimeAsync(300_000);

    expect(await read).toMatchObject({ message: "upstream timeout after 300000ms" });
  });

  it("rejects a body that is not JSON", async () => {
    upstream.use(http.get("*/api/2.0/files/metadata/:kind/:id", () => HttpResponse.text("<html>")));

    await expect(getEntryMetadata("file", "10")).rejects.toEqual(
      new UpstreamShapeError("GET files/metadata/file/10"),
    );
  });
});

describe("the metadata writes against the Files API", () => {
  it("PUTs the values with a JSON body and the caller's headers and maps the answer", async () => {
    upstream.use(
      http.put("*/api/2.0/files/metadata/:kind/:id/values", () => envelope(METADATA_DTO)),
    );

    const after = await inRequestContext({ authorization: "Bearer t" }, () =>
      setValues("file", "10", [{ fieldId: 7, optionIds: ["x"] }, { fieldId: 8 }]),
    );

    expect(after).toEqual(METADATA);
    expect(upstream.seen[0]).toMatchObject({
      method: "PUT",
      path: "/api/2.0/files/metadata/file/10/values",
      body: { values: [{ fieldId: 7, optionIds: ["x"] }, { fieldId: 8 }] },
    });
    expect(upstream.seen[0]?.headers).toMatchObject({
      authorization: "Bearer t",
      "content-type": "application/json",
    });
  });

  it("PUTs the custom fields, a null value included, and ignores the answer", async () => {
    upstream.use(
      http.put("*/api/2.0/files/metadata/:kind/:id/customFields", () => envelope("anything")),
    );

    await expect(
      setCustomFields("folder", "4", [
        { name: "A", value: "1" },
        { name: "B", value: null },
      ]),
    ).resolves.toBeUndefined();
    expect(upstream.seen[0]).toMatchObject({
      method: "PUT",
      path: "/api/2.0/files/metadata/folder/4/customFields",
      body: {
        fields: [
          { name: "A", value: "1" },
          { name: "B", value: null },
        ],
      },
    });
  });

  it.each([
    [403, "Forbidden"],
    [500, "Server Error"],
  ])("rejects a write the portal answers %i for", async (status, statusText) => {
    upstream.use(
      http.put("*/api/2.0/files/metadata/:kind/:id/values", () =>
        HttpResponse.json({}, { status, statusText }),
      ),
    );

    const failure = await setValues("file", "10", []).catch((err: unknown) => err);

    expect(failure).toBeInstanceOf(DocspaceApiHttpError);
    expect(failure).toMatchObject({ status, statusText });
  });
});

describe("parseEntryMetadata", () => {
  it.each([null, undefined, "x", 5, [METADATA_DTO]])("answers empty lists for %j", (raw) => {
    expect(parseEntryMetadata(raw)).toEqual({ templates: [], customFields: [] });
  });
});
