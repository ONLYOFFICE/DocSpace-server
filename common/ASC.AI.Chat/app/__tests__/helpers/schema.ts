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

import { createRequire } from "node:module";
import { Ajv2020 } from "ajv/dist/2020.js";
import type { ErrorObject, ValidateFunction } from "ajv/dist/2020.js";
import addFormatsModule from "ajv-formats";
import { API_PREFIX, CUSTOM_ROUTE_DOCS, CUSTOM_TAG_DOCS, ENGINE_DOCS } from "../../apiCatalog.js";
import { buildOpenApiDocument } from "../../openapi.js";
import type { OpenApiSchemaBundle } from "../../openapi.js";

type Json = Record<string, unknown>;

export interface PublishedOperation {
  operationId: string;
  method: string;
  /** The full path as published, `/api/2.0/ai/...`. */
  path: string;
  operation: Json;
}

const schemas = createRequire(import.meta.url)(
  "../../generated/openapi-schemas.json",
) as OpenApiSchemaBundle;

/** The OpenAPI document exactly as `GET /api/2.0/ai/openapi.json` serves it. */
export const openApiDocument = buildOpenApiDocument({
  apiPrefix: API_PREFIX,
  engines: ENGINE_DOCS,
  customRoutes: CUSTOM_ROUTE_DOCS,
  customTagDescriptions: CUSTOM_TAG_DOCS,
  schemas,
}) as unknown as Json;

const operations = new Map<string, PublishedOperation>();
for (const [path, item] of Object.entries(openApiDocument["paths"] as Record<string, Json>)) {
  for (const [method, operation] of Object.entries(item)) {
    const id = (operation as Json | null)?.["operationId"];
    if (typeof id === "string") {
      operations.set(id, { operationId: id, method, path, operation: operation as Json });
    }
  }
}

/** Every operation the service publishes, by operationId. */
export function publishedOperations(): PublishedOperation[] {
  return [...operations.values()];
}

export function publishedOperation(operationId: string): PublishedOperation {
  const found = operations.get(operationId);
  if (!found) {
    throw new Error(`the OpenAPI document has no operation "${operationId}"`);
  }
  return found;
}

const addFormats = addFormatsModule as unknown as (ajv: Ajv2020) => void;
const ajv = new Ajv2020({ strict: false, allErrors: true });
addFormats(ajv);
const compiled = new Map<string, ValidateFunction>();

function validatorFor(key: string, schema: unknown): ValidateFunction {
  let validate = compiled.get(key);
  if (!validate) {
    // `#/components/schemas/X` resolves against the root of what is compiled.
    validate = ajv.compile({
      $id: `urn:aichat:${key}`,
      components: openApiDocument["components"],
      allOf: [schema],
    });
    compiled.set(key, validate);
  }
  return validate;
}

function jsonSchemaOf(holder: unknown): unknown {
  const content = (holder as Json | undefined)?.["content"] as Json | undefined;
  return (content?.["application/json"] as Json | undefined)?.["schema"];
}

/** The published JSON schema of the answer with this status, undefined when none is published. */
export function responseSchemaOf(operationId: string, status: number): unknown {
  const responses = publishedOperation(operationId).operation["responses"] as Json | undefined;
  return jsonSchemaOf(responses?.[String(status)]);
}

/** The published JSON schema of the request body, undefined when none is published. */
export function requestSchemaOf(operationId: string): unknown {
  return jsonSchemaOf(publishedOperation(operationId).operation["requestBody"]);
}

function describe(errors: ErrorObject[] | null | undefined): string[] {
  return (errors ?? []).map((e) => `${e.instancePath || "/"} ${e.message ?? e.keyword}`);
}

/**
 * How `body` differs from the schema the service publishes for this answer: an empty list when
 * it matches. A status the document does not list, or lists without a JSON schema, is itself a
 * difference, so that an undocumented answer cannot pass by having nothing to compare with.
 */
export function responseViolations(operationId: string, status: number, body: unknown): string[] {
  const schema = responseSchemaOf(operationId, status);
  if (schema === undefined) {
    return [`${operationId} publishes no JSON schema for status ${status}`];
  }
  const validate = validatorFor(`${operationId}:${status}`, schema);
  return validate(body) ? [] : describe(validate.errors);
}

/** How a request body differs from the published request schema of the operation. */
export function requestViolations(operationId: string, body: unknown): string[] {
  const schema = requestSchemaOf(operationId);
  if (schema === undefined) {
    return [`${operationId} publishes no JSON request schema`];
  }
  const validate = validatorFor(`${operationId}:request`, schema);
  return validate(body) ? [] : describe(validate.errors);
}
