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

import { randomUUID } from "crypto";
import { aiService } from "../storage/httpClient.js";
import { storage } from "../storage/index.js";
import { getResolvedFormId, setResolvedFormId } from "../requestContext.js";
import { getArray, getString, isObject, parseInt10 } from "../narrow.js";
import logger from "../log.js";
import type { ToolsAdapter, TMCPItem } from "@onlyoffice/ai-chat/core";

const LIST_PATH = "/tools/list";
const CALL_PATH = "/tools/call";

// Group key for the disabled / allow-always filters in `storage.toolPrefs`.
// DocSpace tools are a single logical source, so they share one serverType.
// Distinct from the MCP server name ("docspace") so Object.assign in
// composeToolsAdapters does not overwrite the 23 MCP tools with these 3.
const SERVER_TYPE = "docspace-integration";

// Tools that must surface a UI approval dialog before running. The engine
// gates approval per serverType (a serverType listed in `systemServerTypes`
// requires approval), so these tools are emitted under a dedicated
// serverType (`DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE`) which the engine
// is configured to treat as approval-required; everything else stays under
// `SERVER_TYPE` and runs in-engine without a round-trip.
export const DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE =
    "docspace-integration-approval";

const APPROVAL_TOOL_NAMES = new Set<string>([
    "docspace_generate_docx",
    "docspace_generate_presentation",
    "docspace_generate_form",
]);

type ToolsList = {
    tools: TMCPItem[];
    prompt: string;
};

// `ToolContext` on the C# side — `{ folderId, formId }`. `entityId` is the
// opaque widget scope token; for room-bound chat it carries the room id.
type ToolContextDto = {
    folderId: number;
    formId: number;
};

// A ref-carrying content part encodes `{ref, title, kind}` as JSON in
// `mimeType` (file parts) or `image` (image parts) — mirrors
// `@onlyoffice/ai-chat/core`'s internal (unexported) `extractRefIdsFromMessage`.
function parseRef(value: unknown): string | null {
    if (typeof value !== "string" || value.length === 0 || value[0] !== "{") {
        return null;
    }
    try {
        const parsed: unknown = JSON.parse(value);
        return isObject(parsed) && typeof parsed["ref"] === "string" ? parsed["ref"] : null;
    } catch {
        return null;
    }
}

/**
 * Extract attachment ref ids from a single `ThreadMessageLike`-shaped
 * message (loosely typed — only the fields read here matter). Used to give
 * `getPrompt` (called outside the engine, before `getTools` sees the
 * message) the same attachment context.
 */
export function extractAttachmentRefIds(message: unknown): string[] {
    if (!isObject(message)) {
        return [];
    }
    const content = message["content"];
    if (!Array.isArray(content)) {
        return [];
    }
    const ids: string[] = [];
    const seen = new Set<string>();
    for (const part of content) {
        if (!isObject(part)) {
            continue;
        }
        const ref =
            part["type"] === "file" ? parseRef(part["mimeType"]) :
            part["type"] === "image" ? parseRef(part["image"]) :
            null;
        if (ref && !seen.has(ref)) {
            seen.add(ref);
            ids.push(ref);
        }
    }
    return ids;
}

// Resolve an attachment ref id to the DocSpace numeric file id the C# side
// expects as `formId`. `HttpAttachmentsStorage` composes `path` as
// `${entryId}/${title}` (see `dtoToAttachment`) — the leading segment is the
// DocSpace entry id, echoed back verbatim from `/attachments`.
// Whether the file actually is a started form is validated on the C# side
// (`FormDataToolsFactory.TryInitAsync`); a non-form id resolves to an empty
// tool bundle.
async function resolveFormId(attachmentId: string[] | undefined): Promise<number> {
    if (!attachmentId || attachmentId.length === 0) {
        return 0;
    }
    try {
        const records = await storage.attachments.readManyByIds(attachmentId);
        for (const record of records) {
            const entryId = record?.path?.split("/", 1)[0];
            const numeric = parseInt10(entryId, 0) ?? 0;
            if (numeric > 0) {
                return numeric;
            }
        }
    } catch (err) {
        logger.warn(
            `resolveFormId: failed to resolve attachment(s) [${attachmentId.join(",")}]: ${
                err instanceof Error ? err.message : String(err)
            }`,
        );
    }
    return 0;
}

// Form features are scoped to the CURRENT user message only: the controller
// (`withToolsPrompt`) extracts the message's attachment refs and resolves
// them here before the stream starts; the resolved formId is kept in the
// per-request context so the engine's later `getTools` and same-turn
// `callTool` (whose signatures carry no current-message refs) see the same
// value. A form that only lives in the thread history must NOT re-activate
// the form tools, which is why the engine-supplied `config.attachmentId`
// (collected across the whole history) is deliberately ignored.
async function toContext(
    entityId: string | undefined,
    attachmentId?: string[],
): Promise<ToolContextDto> {
    const folderId = parseInt10(entityId, 0) ?? 0;
    if (attachmentId && attachmentId.length > 0) {
        const formId = await resolveFormId(attachmentId);
        if (formId > 0) {
            setResolvedFormId(formId);
        }
        return { folderId, formId };
    }
    return { folderId, formId: getResolvedFormId() ?? 0 };
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

/**
 * {@link ToolsAdapter} backed by the DocSpace AI integration endpoints
 * (`tools/list` / `tools/call`). Most tools served
 * by this adapter are executed in-engine and the chat resumes automatically
 * with no approval round-trip; the tools in `APPROVAL_TOOL_NAMES` are
 * emitted under a separate serverType so the engine surfaces an approval
 * dialog before running them.
 */
export class HttpToolsAdapter implements ToolsAdapter {
    // `config.attachmentId` (the engine's ref collection over the thread) is
    // forwarded to `list` so `toContext` can resolve the attached form.
    async getTools(
        entityId?: string,
        config?: { attachmentId: string[] },
    ): Promise<Record<string, TMCPItem[]>> {
        const { tools } = await this.list(entityId, config?.attachmentId);
        if (tools.length === 0) {
            return {};
        }
        // Split into the silent group and the approval-required group so the
        // engine shows an approval dialog only for `APPROVAL_TOOL_NAMES`.
        const silent: TMCPItem[] = [];
        const approval: TMCPItem[] = [];
        for (const tool of tools) {
            (APPROVAL_TOOL_NAMES.has(tool.name) ? approval : silent).push(tool);
        }
        const grouped: Record<string, TMCPItem[]> = {};
        if (silent.length > 0) {
            grouped[SERVER_TYPE] = silent;
        }
        if (approval.length > 0) {
            grouped[DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE] = approval;
        }
        return grouped;
    }

    async callTool(
        toolName: string,
        args: Record<string, unknown>,
        entityId?: string,
    ): Promise<unknown> {
        const body = {
            ...(await toContext(entityId)),
            calls: [{ id: randomUUID(), name: toolName, arguments: args }],
        };
        // Verbose lifecycle logging: DocSpace integration tools run silently
        // in-engine, so without this a tool that stalls the C# round-trip
        // (up to the 60s upstream timeout) is invisible — the UI just shows a
        // hanging tool card. Log start, elapsed, and the result classification
        // (no-result / failed / ok) so the stall point is pinpointable.
        const started = Date.now();
        logger.info(
            `docspaceTools.callTool name=${toolName} args=[${Object.keys(
                args,
            ).join(",")}] entityId=${entityId ?? "-"} -> ${CALL_PATH}`,
        );
        let raw: unknown;
        try {
            raw = await aiService.post(CALL_PATH, body);
        } catch (err) {
            logger.error(
                `docspaceTools.callTool name=${toolName} threw after ${
                    Date.now() - started
                }ms: ${err instanceof Error ? err.message : String(err)}`,
            );
            throw err;
        }
        const results = Array.isArray(raw) ? raw : [];
        const result = results.find((r) => isObject(r)) ?? results[0];
        if (!isObject(result)) {
            logger.warn(
                `docspaceTools.callTool name=${toolName} no-result after ${
                    Date.now() - started
                }ms`,
            );
            return `Tool "${toolName}" returned no result`;
        }
        const error = getString(result, "error") ?? getString(result, "Error");
        if (error) {
            // Contract: stringify failures into the result so the model can
            // react instead of aborting the stream.
            logger.warn(
                `docspaceTools.callTool name=${toolName} failed after ${
                    Date.now() - started
                }ms: ${error}`,
            );
            return `Tool "${toolName}" failed: ${error}`;
        }
        const value = "result" in result ? result["result"] : result["Result"];
        // Contract: a tool result must reach the engine as a string — the
        // same shape MCP tools produce. An object here would be spliced
        // verbatim into the tool message's `content` and break providers
        // that expect text (or text-part arrays).
        const text = typeof value === "string" ? value : JSON.stringify(value ?? null);
        logger.info(
            `docspaceTools.callTool name=${toolName} ok in ${
                Date.now() - started
            }ms (resultLength=${text.length})`,
        );
        return text;
    }

    /**
     * System-prompt fragment that accompanies the tool list. Consumed by
     * the controller to append to the chat's system prompt. `attachmentId`
     * lets the caller (which runs before the engine's own `getTools` call)
     * pass the current message's attachment refs so the fragment reflects
     * the form actually attached to this turn.
     */
    async getPrompt(entityId?: string, attachmentId?: string[]): Promise<string> {
        const { prompt } = await this.list(entityId, attachmentId);
        return prompt;
    }

    private async list(entityId: string | undefined, attachmentId?: string[]): Promise<ToolsList> {
        // Runs on every stream (tool list + prompt fragment) before the
        // assistant reply starts; a stalled list here delays the whole chat,
        // so time it and report the tool count.
        const started = Date.now();
        const context = await toContext(entityId, attachmentId);
        logger.info(
            `docspaceTools.list entityId=${entityId ?? "-"} -> ${LIST_PATH} context=${JSON.stringify(context)}`,
        );
        let raw: unknown;
        try {
            raw = await aiService.post(LIST_PATH, context);
        } catch (err) {
            logger.error(
                `docspaceTools.list entityId=${entityId ?? "-"} request failed after ${Date.now() - started}ms: ${err instanceof Error ? err.message : String(err)}`,
            );
            throw err;
        }
        logger.info(
            `docspaceTools.list raw response: ${JSON.stringify(raw).slice(0, 1000)}`,
        );
        const parsed = parseList(raw);
        const names = parsed.tools.map((t) => t.name).join(", ") || "<none>";
        logger.info(
            `docspaceTools.list entityId=${entityId ?? "-"} -> ${parsed.tools.length} tool(s) in ${Date.now() - started}ms: [${names}]`,
        );
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
    attachmentId?: string[],
): Promise<string> {
    try {
        return await adapter.getPrompt(entityId, attachmentId);
    } catch (err) {
        logger.warn(
            `tools/list prompt fetch failed: ${
                err instanceof Error ? err.message : String(err)
            }`,
        );
        return "";
    }
}

