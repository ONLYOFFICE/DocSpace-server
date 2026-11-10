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

// The tenant settings of the document agent (Portal settings → AI settings
// → Document Agent): which model and effort its tasks run on, their
// limits, whether and where their logs are kept. The AI service owns them
// (`internal/ai/document-agent/settings`, stored with the portal's other
// settings, so they travel with a backup); this service reads them for a
// task and proxies the tab's read and write. A portal that never saved
// anything gets the defaults from there.

import { z } from "zod";

import logger from "../log.js";
import { getChatContextSnapshot } from "../requestContext.js";
import { aiService } from "./httpClient.js";

export const REASONING_LEVELS = ["off", "low", "medium", "high", "max"] as const;
export type ReasoningLevel = (typeof REASONING_LEVELS)[number];

/** The agent's own ceilings; the tab and the request may only lower them. */
export const MAX_TOOL_CALLS = 150;
export const MAX_FILE_BYTES = 32 * 1024 * 1024;

export const DOCUMENT_AGENT_SETTINGS_PATH = "/document-agent/settings";

/** What the tab may send: the AI service checks the rest (the profile, the folder, who writes). */
export const documentAgentSettingsSchema = z.object({
  /** Whether the `document_agent` tool is offered to the chat at all. */
  enabled: z.boolean(),
  /** The profile the agent runs on; null means the chat's own model. */
  profileId: z.string().min(1).nullable(),
  /** The effort; "default" follows the model's catalogue default. */
  reasoning: z.enum(["default", ...REASONING_LEVELS]),
  maxToolCalls: z.number().int().min(1).max(MAX_TOOL_CALLS),
  maxFileBytes: z.number().int().min(1).max(MAX_FILE_BYTES),
  /** Whether every task leaves its full log as a file. */
  taskLog: z.boolean(),
  /** The folder the logs go to; null means next to the task's results. */
  taskLogFolderId: z.string().min(1).nullable(),
});

export type DocumentAgentSettings = z.infer<typeof documentAgentSettingsSchema>;

export const DOCUMENT_AGENT_DEFAULTS: DocumentAgentSettings = {
  enabled: true,
  profileId: null,
  reasoning: "default",
  maxToolCalls: MAX_TOOL_CALLS,
  maxFileBytes: MAX_FILE_BYTES,
  taskLog: false,
  taskLogFolderId: null,
};

// The settings as the AI service answers them. It leaves a null out or
// sends it, and keeps no ceiling of its own on the limits, so an absent
// reference is null and a limit above the agent's ceiling is the ceiling.
const storedSettingsSchema = z.object({
  enabled: z.boolean(),
  profileId: z.string().min(1).nullish(),
  reasoning: z.enum(["default", ...REASONING_LEVELS]),
  maxToolCalls: z.number().int().min(1),
  maxFileBytes: z.number().int().min(1),
  taskLog: z.boolean(),
  taskLogFolderId: z.union([z.string().min(1), z.number().int()]).nullish(),
});

/** The AI service's answer as this service uses it; throws on one it cannot read. */
export function parseDocumentAgentSettings(raw: unknown): DocumentAgentSettings {
  const stored = storedSettingsSchema.parse(raw);
  return {
    enabled: stored.enabled,
    profileId: stored.profileId ?? null,
    reasoning: stored.reasoning,
    maxToolCalls: Math.min(stored.maxToolCalls, MAX_TOOL_CALLS),
    maxFileBytes: Math.min(stored.maxFileBytes, MAX_FILE_BYTES),
    taskLog: stored.taskLog,
    taskLogFolderId: stored.taskLogFolderId == null ? null : String(stored.taskLogFolderId),
  };
}

/** The settings of the portal the current request came for, read as its user. */
export async function readDocumentAgentSettings(): Promise<DocumentAgentSettings> {
  return parseDocumentAgentSettings(await aiService.get(DOCUMENT_AGENT_SETTINGS_PATH));
}

/**
 * What a task runs with when the portal's settings cannot be read (the AI
 * service is down, refuses the user or answers something else): the
 * defaults, with the agent off. A portal whose admin turned the agent off
 * must not get it back because of a failed read.
 */
export const DOCUMENT_AGENT_UNREADABLE: DocumentAgentSettings = {
  ...DOCUMENT_AGENT_DEFAULTS,
  enabled: false,
};

/** The portal's settings for a task; the agent is off while they cannot be read. */
export async function readDocumentAgentSettingsForTask(
  read: () => Promise<DocumentAgentSettings> = readDocumentAgentSettings,
): Promise<DocumentAgentSettings> {
  try {
    return await read();
  } catch (err) {
    logger.warn(
      `documentAgentSettings: not read, the agent is off: ${err instanceof Error ? err.message : String(err)}`,
    );
    return DOCUMENT_AGENT_UNREADABLE;
  }
}

/**
 * Whether the portal offers the agent to the chat. A chat round already
 * holds the answer (the aggregate's `documentAgentEnabled`), so listing the
 * round's tools costs no request; outside a round, or with an aggregate
 * that does not carry it, the settings are read.
 */
export async function isDocumentAgentEnabled(): Promise<boolean> {
  const primed = getChatContextSnapshot()?.documentAgentEnabled;
  if (primed !== undefined) {
    return primed;
  }
  return (await readDocumentAgentSettingsForTask()).enabled;
}
