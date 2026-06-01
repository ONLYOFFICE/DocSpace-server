// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

import { randomUUID } from "crypto";
import { aiService } from "../storage/httpClient.js";
import { getArray, getString, isObject, parseInt10 } from "../narrow.js";
import logger from "../log.js";
import type { ToolsAdapter, TMCPItem } from "@onlyoffice/ai-chat/core";

const LIST_PATH = "/integration/tools/list";
const CALL_PATH = "/integration/tools/call";

// Group key for the disabled / allow-always filters in `storage.toolPrefs`.
// DocSpace tools are a single logical source, so they share one serverType.
const SERVER_TYPE = "docspace";

// The engine calls `getTools` and the controller pre-fetches the system
// prompt (see `getPrompt`) within the same stream. Cache the single
// `tools/list` round-trip per scope for a short window so both reuse it.
const CACHE_TTL_MS = 10_000;

type ToolsList = {
  tools: TMCPItem[];
  prompt: string;
};

type CacheEntry = ToolsList & { expires: number };

// `ToolContext` on the C# side — `{ agentId, formId }`. `entityId` is the
// opaque widget scope token; for room-bound chat it carries the room id.
type ToolContextDto = {
  agentId: number;
  formId: number;
};

function toContext(entityId: string | undefined): ToolContextDto {
  return {
    agentId: parseInt10(entityId, 0) ?? 0,
    formId: 0,
  };
}

// `ToolDescriptor` on the C# side — `{ name, description, parameters }`,
// where `parameters` is the JSON Schema. Falls back to the schema's own
// `description` for older servers that didn't expose the dedicated field.
function parseTool(raw: unknown): TMCPItem | null {
  if (!isObject(raw)) {
    return null;
  }
  const name = getString(raw, "name") ?? getString(raw, "Name");
  if (!name) {
    return null;
  }
  const schema = raw["parameters"] ?? raw["Parameters"];
  const inputSchema = isObject(schema) ? schema : {};
  const description =
    getString(raw, "description") ??
    getString(raw, "Description") ??
    getString(inputSchema, "description") ??
    "";
  return { name, description, inputSchema, enabled: true };
}

function parseList(raw: unknown): ToolsList {
  if (!isObject(raw)) {
    return { tools: [], prompt: "" };
  }
  const rawTools = getArray(raw, "tools") ?? getArray(raw, "Tools") ?? [];
  const tools: TMCPItem[] = [];
  for (const item of rawTools) {
    const tool = parseTool(item);
    if (tool) {
      tools.push(tool);
    }
  }
  const prompt = getString(raw, "prompt") ?? getString(raw, "Prompt") ?? "";
  return { tools, prompt };
}

function cacheKey(entityId: string | undefined): string {
  return entityId ?? "";
}

/**
 * {@link ToolsAdapter} backed by the DocSpace AI integration endpoints
 * (`integration/tools/list` / `integration/tools/call`). Tools served by
 * this adapter are executed in-engine and the chat resumes automatically,
 * so no approval round-trip surfaces to the UI.
 */
export class HttpToolsAdapter implements ToolsAdapter {
  private readonly cache = new Map<string, CacheEntry>();

  async getTools(
    entityId?: string,
    _config?: { attachmentId: string[] },
  ): Promise<Record<string, TMCPItem[]>> {
    const { tools } = await this.list(entityId);
    return tools.length > 0 ? { [SERVER_TYPE]: tools } : {};
  }

  async callTool(
    toolName: string,
    args: Record<string, unknown>,
    entityId?: string,
  ): Promise<unknown> {
    const body = {
      ...toContext(entityId),
      calls: [{ id: randomUUID(), name: toolName, arguments: args }],
    };
    const raw = await aiService.post(CALL_PATH, body);
    const results = Array.isArray(raw) ? raw : [];
    const result = results.find((r) => isObject(r)) ?? results[0];
    if (!isObject(result)) {
      return `Tool "${toolName}" returned no result`;
    }
    const error = getString(result, "error") ?? getString(result, "Error");
    if (error) {
      // Contract: stringify failures into the result so the model can
      // react instead of aborting the stream.
      return `Tool "${toolName}" failed: ${error}`;
    }
    return "result" in result ? result["result"] : result["Result"];
  }

  /**
   * System-prompt fragment that accompanies the tool list. Consumed by
   * the controller to append to the chat's system prompt. Shares the
   * cached `tools/list` fetch with {@link getTools}.
   */
  async getPrompt(entityId?: string): Promise<string> {
    const { prompt } = await this.list(entityId);
    return prompt;
  }

  private async list(entityId: string | undefined): Promise<ToolsList> {
    const key = cacheKey(entityId);
    const cached = this.cache.get(key);
    if (cached && cached.expires > Date.now()) {
      return cached;
    }
    const raw = await aiService.post(LIST_PATH, toContext(entityId));
    const parsed = parseList(raw);
    this.cache.set(key, { ...parsed, expires: Date.now() + CACHE_TTL_MS });
    return parsed;
  }
}

/**
 * Best-effort prompt fetch for the controller. Never throws — a failed
 * fetch leaves the system prompt unchanged.
 */
export async function safeGetToolsPrompt(
  adapter: HttpToolsAdapter,
  entityId: string | undefined,
): Promise<string> {
  try {
    return await adapter.getPrompt(entityId);
  } catch (err) {
    logger.warn(
      `tools/list prompt fetch failed: ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
    return "";
  }
}
