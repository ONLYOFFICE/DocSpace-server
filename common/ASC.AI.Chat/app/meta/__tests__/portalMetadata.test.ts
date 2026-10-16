import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  FakeDocspaceApiHttpError,
  fakeFetch,
  headers,
  logger,
  portalJson,
  PROXY,
  withTimeout,
} from "./mocks.js";

vi.mock("../../log.js", () => ({ default: logger }));
vi.mock("../../storage/httpClient.js", () => ({ withTimeout, proxyBaseUrl: PROXY }));
vi.mock("../../requestContext.js", () => ({ getForwardedHeaders: () => headers }));
vi.mock("../../storage/docspaceFilesApi.js", () => ({
  DocspaceApiHttpError: FakeDocspaceApiHttpError,
}));

const { FieldType, getEntryMetadata, hasValue, parseEntryMetadata, setCustomFields, setValues } =
  await import("../portalMetadata.js");

describe("parseEntryMetadata", () => {
  it("reads templates, typed fields with options and values, and custom fields", () => {
    const parsed = parseEntryMetadata({
      templates: [
        {
          id: 7,
          name: "Contract",
          visible: true,
          fields: [
            { id: 1, name: "Counterparty", type: 0, options: [], order: 0 },
            {
              id: 2,
              name: "Signed on",
              type: 1,
              value: { dateValue: "2024-03-01T00:00:00.0000000Z" },
            },
            { id: 3, name: "Amount", type: 2, value: { numberValue: 1500 } },
            {
              id: 4,
              name: "Status",
              type: 3,
              options: [
                { id: "a1", value: "Draft" },
                { id: "b2", value: "Signed" },
              ],
              value: { optionIds: ["b2"] },
            },
          ],
        },
      ],
      customFields: [{ name: "Project", value: "Apollo" }, { name: "Empty" }],
    });
    expect(parsed.templates).toHaveLength(1);
    const [template] = parsed.templates;
    expect(template?.id).toBe(7);
    expect(template?.fields.map((f) => [f.id, f.type])).toEqual([
      [1, FieldType.String],
      [2, FieldType.Date],
      [3, FieldType.Number],
      [4, FieldType.SingleChoice],
    ]);
    expect(template?.fields[0]?.value).toBeUndefined();
    expect(template?.fields[1]?.value).toEqual({ dateValue: "2024-03-01T00:00:00.0000000Z" });
    expect(template?.fields[2]?.value).toEqual({ numberValue: 1500 });
    expect(template?.fields[3]?.options).toEqual([
      { id: "a1", value: "Draft" },
      { id: "b2", value: "Signed" },
    ]);
    expect(template?.fields[3]?.value).toEqual({ optionIds: ["b2"] });
    expect(parsed.customFields).toEqual([
      { name: "Project", value: "Apollo" },
      { name: "Empty", value: "" },
    ]);
  });

  it("accepts the enum spelled by name and drops fields it cannot type", () => {
    const parsed = parseEntryMetadata({
      templates: [
        {
          id: 1,
          fields: [
            { id: 1, name: "A", type: "MultiChoice" },
            { id: 2, name: "B", type: "date" },
            { id: 3, name: "C", type: 9 },
            { id: 4, name: "D", type: "unknown" },
            { name: "no id", type: 0 },
          ],
        },
      ],
    });
    expect(parsed.templates[0]?.fields.map((f) => [f.id, f.type])).toEqual([
      [1, FieldType.MultiChoice],
      [2, FieldType.Date],
    ]);
    expect(parsed.customFields).toEqual([]);
  });

  it("answers empty lists for anything that is not an object", () => {
    expect(parseEntryMetadata(undefined)).toEqual({ templates: [], customFields: [] });
    expect(parseEntryMetadata([])).toEqual({ templates: [], customFields: [] });
  });
});

describe("hasValue", () => {
  it("is true for exactly the members that carry something", () => {
    expect(hasValue(undefined)).toBe(false);
    expect(hasValue({})).toBe(false);
    expect(hasValue({ stringValue: "" })).toBe(false);
    expect(hasValue({ optionIds: [] })).toBe(false);
    expect(hasValue({ stringValue: "x" })).toBe(true);
    expect(hasValue({ numberValue: 0 })).toBe(true);
    expect(hasValue({ dateValue: "2024-01-01T00:00:00Z" })).toBe(true);
    expect(hasValue({ optionIds: ["a"] })).toBe(true);
  });
});

describe("the Files metadata calls", () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
  });

  it("reads an entry's metadata under the caller's headers", async () => {
    const { fetchMock, calls } = fakeFetch([
      {
        match: (url) => url.endsWith("/api/2.0/files/metadata/file/42"),
        answer: () => portalJson({ templates: [], customFields: [{ name: "A", value: "1" }] }),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const metadata = await getEntryMetadata("file", "42");
    expect(metadata.customFields).toEqual([{ name: "A", value: "1" }]);
    expect(calls[0]?.url).toBe(`${PROXY}/api/2.0/files/metadata/file/42`);
    expect((calls[0]?.init?.headers as Record<string, string>)["cookie"]).toBe(headers.cookie);
  });

  it("writes values with PUT and a JSON body, and parses the metadata that comes back", async () => {
    const { fetchMock, calls } = fakeFetch([
      {
        match: (url, init) => url.endsWith("/metadata/folder/5/values") && init?.method === "PUT",
        answer: () =>
          portalJson({
            templates: [
              { id: 1, fields: [{ id: 9, name: "N", type: 2, value: { numberValue: 3 } }] },
            ],
            customFields: [],
          }),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    const after = await setValues("folder", "5", [{ fieldId: 9, numberValue: 3 }]);
    expect(after.templates[0]?.fields[0]?.value).toEqual({ numberValue: 3 });
    const init = calls[0]?.init;
    expect(init?.method).toBe("PUT");
    expect((init?.headers as Record<string, string>)["content-type"]).toBe("application/json");
    expect(JSON.parse(String(init?.body))).toEqual({ values: [{ fieldId: 9, numberValue: 3 }] });
  });

  it("writes custom fields with PUT", async () => {
    const { fetchMock, calls } = fakeFetch([
      {
        match: (url, init) =>
          url.endsWith("/metadata/file/1/customFields") && init?.method === "PUT",
        answer: () => portalJson([{ name: "A", value: "x" }]),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    await setCustomFields("file", "1", [{ name: "A", value: "x" }]);
    expect(JSON.parse(String(calls[0]?.init?.body))).toEqual({
      fields: [{ name: "A", value: "x" }],
    });
  });

  it("relays a portal refusal as DocspaceApiHttpError with its status", async () => {
    const { fetchMock } = fakeFetch([
      {
        match: (url) => url.includes("/metadata/file/3"),
        answer: () => new Response("", { status: 403, statusText: "Forbidden" }),
      },
    ]);
    vi.stubGlobal("fetch", fetchMock);
    await expect(getEntryMetadata("file", "3")).rejects.toMatchObject({ status: 403 });
  });
});
