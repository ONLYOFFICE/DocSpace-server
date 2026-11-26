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

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

// Reads the C# sources of the .NET services as text: which properties a DTO class has, of what
// kind, and what an enum's members are. Enough for the contract test to compare a DTO with the
// Node code that reads it; it is not a C# parser and knows only the shapes these DTOs use.

const HERE = path.dirname(fileURLToPath(import.meta.url));
/** The repository root (`server/`), four levels above this file. */
export const REPO_ROOT = path.resolve(HERE, "../../../../..");
/** The module itself, `common/ASC.AI.Chat`. */
export const MODULE_ROOT = path.resolve(HERE, "../../..");

/** How a value of a C# type arrives in JSON. `any` is a type this reader cannot place. */
export type JsonKind = "string" | "number" | "boolean" | "array" | "object" | "any";

export interface CsProperty {
  /** The JSON name: the C# name in camelCase, or its `[JsonPropertyName]`. */
  name: string;
  /** The C# type as written, `string?`, `List<FolderDto>`. */
  type: string;
  kind: JsonKind;
  /** Declared with `?`: the value may be null or absent. */
  nullable: boolean;
}

export interface CsEnum {
  /** Member names in declaration order, with their numeric values. */
  members: { name: string; value: number }[];
  /** Serialized by name (a `JsonStringEnumConverter` on the type) instead of by number. */
  asString: boolean;
  /** The naming policy of that converter is camelCase. */
  camelCase: boolean;
}

const camel = (name: string): string => name.charAt(0).toLowerCase() + name.slice(1);

function read(file: string): string {
  return fs.readFileSync(path.join(REPO_ROOT, file), "utf8").replace(/^﻿/, "");
}

/** The text of a `{ ... }` block that opens at or after `from`. */
function block(source: string, from: number): string {
  const open = source.indexOf("{", from);
  let depth = 0;
  for (let i = open; i < source.length; i++) {
    if (source[i] === "{") depth++;
    if (source[i] === "}" && --depth === 0) {
      return source.slice(open + 1, i);
    }
  }
  throw new Error("unbalanced braces in a C# source");
}

function declaration(source: string, keyword: string, name: string): RegExpExecArray | null {
  return new RegExp(`\\b${keyword}\\s+${name}\\b(?:<[^>{]*>)?([^{;]*)`).exec(source);
}

/** True when the repository's C# tree is there: the module can be checked out without it. */
export function hasCsharpSources(): boolean {
  return fs.existsSync(path.join(REPO_ROOT, "products/ASC.AI/Server"));
}

export function readEnum(file: string, name: string): CsEnum {
  const source = read(file);
  const found = declaration(source, "enum", name);
  if (!found) {
    throw new Error(`${file} declares no enum ${name}`);
  }
  const members: CsEnum["members"] = [];
  let next = 0;
  const body = block(source, found.index).replace(/\/\/[^\n]*|\[[^\]]*\]/g, "");
  for (const part of body.split(",")) {
    const member = /^\s*(\w+)\s*(?:=\s*(0x[0-9a-fA-F]+|-?\d+))?\s*$/.exec(part);
    if (!member?.[1]) {
      continue;
    }
    const value = member[2] === undefined ? next : Number(member[2]);
    members.push({ name: member[1], value });
    next = value + 1;
  }
  const converter = new RegExp(
    `\\[JsonConverter\\(typeof\\((\\w+)[^\\]]*\\]\\s*public\\s+enum\\s+${name}\\b`,
  ).exec(source);
  const converterName = converter?.[1] ?? "";
  const ownConverter = converterName
    ? (new RegExp(`class\\s+${converterName}\\b[^;{]*`).exec(source)?.[0] ?? "")
    : "";
  return {
    members,
    asString: /JsonStringEnumConverter/.test(converterName + ownConverter),
    camelCase: /CamelCase/.test(converter?.[0] ?? "") || /CamelCase/.test(ownConverter),
  };
}

function kindOf(type: string, enums: ReadonlyMap<string, CsEnum>): JsonKind {
  const bare = type.replace(/\?$/, "").trim();
  if (/^(string|Guid|DateTime|DateTimeOffset|Uri|TimeSpan)$/.test(bare)) return "string";
  if (/^(bool)$/.test(bare)) return "boolean";
  if (/^(int|long|short|byte|uint|ulong|double|float|decimal)$/.test(bare)) return "number";
  if (
    /\[\]$/.test(bare) ||
    /^(List|IList|IEnumerable|ICollection|IReadOnlyList|IReadOnlyCollection|HashSet|IAsyncEnumerable)</.test(
      bare,
    )
  ) {
    return "array";
  }
  if (/^(Dictionary|IDictionary|IReadOnlyDictionary)</.test(bare)) return "object";
  const known = enums.get(bare);
  if (known) {
    return known.asString ? "string" : "number";
  }
  if (/^(T|object|JsonElement|JsonNode|ApiDateTime)$/.test(bare)) return "any";
  return "object";
}

/**
 * The JSON properties of a class or record of `file`, its own and those of the base classes
 * named in `bases` (file by class name). `enums` are the enum types its properties may have,
 * read with {@link readEnum}; a type that is neither known nor an enum counts as an object.
 */
export function readClass(
  file: string,
  name: string,
  options: { enums?: ReadonlyMap<string, CsEnum>; bases?: Readonly<Record<string, string>> } = {},
): CsProperty[] {
  const source = read(file);
  const found = declaration(source, "(?:class|record)", name);
  if (!found) {
    throw new Error(`${file} declares no class ${name}`);
  }
  const enums = options.enums ?? new Map<string, CsEnum>();
  const properties: CsProperty[] = [];
  const body = block(source, found.index);
  const property =
    /(?:\[JsonPropertyName\("(\w+)"\)\]\s*)?public\s+(?:required\s+|virtual\s+|override\s+|new\s+)*([\w<>[\]?., ]+?)\s+(\w+)\s*\{\s*get/g;
  for (let m = property.exec(body); m; m = property.exec(body)) {
    const type = (m[2] ?? "").trim();
    properties.push({
      name: m[1] ?? camel(m[3] ?? ""),
      type,
      kind: kindOf(type, enums),
      nullable: type.endsWith("?"),
    });
  }
  const inherits = /:\s*([\w<>, .]+)/.exec(found[1] ?? "")?.[1] ?? "";
  for (const base of inherits.split(",").map((b) => b.trim().replace(/<.*$/, ""))) {
    const baseFile = options.bases?.[base];
    if (baseFile) {
      properties.push(...readClass(baseFile, base, options));
    }
  }
  return properties;
}

/** How Node reads a field: the getter of `app/narrow.ts`, or `index` for `value["field"]`. */
export type NodeRead = "string" | "number" | "boolean" | "array" | "object" | "id" | "index";

/**
 * The fields a function of the module reads from the object it is given: every
 * `getString(x, "field")` and friends, and every `x["field"]`, in the body of `functionName`.
 */
export function readsOf(file: string, functionName: string): Map<string, NodeRead> {
  const source = fs.readFileSync(path.join(MODULE_ROOT, file), "utf8");
  // The declaration first: a call of the function may stand above it in the file.
  const start =
    new RegExp(`\\bfunction\\s+${functionName}\\s*\\(`).exec(source) ??
    new RegExp(`^\\s+(?:async\\s+)?${functionName}\\s*\\([^)]*\\)[^;]*\\{`, "m").exec(source);
  if (!start) {
    throw new Error(`${file} has no function ${functionName}`);
  }
  const body = block(source, source.indexOf(")", start.index));
  const reads = new Map<string, NodeRead>();
  const kinds: Record<string, NodeRead> = {
    String: "string",
    Number: "number",
    Boolean: "boolean",
    Array: "array",
    ObjectArray: "array",
    Object: "object",
    EntityId: "id",
  };
  const getter =
    /\bget(String|Number|Boolean|ObjectArray|Array|Object|EntityId)\(\s*[\w.]+\s*,\s*"(\w+)"\s*\)/g;
  for (let m = getter.exec(body); m; m = getter.exec(body)) {
    reads.set(m[2] ?? "", kinds[m[1] ?? ""] ?? "index");
  }
  // A read only: `out["field"] = value` writes the function's own result.
  const index = /\b\w+\[\s*"(\w+)"\s*\](?!\s*=[^=])/g;
  for (let m = index.exec(body); m; m = index.exec(body)) {
    if (!reads.has(m[1] ?? "")) {
      reads.set(m[1] ?? "", "index");
    }
  }
  return reads;
}

/** Whether a value that C# sends as `kind` is one the Node read `how` accepts. */
export function accepts(how: NodeRead, kind: JsonKind): boolean {
  if (how === "index" || kind === "any") return true;
  if (how === "id") return kind === "string" || kind === "number";
  return how === kind;
}

/** The value of a numeric `const NAME = 12;` of a file of the module. */
export function constantOf(file: string, name: string): number {
  const source = fs.readFileSync(path.join(MODULE_ROOT, file), "utf8");
  const found = new RegExp(`\\bconst\\s+${name}\\s*=\\s*(\\d+)\\s*;`).exec(source);
  if (!found?.[1]) {
    throw new Error(`${file} has no numeric constant ${name}`);
  }
  return Number(found[1]);
}
