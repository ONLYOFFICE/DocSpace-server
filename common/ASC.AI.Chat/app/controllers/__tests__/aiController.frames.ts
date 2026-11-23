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

// Reading a stream answer frame by frame, and checking a frame against a schema of the published
// OpenAPI document.

import { Ajv2020 } from "ajv/dist/2020.js";
import type { ValidateFunction } from "ajv/dist/2020.js";
import { openApiDocument } from "../../__tests__/helpers/schema.js";

/** The frames of an NDJSON answer, blank keep-alive lines left out. */
export function ndjsonFrames(text: unknown): unknown[] {
  return String(text)
    .split("\n")
    .filter((line) => line.length > 0)
    .map((line) => JSON.parse(line) as unknown);
}

/** The `data:` payloads of an SSE answer, `[DONE]` kept as the string. */
export function sseFrames(text: unknown): unknown[] {
  return String(text)
    .split("\n\n")
    .filter((frame) => frame.startsWith("data: "))
    .map((frame) => frame.slice("data: ".length))
    .map((data) => (data === "[DONE]" ? data : (JSON.parse(data) as unknown)));
}

const ajv = new Ajv2020({ strict: false, allErrors: true });
const validators = new Map<string, ValidateFunction>();

/**
 * How `value` differs from `#/components/schemas/<name>` of the published document. The stream
 * answers are published as `application/x-ndjson` / `text/event-stream`, which the shared
 * `responseViolations` does not read, so a frame is checked against its schema here.
 */
export function schemaViolations(name: string, value: unknown): string[] {
  let validate = validators.get(name);
  if (!validate) {
    validate = ajv.compile({
      $id: `urn:aichat-frames:${name}`,
      components: openApiDocument["components"],
      $ref: `#/components/schemas/${name}`,
    });
    validators.set(name, validate);
  }
  return validate(value)
    ? []
    : (validate.errors ?? []).map((e) => `${e.instancePath || "/"} ${e.message ?? e.keyword}`);
}
