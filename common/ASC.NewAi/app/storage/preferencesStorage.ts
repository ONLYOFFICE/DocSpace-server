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

import { aiService, AiServiceHttpError, type QueryValue } from "./httpClient.js";
import { resolveAgentEntityId } from "./docspaceFilesApi.js";
import { isObject } from "../narrow.js";
import type { PreferencesStorage } from "@onlyoffice/ai-chat/core";
import {
  chatContextScope,
  invalidateChatContext,
  readChatContext,
  reportChatContextMiss,
} from "./chatContextSnapshot.js";
import {
  DEFAULT_REASONING_LEVEL,
  depthToLevel,
  levelToDepth,
  type ReasoningLevel,
} from "./reasoningDepth.js";

const PATH = "/preferences";

function entityIdQuery(entityId: string | undefined): Record<string, QueryValue> | undefined {
  return entityId ? { entityId } : undefined;
}

// Writes and reads must agree on the scope key. Reads fold a non-agent
// entityId to the global scope (`resolveAgentEntityId` → undefined), so a
// write keyed by a raw room/folder id would store a row no read ever sees —
// the deep-mode toggle flipped from an ordinary room appeared saved but
// read back as the portal value, and a folder id 403'd in the C# PUT
// (Bug 82900). Scope resolution here mirrors threads and tool prefs: the
// scope ladder is global-or-agent by design (Bug 82719).
async function scopedEntityId(
  entityId: string | undefined,
): Promise<string | null> {
  return (await resolveAgentEntityId(entityId)) ?? null;
}

// The C# storage keeps ONE value per scope: `depth`, its `ReasoningDepth`
// enum (`none | low | medium | high | xhigh | max`, see `reasoningDepth.ts`).
// The library's two preferences are both views of it:
//
// - the reasoning level IS the depth (`none` ↔ `off`);
// - deep mode is "the depth is above `none`". Writing `false` stores `none`;
//   writing `true` keeps whatever depth is stored and only falls back to the
//   default depth when nothing (or `none`) is there — the engine's
//   `setReasoningLevel` writes the toggle first and the depth right after,
//   so the fallback is overwritten within the same call, and a bare toggle
//   (an older client, the widget's fallback path) lands on `medium`, which
//   is exactly what the legacy boolean has always meant.
//
// `null` from a read means "nothing persisted in scope" for both views.
export class HttpPreferencesStorage implements PreferencesStorage {
  // -- reasoning level -----------------------------------------------------

  async createReasoningLevel(value: ReasoningLevel, entityId?: string): Promise<void> {
    await this.writeDepth(value, entityId);
  }

  async readReasoningLevel(entityId?: string): Promise<ReasoningLevel | null> {
    const snapshot = readChatContext("preferences");
    const scope = snapshot ? chatContextScope(snapshot, entityId) : undefined;
    if (scope) {
      return scope.reasoningLevel;
    }
    reportChatContextMiss(`preferences.readReasoningLevel(${entityId ?? "-"})`);
    try {
      const query = entityIdQuery(await resolveAgentEntityId(entityId));
      const raw = await aiService.get(PATH, query ? { query } : undefined);
      if (!isObject(raw)) {
        return null;
      }
      return depthToLevel(raw["depth"]);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async updateReasoningLevel(value: ReasoningLevel, entityId?: string): Promise<void> {
    await this.writeDepth(value, entityId);
  }

  async upsertReasoningLevel(value: ReasoningLevel, entityId?: string): Promise<void> {
    await this.writeDepth(value, entityId);
  }

  async deleteReasoningLevel(entityId?: string): Promise<void> {
    await this.deleteScope(entityId);
  }

  // -- deep mode (derived) -------------------------------------------------

  async createDeepMode(value: boolean, entityId?: string): Promise<void> {
    await this.writeDeepMode(value, entityId);
  }

  async readDeepMode(entityId?: string): Promise<boolean | null> {
    const level = await this.readReasoningLevel(entityId);
    return level === null ? null : level !== "off";
  }

  async updateDeepMode(value: boolean, entityId?: string): Promise<void> {
    await this.writeDeepMode(value, entityId);
  }

  async upsertDeepMode(value: boolean, entityId?: string): Promise<void> {
    await this.writeDeepMode(value, entityId);
  }

  async deleteDeepMode(entityId?: string): Promise<void> {
    await this.deleteScope(entityId);
  }

  // -- shared --------------------------------------------------------------

  private async writeDeepMode(value: boolean, entityId?: string): Promise<void> {
    if (!value) {
      await this.writeDepth("off", entityId);
      return;
    }
    const current = await this.readReasoningLevel(entityId);
    if (current !== null && current !== "off") {
      return;
    }
    await this.writeDepth(DEFAULT_REASONING_LEVEL, entityId);
  }

  private async writeDepth(level: ReasoningLevel, entityId?: string): Promise<void> {
    await aiService.put(PATH, {
      depth: levelToDepth(level),
      entityId: await scopedEntityId(entityId),
    });
    invalidateChatContext("preferences");
  }

  private async deleteScope(entityId?: string): Promise<void> {
    invalidateChatContext("preferences");
    try {
      const query = entityIdQuery(await resolveAgentEntityId(entityId));
      await aiService.delete(PATH, query ? { query } : undefined);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }
}
