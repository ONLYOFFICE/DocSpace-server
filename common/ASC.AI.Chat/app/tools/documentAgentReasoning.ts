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

import type { Profile } from "@onlyoffice/ai-chat/core";
import { getReasoningSupport } from "@onlyoffice/ai-chat/providers";

import type {
  DocumentAgentSettings as TenantSettings,
  ReasoningLevel,
} from "../storage/documentAgentSettingsStorage.js";

const DEPTHS: readonly ReasoningLevel[] = ["low", "medium", "high", "max"];

/**
 * The effort the task runs at, as the chat would resolve it for the same
 * profile: what the model can do comes from the catalogue's record saved on
 * the profile, else from the provider's table by model id (an `external`
 * profile by the provider it is based on), else from the legacy boolean. The
 * portal's pick, or the model's own default, is then snapped to the depths
 * the model distinguishes (the nearest, upwards on a tie), and a model that
 * cannot think, or that the profile keeps from thinking, runs with it off.
 */
export function reasoningFor(
  profile: Pick<Profile, "providerType" | "basedOn" | "modelId" | "reasoning" | "reasoningSupport">,
  picked: TenantSettings["reasoning"],
): ReasoningLevel {
  const support = profile.reasoningSupport ?? supportFromTable(profile);
  const thinks = support
    ? support.thinks && (!support.canDisable || profile.reasoning !== false)
    : profile.reasoning === true;
  if (!thinks) {
    return "off";
  }
  if (picked === "off") {
    return support?.canDisable === false ? clamp(support.defaultDepth ?? "medium", support) : "off";
  }
  const wanted = picked === "default" ? (support?.defaultDepth ?? "medium") : picked;
  return clamp(wanted, support);
}

function supportFromTable(
  profile: Pick<Profile, "providerType" | "basedOn" | "modelId">,
): Profile["reasoningSupport"] {
  try {
    const support = getReasoningSupport(profile.basedOn ?? profile.providerType, profile.modelId);
    return support.thinks ? support : undefined;
  } catch {
    return undefined;
  }
}

/** The nearest depth the model has, upwards on a tie; a model with no depths runs the level as asked. */
function clamp(level: ReasoningLevel, support: Profile["reasoningSupport"]): ReasoningLevel {
  const depths = support?.depths ?? [];
  if (level === "off" || depths.length === 0 || depths.includes(level as never)) {
    return level;
  }
  const wanted = DEPTHS.indexOf(level);
  let best: ReasoningLevel | undefined;
  let bestDistance = Infinity;
  for (const depth of DEPTHS) {
    if (!depths.includes(depth as never)) {
      continue;
    }
    const distance = Math.abs(DEPTHS.indexOf(depth) - wanted);
    if (distance < bestDistance || (distance === bestDistance && DEPTHS.indexOf(depth) > wanted)) {
      best = depth;
      bestDistance = distance;
    }
  }
  return best ?? level;
}
