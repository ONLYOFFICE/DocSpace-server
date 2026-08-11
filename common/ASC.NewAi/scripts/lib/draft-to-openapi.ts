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

// Convert the JSON-Schema draft-07 output of `ts-json-schema-generator` into
// the subset OpenAPI accepts. `const` and the `examples` array are JSON Schema
// keywords that openapi 3.1 takes verbatim, so they now pass through untouched.
// Handled differences:
//   • `#/definitions/X`      → `#/components/schemas/X`
//   • `type: [T, "null"]`    → `type: T, nullable: true`
//   • strips `$schema` / `$id` / `$comment`
// Everything else (properties, `$ref`, `enum`, `additionalProperties`,
// `oneOf`/`anyOf`/`allOf`, `items`, `required`, `description`) passes through
// unchanged.

type Json = unknown;
type JsonObject = Record<string, Json>;

const DEFINITIONS_REF = "#/definitions/";
const COMPONENTS_REF = "#/components/schemas/";

function isObject(node: Json): node is JsonObject {
  return typeof node === "object" && node !== null && !Array.isArray(node);
}

// Keys whose value is a map of *arbitrary names* → sub-schema (property
// names, not schema keywords). Their child keys must be preserved verbatim
// and only the values recursed — otherwise a property literally named `type`
// / `const` / `$ref` would be misread as a schema keyword.
const SCHEMA_MAP_KEYS = new Set([
  "properties",
  "patternProperties",
  "definitions",
  "$defs",
]);

function convertSchemaMap(node: Json): Json {
  if (!isObject(node)) {
    return convertNode(node);
  }
  const out: JsonObject = {};
  for (const [name, value] of Object.entries(node)) {
    out[name] = convertNode(value);
  }
  return out;
}

// OpenAPI 3.0 has no `null` type: a union member `{ "type": "null" }`
// (emitted by the generator for `X | null`) must become `nullable: true` on
// the union itself. A single surviving member is collapsed into the parent —
// via `allOf` when it is a `$ref`, since 3.0 ignores siblings of `$ref`.
function normalizeNullableUnion(node: JsonObject): JsonObject {
  for (const keyword of ["anyOf", "oneOf"] as const) {
    const members = node[keyword];
    if (!Array.isArray(members)) {
      continue;
    }
    const kept = members.filter(
      (m) => !(isObject(m) && m["type"] === "null" && Object.keys(m).length === 1),
    );
    if (kept.length === members.length) {
      continue;
    }

    const out: JsonObject = { ...node, nullable: true };
    delete out[keyword];

    if (kept.length === 1) {
      const only = kept[0];
      if (isObject(only) && typeof only["$ref"] === "string") {
        out["allOf"] = [only];
      } else if (isObject(only)) {
        Object.assign(out, only);
      }
    } else if (kept.length > 1) {
      out[keyword] = kept;
    }
    return out;
  }
  return node;
}

// Widen two schemas for the same property name across union members: keep a
// shared `type` but drop value constraints that only held for one branch
// (e.g. the `success: true` / `success: false` discriminator literals).
//
// Exception: string-literal discriminators (a `type: "message-start" | …`
// field, emitted as single-value `enum`s) have their value sets UNIONed
// instead of dropped — this preserves the discriminator's allowed values
// (e.g. every `ChatEvent.type`) while still collapsing to one SDK-friendly
// object. Non-string discriminators (booleans) keep the widen-to-`type`
// behaviour.
// The allowed string values of a discriminator, however it is spelled: a
// single literal arrives as `const`, a closed set as `enum`.
function stringValues(node: JsonObject): string[] | null {
  const values = node["enum"];
  if (Array.isArray(values) && values.length > 0 && values.every((v) => typeof v === "string")) {
    return values as string[];
  }
  if (typeof node["const"] === "string") {
    return [node["const"]];
  }
  return null;
}

function widen(a: Json, b: Json): Json {
  if (JSON.stringify(a) === JSON.stringify(b)) {
    return a;
  }
  if (isObject(a) && isObject(b)) {
    const aEnum = stringValues(a);
    const bEnum = stringValues(b);
    if (aEnum !== null && bEnum !== null) {
      const seen = new Set<string>();
      const values = [...aEnum, ...bEnum].filter((v) => {
        if (seen.has(v)) {
          return false;
        }
        seen.add(v);
        return true;
      });
      const merged: JsonObject = values.length === 1
        ? { const: values[0] }
        : { enum: values };
      if (a["type"] === b["type"] && a["type"] !== undefined) {
        merged["type"] = a["type"];
      }
      const description = a["description"] ?? b["description"];
      if (description !== undefined) {
        merged["description"] = description;
      }
      return merged;
    }
    if (a["type"] === b["type"] && a["type"] !== undefined) {
      const merged: JsonObject = { type: a["type"] };
      const description = a["description"] ?? b["description"];
      if (description !== undefined) {
        merged["description"] = description;
      }
      return merged;
    }
  }
  return {};
}

/**
 * Collapse a union of anonymous object branches into one object with the
 * union of their properties (required = properties present in every branch).
 *
 * TypeScript discriminated unions (`{success:true,…} | {success:false,…}`)
 * become `anyOf` of *inline, unnamed* objects. SDK generators turn those into
 * `IValidatableObject` composition wrappers with inline sub-models, which is
 * the shape that fails to compile. Branches that are `$ref`s are left alone —
 * those resolve to real named models.
 */
function flattenObjectUnion(node: JsonObject): JsonObject {
  for (const keyword of ["anyOf", "oneOf"] as const) {
    const members = node[keyword];
    if (!Array.isArray(members) || members.length < 2) {
      continue;
    }
    const allInlineObjects = members.every(
      (m) => isObject(m) && m["type"] === "object" && isObject(m["properties"]),
    );
    if (!allInlineObjects) {
      continue;
    }

    const properties: JsonObject = {};
    let required: string[] | undefined;
    for (const member of members as JsonObject[]) {
      const memberProps = member["properties"] as JsonObject;
      for (const [name, schema] of Object.entries(memberProps)) {
        properties[name] = name in properties
          ? widen(properties[name]!, schema)
          : schema;
      }
      const memberRequired = Array.isArray(member["required"])
        ? (member["required"] as string[])
        : [];
      required = required === undefined
        ? memberRequired
        : required.filter((r) => memberRequired.includes(r));
    }

    const out: JsonObject = { ...node, type: "object", properties };
    delete out[keyword];
    if (required && required.length > 0) {
      out["required"] = required;
    } else {
      delete out["required"];
    }
    return out;
  }
  return node;
}

function convertNode(node: Json): Json {
  if (Array.isArray(node)) {
    return node.map(convertNode);
  }
  if (!isObject(node)) {
    return node;
  }

  const out: JsonObject = {};
  for (const [key, value] of Object.entries(node)) {
    if (SCHEMA_MAP_KEYS.has(key)) {
      out[key] = convertSchemaMap(value);
      continue;
    }
    switch (key) {
      case "$schema":
      case "$id":
      case "$comment":
        // draft-only metadata with no OpenAPI equivalent.
        break;
      case "$ref":
        out["$ref"] = typeof value === "string"
          ? value.replace(DEFINITIONS_REF, COMPONENTS_REF)
          : value;
        break;
      case "type":
        if (Array.isArray(value)) {
          const nonNull = value.filter((t) => t !== "null");
          if (value.includes("null")) {
            out["nullable"] = true;
          }
          if (nonNull.length === 1) {
            out["type"] = nonNull[0];
          } else if (nonNull.length > 1) {
            out["oneOf"] = nonNull.map((t) => ({ type: t }));
          }
          // Only "null" → leave `type` unset, `nullable: true` already set.
        } else {
          out["type"] = value;
        }
        break;
      default:
        out[key] = convertNode(value);
        break;
    }
  }

  // An empty-schema `additionalProperties: {}` (emitted for `Record<string,
  // unknown>` / index signatures / an arbitrary JSON blob) means "any extra
  // property, any value" — identical to omitting the keyword, but the docs UI
  // renders the empty map as noise (`additionalProp1/2/3`). Drop it so such a
  // field shows as a plain open object. A TYPED map (`{ type: "string" }`,
  // e.g. `headers`) or `additionalProperties: false` is meaningful and kept.
  if (
    isObject(out["additionalProperties"])
    && Object.keys(out["additionalProperties"] as JsonObject).length === 0
  ) {
    delete out["additionalProperties"];
  }

  return flattenObjectUnion(normalizeNullableUnion(out));
}

/**
 * Convert a `{ name: draft07Schema }` definitions map into an OpenAPI 3.0
 * `components.schemas` map.
 */
export function toOpenApiSchemas(
  definitions: Record<string, Json>,
): Record<string, Json> {
  const schemas: Record<string, Json> = {};
  for (const [name, schema] of Object.entries(definitions)) {
    schemas[name] = convertNode(schema);
  }
  return schemas;
}
