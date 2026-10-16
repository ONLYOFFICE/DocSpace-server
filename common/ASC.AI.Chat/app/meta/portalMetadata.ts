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

/**
 * The Files metadata API under the caller's credentials: read an entry's
 * metadata and write field values and custom fields, the same calls the
 * client's info panel makes. Shapes mirror `MetadataController` DTOs.
 */

import { withTimeout, proxyBaseUrl } from "../storage/httpClient.js";
import { DocspaceApiHttpError } from "../storage/docspaceFilesApi.js";
import { getForwardedHeaders } from "../requestContext.js";
import { getNumber, getObject, getObjectArray, getString, isObject } from "../narrow.js";

export type EntryKind = "file" | "folder";

/** `MetadataFieldType` on the portal. */
export const FieldType = {
  String: 0,
  Date: 1,
  Number: 2,
  SingleChoice: 3,
  MultiChoice: 4,
} as const;
export type FieldType = (typeof FieldType)[keyof typeof FieldType];

export interface FieldOption {
  id: string;
  value: string;
}

export interface FieldValue {
  stringValue?: string;
  numberValue?: number;
  dateValue?: string;
  optionIds?: string[];
}

export interface EntryField {
  id: number;
  name: string;
  type: FieldType;
  options: FieldOption[];
  value?: FieldValue;
}

export interface EntryTemplate {
  id: number;
  name: string;
  fields: EntryField[];
}

export interface CustomField {
  name: string;
  value: string;
}

export interface EntryMetadata {
  templates: EntryTemplate[];
  customFields: CustomField[];
}

/** One value to write: the field id and exactly one member of its type, or none to clear. */
export type ValueRequest = { fieldId: number } & FieldValue;

export function hasValue(value: FieldValue | undefined): boolean {
  if (!value) {
    return false;
  }
  return (
    (value.stringValue !== undefined && value.stringValue !== "") ||
    (value.numberValue !== undefined && value.numberValue !== null) ||
    (value.dateValue !== undefined && value.dateValue !== "") ||
    (value.optionIds !== undefined && value.optionIds.length > 0)
  );
}

function metadataUrl(kind: EntryKind, id: string, suffix = ""): string {
  return `${proxyBaseUrl}/api/2.0/files/metadata/${kind}/${encodeURIComponent(id)}${suffix}`;
}

async function call(url: string, init?: RequestInit): Promise<unknown> {
  const { signal, cancel } = withTimeout(undefined);
  try {
    const res = await fetch(url, {
      ...init,
      headers: {
        ...getForwardedHeaders(),
        ...(init?.body ? { "content-type": "application/json" } : {}),
        ...(init?.headers ?? {}),
      },
      signal,
    });
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    const raw: unknown = await res.json();
    return isObject(raw) ? raw["response"] : undefined;
  } finally {
    cancel();
  }
}

function toValue(raw: Record<string, unknown> | undefined): FieldValue | undefined {
  if (!raw) {
    return undefined;
  }
  const value: FieldValue = {};
  const stringValue = getString(raw, "stringValue");
  if (stringValue !== undefined) {
    value.stringValue = stringValue;
  }
  const numberValue = getNumber(raw, "numberValue");
  if (numberValue !== undefined) {
    value.numberValue = numberValue;
  }
  const dateValue = getString(raw, "dateValue");
  if (dateValue !== undefined) {
    value.dateValue = dateValue;
  }
  const optionIds = raw["optionIds"];
  if (Array.isArray(optionIds)) {
    value.optionIds = optionIds.filter((o): o is string => typeof o === "string");
  }
  return value;
}

const FIELD_TYPE_NAMES: Record<string, FieldType> = {
  string: FieldType.String,
  date: FieldType.Date,
  number: FieldType.Number,
  singlechoice: FieldType.SingleChoice,
  multichoice: FieldType.MultiChoice,
};

// The portal serializes the enum as a number; its name is accepted too in
// case a serializer option ever flips it.
function toFieldType(raw: unknown): FieldType | undefined {
  if (typeof raw === "number") {
    return (Object.values(FieldType) as number[]).includes(raw) ? (raw as FieldType) : undefined;
  }
  if (typeof raw === "string") {
    return FIELD_TYPE_NAMES[raw.toLowerCase()];
  }
  return undefined;
}

function toField(raw: Record<string, unknown>): EntryField | undefined {
  const id = getNumber(raw, "id");
  const name = getString(raw, "name");
  const type = toFieldType(raw["type"]);
  if (id === undefined || !name || type === undefined) {
    return undefined;
  }
  const options: FieldOption[] = [];
  for (const option of getObjectArray(raw, "options") ?? []) {
    const optionId = getString(option, "id");
    const value = getString(option, "value");
    if (optionId && value !== undefined) {
      options.push({ id: optionId, value });
    }
  }
  const field: EntryField = { id, name, type, options };
  const value = toValue(getObject(raw, "value"));
  if (value) {
    field.value = value;
  }
  return field;
}

export function parseEntryMetadata(raw: unknown): EntryMetadata {
  const templates: EntryTemplate[] = [];
  const customFields: CustomField[] = [];
  if (!isObject(raw)) {
    return { templates, customFields };
  }
  for (const template of getObjectArray(raw, "templates") ?? []) {
    const id = getNumber(template, "id");
    if (id === undefined) {
      continue;
    }
    const fields: EntryField[] = [];
    for (const field of getObjectArray(template, "fields") ?? []) {
      const parsed = toField(field);
      if (parsed) {
        fields.push(parsed);
      }
    }
    templates.push({ id, name: getString(template, "name") ?? "", fields });
  }
  for (const field of getObjectArray(raw, "customFields") ?? []) {
    const name = getString(field, "name");
    if (name) {
      customFields.push({ name, value: getString(field, "value") ?? "" });
    }
  }
  return { templates, customFields };
}

/** `GET metadata/{kind}/{id}`: the entry's templates with values and its custom fields. */
export async function getEntryMetadata(kind: EntryKind, id: string): Promise<EntryMetadata> {
  return parseEntryMetadata(await call(metadataUrl(kind, id)));
}

/** `PUT metadata/{kind}/{id}/values`; answers the whole metadata after the write. */
export async function setValues(
  kind: EntryKind,
  id: string,
  values: ValueRequest[],
): Promise<EntryMetadata> {
  return parseEntryMetadata(
    await call(metadataUrl(kind, id, "/values"), {
      method: "PUT",
      body: JSON.stringify({ values }),
    }),
  );
}

/** `PUT metadata/{kind}/{id}/customFields`; a null value removes the field. */
export async function setCustomFields(
  kind: EntryKind,
  id: string,
  fields: { name: string; value: string | null }[],
): Promise<void> {
  await call(metadataUrl(kind, id, "/customFields"), {
    method: "PUT",
    body: JSON.stringify({ fields }),
  });
}
