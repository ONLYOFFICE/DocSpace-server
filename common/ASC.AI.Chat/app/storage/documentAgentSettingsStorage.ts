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
// limits, whether and where their logs are kept. Owned by this service and
// kept in one JSON file on its host, one entry per portal, keyed by the
// host the portal is reached at (the same key the portal resolves the
// tenant by). A portal that never saved anything gets the defaults.

import { promises as fs } from "fs";
import path from "path";

import { z } from "zod";

import { getDocumentAgent } from "../../config/index.js";
import logger, { logDir } from "../log.js";
import { getForwardedHeaders } from "../requestContext.js";

export const REASONING_LEVELS = ["off", "low", "medium", "high", "max"] as const;
export type ReasoningLevel = (typeof REASONING_LEVELS)[number];

/** The agent's own ceilings; the tab and the request may only lower them. */
export const MAX_TOOL_CALLS = 150;
export const MAX_FILE_BYTES = 32 * 1024 * 1024;

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

const DEFAULT_FILE_NAME = "document-agent-settings.json";

/**
 * The portal the request came for, as the portal itself tells tenants apart:
 * the host the browser used, forwarded by the router (see tenantHeaders in
 * documentAgent.ts), else the plain host. Case does not matter in a host.
 */
export function tenantKey(): string {
  const forwarded = getForwardedHeaders();
  const host = forwarded["x-forwarded-host"] || forwarded["host"] || "";
  return host.split(",")[0]?.trim().toLowerCase() || "default";
}

/** One JSON file holding every portal's settings; read once, written through. */
export class DocumentAgentSettingsStorage {
  readonly #path: string;
  #entries: Promise<Record<string, DocumentAgentSettings>> | undefined;
  #writing: Promise<void> = Promise.resolve();

  constructor(filePath: string) {
    this.#path = filePath;
  }

  get path(): string {
    return this.#path;
  }

  /** The portal's settings, the defaults when it saved none; a damaged entry counts as none. */
  async read(key: string): Promise<DocumentAgentSettings> {
    const entries = await this.#load();
    return entries[key] ?? DOCUMENT_AGENT_DEFAULTS;
  }

  async write(key: string, settings: DocumentAgentSettings): Promise<DocumentAgentSettings> {
    // Writes queue behind each other: two admins saving at once must not
    // interleave on the file, and each one starts from what the previous one
    // left. The cache takes the new entry only once the file holds it, so a
    // failed write is not served as saved; and a failed write does not stop
    // the queue, so the next save tries the file again.
    const step = this.#writing.then(async () => {
      const updated = { ...(await this.#load()), [key]: settings };
      await this.#save(updated);
      this.#entries = Promise.resolve(updated);
    });
    this.#writing = step.catch(() => undefined);
    await step;
    return settings;
  }

  // A read that fails is not kept: the next call reads the file again.
  async #load(): Promise<Record<string, DocumentAgentSettings>> {
    if (!this.#entries) {
      const loading = this.#readFile();
      this.#entries = loading;
      loading.catch(() => {
        if (this.#entries === loading) {
          this.#entries = undefined;
        }
      });
    }
    return this.#entries;
  }

  async #readFile(): Promise<Record<string, DocumentAgentSettings>> {
    let raw: string;
    try {
      raw = await fs.readFile(this.#path, "utf8");
    } catch (err) {
      if ((err as { code?: string }).code === "ENOENT") {
        return {};
      }
      throw err;
    }
    const parsed: unknown = JSON.parse(raw);
    const entries: Record<string, DocumentAgentSettings> = {};
    if (parsed && typeof parsed === "object" && !Array.isArray(parsed)) {
      for (const [key, value] of Object.entries(parsed as Record<string, unknown>)) {
        const result = documentAgentSettingsSchema.safeParse(value);
        if (result.success) {
          entries[key] = result.data;
        }
      }
    }
    return entries;
  }

  async #save(entries: Record<string, DocumentAgentSettings>): Promise<void> {
    await fs.mkdir(path.dirname(this.#path), { recursive: true });
    // Written beside and renamed over: a crash mid-write leaves the old file whole.
    const tmp = `${this.#path}.${process.pid}.tmp`;
    await fs.writeFile(tmp, `${JSON.stringify(entries, null, 2)}\n`, "utf8");
    await fs.rename(tmp, this.#path);
  }
}

let shared: DocumentAgentSettingsStorage | undefined;

/** The service's one storage, at `ai.documentAgent.settingsPath` or next to the logs. */
export function documentAgentSettingsStorage(): DocumentAgentSettingsStorage {
  shared ??= new DocumentAgentSettingsStorage(
    getDocumentAgent()?.settingsPath ?? path.join(logDir, DEFAULT_FILE_NAME),
  );
  return shared;
}

/** The settings of the portal the current request came for. */
/**
 * What a task runs with when the portal's settings cannot be read (the file
 * is unreadable or damaged): the defaults, with the agent off. A portal whose
 * admin turned the agent off must not get it back because of a broken file.
 */
export const DOCUMENT_AGENT_UNREADABLE: DocumentAgentSettings = {
  ...DOCUMENT_AGENT_DEFAULTS,
  enabled: false,
};

/** The portal's settings for a task; the agent is off while they cannot be read. */
export async function readDocumentAgentSettingsForTask(
  storage: DocumentAgentSettingsStorage = documentAgentSettingsStorage(),
): Promise<DocumentAgentSettings> {
  try {
    return await storage.read(tenantKey());
  } catch (err) {
    logger.warn(
      `documentAgentSettings: not read, the agent is off: ${err instanceof Error ? err.message : String(err)}`,
    );
    return DOCUMENT_AGENT_UNREADABLE;
  }
}
