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

import { loadProvider, registerProvider } from "@onlyoffice/ai-chat/providers";
import type { AbstractBaseProvider, ProviderConstructor } from "@onlyoffice/ai-chat/providers";
import { getSourceMeta } from "../requestContext.js";
import type { SourceMeta } from "../storage/docspaceFilesApi.js";
import logger from "../log.js";

// The ONLYOFFICE provider in `@onlyoffice/ai-chat` describes the round's host
// entity on every request as `metadata: { agent_id, agent_title }`, built in
// its `extraBody()` from `ProviderCredentials.entityId` / `entityTitle`. The
// billing backend now classifies the source instead — `source_id`,
// `source_type`, `source_title` — and the type has no place in the library's
// credentials: its provider factory copies exactly `entityId` and
// `entityTitle` out of `actionArgs`, nothing else survives.
//
// So the host takes the field over. The library's registry lets a custom
// constructor shadow a built-in type (`loadProvider` consults the custom map
// first), and the built-in class is reachable through `loadProvider` itself,
// so a subclass overriding `extraBody()` is registered under the same
// `onlyoffice` type at startup. The override reads the source resolved by
// `primeSourceMeta` from the request context — AsyncLocalStorage follows the
// round through every await, tool-call resume round included — and ignores
// the credentials pair entirely. Everything else (the `x-session-id` header,
// the prompt-cache breakpoints, the model list) is inherited unchanged.

const ONLYOFFICE_PROVIDER_TYPE = "onlyoffice";

/** Wire keys the billing backend reads from the request `metadata`. */
const METADATA_KEYS = {
  id: "source_id",
  type: "source_type",
  title: "source_title",
} as const;

/**
 * The `metadata` object for a resolved source, or `undefined` when there is
 * none — the whole object is dropped rather than sent with empty values.
 * Shared with the OpenAI passthrough, which splices the same object into the
 * plugin's raw request body.
 */
export function sourceMetadata(source: SourceMeta | undefined): Record<string, string> | undefined {
  if (!source?.id) {
    return undefined;
  }
  const metadata: Record<string, string> = {
    [METADATA_KEYS.id]: source.id,
    [METADATA_KEYS.type]: source.type,
  };
  if (source.title) {
    metadata[METADATA_KEYS.title] = source.title;
  }
  return metadata;
}

// The base class as the subclass needs to see it: `extraBody` is a protected
// member of the library's OpenAI provider family and is not part of the
// public `AbstractBaseProvider` type. A single construct signature (the
// registry's `ProviderConstructor` has its own, with a different instance
// type) plus the static side the registry calls.
type ProviderCredentials = ConstructorParameters<ProviderConstructor>[0];
// The mapped type strips the `abstract` modifiers of `AbstractBaseProvider`:
// the runtime base is the concrete library class, so nothing is left for the
// subclass to implement.
type ProviderWithExtraBody = { [K in keyof AbstractBaseProvider]: AbstractBaseProvider[K] } & {
  extraBody(): Record<string, unknown>;
};
type ProviderWithExtraBodyConstructor = {
  new (creds: ProviderCredentials): ProviderWithExtraBody;
} & Pick<ProviderConstructor, "checkProvider" | "getProviderModels" | "getName" | "getBaseUrl">;

/**
 * Shadow the built-in ONLYOFFICE provider with one whose request `metadata`
 * describes the request-context source. Must run once at startup, before
 * the first chat round. Throws when the library no longer exposes the hook
 * the override relies on: a silent fallback would send no metadata at all,
 * and the backend's usage accounting would quietly lose every source.
 */
export async function registerOnlyofficeSourceProvider(): Promise<void> {
  const Base = (await loadProvider(ONLYOFFICE_PROVIDER_TYPE)) as
    ProviderWithExtraBodyConstructor | undefined;
  if (!Base) {
    throw new Error(
      `@onlyoffice/ai-chat has no "${ONLYOFFICE_PROVIDER_TYPE}" provider to override`,
    );
  }
  const proto = Base.prototype as Partial<ProviderWithExtraBody>;
  if (typeof proto.extraBody !== "function") {
    throw new Error(
      `@onlyoffice/ai-chat "${ONLYOFFICE_PROVIDER_TYPE}" provider has no extraBody() hook — ` +
        "the source metadata override cannot attach; check the library upgrade",
    );
  }

  class OnlyofficeSourceProvider extends Base {
    override extraBody(): Record<string, unknown> {
      const metadata = sourceMetadata(getSourceMeta());
      return metadata ? { metadata } : {};
    }
  }

  // `keyof` sees no protected members, so the mapped instance type above lacks
  // `creds`; at runtime the subclass is the library class plus one override.
  registerProvider(
    ONLYOFFICE_PROVIDER_TYPE,
    OnlyofficeSourceProvider as unknown as ProviderConstructor,
  );
  logger.info(`Registered the ${ONLYOFFICE_PROVIDER_TYPE} provider override (source metadata)`);
}
