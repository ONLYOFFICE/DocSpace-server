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

import type { PreferencesEngine, Profile } from "@onlyoffice/ai-chat/core";

// The library's provider-neutral extended-thinking scale, and the C# side's
// `ReasoningDepth` enum, are two spellings of one thing. The C# storage
// (`ASC.AI.Integration.Profiles.ReasoningDepth`) serialises camelCase
// strings — `none | low | medium | high | xhigh | max` — while the widget and
// the engines speak `off | low | medium | high | max`. Every storage adapter
// that carries a depth across the boundary translates through this module so
// the two never drift: `none` is `off`, `xhigh` folds into `max` on the way
// in (the library has no such level and never asks for it), and nothing
// outside the C# enum is ever sent.
//
// The library exports the level helpers (`isReasoningLevel`, `REASONING_LEVELS`,
// …) only from its root entry, which pulls React; `/core` exposes the types
// through the engine and storage contracts alone, so they are derived here.

/** `off | low | medium | high | max` — the widget's extended-thinking depth. */
export type ReasoningLevel = Awaited<ReturnType<PreferencesEngine["getReasoningLevel"]>>;

/** What one model can do with extended thinking; persisted on the profile. */
export type ReasoningSupport = NonNullable<Profile["reasoningSupport"]>;

/** A level above `off`. */
export type ReasoningDepthLevel = Exclude<ReasoningLevel, "off">;

/** Every level, lowest first. */
export const REASONING_LEVELS: readonly ReasoningLevel[] = ["off", "low", "medium", "high", "max"];

/** Every depth above `off`, lowest first. */
export const REASONING_DEPTH_LEVELS: readonly ReasoningDepthLevel[] = [
  "low",
  "medium",
  "high",
  "max",
];

/** What the legacy boolean toggle stands for when on (`DEFAULT_PREFERENCES.reasoningLevel`). */
export const DEFAULT_REASONING_LEVEL: ReasoningDepthLevel = "medium";

/** The C# `ReasoningDepth` enum as it travels over the wire. */
export type CsharpReasoningDepth = "none" | "low" | "medium" | "high" | "xhigh" | "max";

export function isReasoningLevel(value: unknown): value is ReasoningLevel {
  return typeof value === "string" && (REASONING_LEVELS as readonly string[]).includes(value);
}

/**
 * A C# depth (or anything that came back where one was expected) as a
 * library level. `null` for a missing / unknown value so callers can tell
 * "nothing stored" from `off`. Case-insensitive: the enum converter on the
 * C# side accepts either spelling, and so does this reader.
 */
export function depthToLevel(raw: unknown): ReasoningLevel | null {
  if (typeof raw !== "string") {
    return null;
  }
  switch (raw.toLowerCase()) {
    case "none":
      return "off";
    case "low":
      return "low";
    case "medium":
      return "medium";
    case "high":
      return "high";
    case "xhigh":
    case "max":
      return "max";
    default:
      return null;
  }
}

/**
 * The C# `ReasoningDepth` member a string names, the way the C# side parses
 * catalogue efforts (`ReasoningDepthExtensions.TryParse(value, ignoreCase:
 * true)`): the enum's names, any casing, nothing else — `minimal` and any
 * other unknown effort come back as `undefined`.
 */
export function parseCsharpReasoningDepth(value: unknown): CsharpReasoningDepth | undefined {
  if (typeof value !== "string") {
    return undefined;
  }
  switch (value.toLowerCase()) {
    case "none":
    case "low":
    case "medium":
    case "high":
    case "xhigh":
    case "max":
      return value.toLowerCase() as CsharpReasoningDepth;
    default:
      return undefined;
  }
}

/** A library level as the C# depth the storage accepts. */
export function levelToDepth(level: ReasoningLevel): CsharpReasoningDepth {
  return level === "off" ? "none" : level;
}

/**
 * The depths a C# `ReasoningConfig.depths` array names, as library depths:
 * `none` dropped (it is the off switch, not a depth), `xhigh` folded into
 * `max`, unknown strings ignored, duplicates removed, lowest first.
 */
export function depthsToLevels(raw: unknown): ReasoningDepthLevel[] {
  if (!Array.isArray(raw)) {
    return [];
  }
  const found = new Set<ReasoningDepthLevel>();
  for (const item of raw) {
    const level = depthToLevel(item);
    if (level !== null && level !== "off") {
      found.add(level);
    }
  }
  return REASONING_DEPTH_LEVELS.filter((level) => found.has(level));
}

/** `ReasoningSupport` for a model that thinks at every depth and can stop. */
export const FULL_REASONING_SUPPORT: ReasoningSupport = {
  thinks: true,
  canDisable: true,
  depths: REASONING_DEPTH_LEVELS,
};

/** `ReasoningSupport` for a model with no extended thinking at all (the library's `NO_REASONING_SUPPORT`). */
export const NO_REASONING_SUPPORT: ReasoningSupport = {
  thinks: false,
  canDisable: false,
  depths: [],
};

/**
 * The C# `ReasoningConfig` object as the library's `ReasoningSupport`, or
 * `undefined` when the payload is not one. A bare boolean — the shape the
 * column had before the depth migration — is accepted too and read as the
 * full / no support it used to mean.
 */
export function reasoningConfigToSupport(raw: unknown): ReasoningSupport | undefined {
  if (typeof raw === "boolean") {
    return raw ? FULL_REASONING_SUPPORT : NO_REASONING_SUPPORT;
  }
  if (typeof raw !== "object" || raw === null || Array.isArray(raw)) {
    return undefined;
  }
  const config = raw as Record<string, unknown>;
  const thinks = config["thinks"];
  const canDisable = config["canDisable"];
  if (typeof thinks !== "boolean" || typeof canDisable !== "boolean") {
    return undefined;
  }
  const support: ReasoningSupport = {
    thinks,
    canDisable,
    depths: depthsToLevels(config["depths"]),
  };
  const defaultDepth = depthToLevel(config["defaultDepth"]);
  if (defaultDepth !== null && defaultDepth !== "off") {
    support.defaultDepth = defaultDepth;
  }
  return support;
}

/**
 * The library's `ReasoningSupport` as the C# `ReasoningConfig` the profile
 * storage persists. `defaultDepth` is sent only when known; the C# side
 * keeps `null` for "unknown" and `none` for "does not think by default".
 */
export function supportToReasoningConfig(support: ReasoningSupport): Record<string, unknown> {
  return {
    thinks: support.thinks,
    canDisable: support.canDisable,
    depths: support.depths.map(levelToDepth),
    defaultDepth: support.defaultDepth ? levelToDepth(support.defaultDepth) : null,
  };
}
