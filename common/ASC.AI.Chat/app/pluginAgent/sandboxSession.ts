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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

// The sandbox of one plugin task, from its creation to its removal: created for the task's id,
// given the skill's scripts and an npm configuration (one that points at this service when the
// installation names its address for sandboxes), kept alive by
// a timer while the task runs, and deleted when the task ends, however it ends (a deletion the
// gateway does not take is logged and the sandbox left to its lifetime).
//
// The lifetime asked for is short and renewed: a sandbox whose task died with the process is
// not deleted by anybody, and expires by itself within that lifetime.

import fs from "fs/promises";
import path from "path";

import type { PluginAgentConfig } from "../../config/index.js";
import logger from "../log.js";
import { pooled } from "../storage/pooled.js";
import { PLUGIN_AGENT_NPM_PATH } from "./npmRegistry.js";
import {
  createSandbox,
  deleteSandbox,
  makeDir,
  renewSandbox,
  SandboxError,
  writeSandboxFile,
} from "./sandboxClient.js";
import type { PluginSkill } from "./skill.js";
import type { RunningTask } from "./tasks.js";

/** Where a task works: the project directories lie under it. */
export const SANDBOX_WORKSPACE = "/workspace";
/** Where the skill's scripts and templates are copied to; `$SKILL` of the skill's own text. */
export const SANDBOX_SKILL_DIR = `${SANDBOX_WORKSPACE}/.skill`;
const SANDBOX_NPMRC = `${SANDBOX_WORKSPACE}/.npmrc`;
/** Files of the skill written at a time. */
const COPY_POOL = 4;
/** The lifetime is renewed at every third of it, so that one failed renewal loses nothing. */
const RENEWALS_PER_LIFETIME = 3;

export interface SandboxSession {
  id: string;
  /** The environment of every command: where npm finds its configuration. */
  env: Record<string, string>;
  /** Aborted when a renewal learns that the sandbox is gone. */
  lost: AbortSignal;
  /** Stops the renewals and deletes the sandbox. Never rejects. */
  dispose(): Promise<void>;
}

/**
 * The npm configuration of a task. With `serviceUrl`, this service is the registry and the
 * task's token its credential. Without it no registry is named: npm keeps the one the sandbox
 * gives it, so the packages its image holds in npm's cache are found there.
 */
export function npmConfiguration(serviceUrl: string | undefined, registryToken: string): string {
  // Asked of a registry by every install and read by nobody: the route of this service serves
  // none of them, and a sandbox's own way to a registry is spared them.
  const quiet = ["audit=false", "fund=false", "update-notifier=false"];
  if (serviceUrl === undefined) {
    return [...quiet, ""].join("\n");
  }
  const registry = `${serviceUrl}${PLUGIN_AGENT_NPM_PATH}/`;
  return [
    `registry=${registry}`,
    // npm sends a token to the addresses under the path it is configured for, the archives included.
    `${registry.replace(/^https?:/, "")}:_authToken=${registryToken}`,
    ...quiet,
    // A lock file then names no registry address: the one a sandbox installs from is this
    // service's internal address, and the lock file leaves with the sources.
    "omit-lockfile-registry-resolved=true",
    "",
  ].join("\n");
}

async function copySkill(
  config: PluginAgentConfig,
  id: string,
  skill: PluginSkill,
  signal: AbortSignal,
): Promise<void> {
  const dirs = new Set(skill.sandboxFiles.map((file) => path.posix.dirname(file)));
  for (const dir of [...dirs].sort()) {
    await makeDir(config, id, `${SANDBOX_SKILL_DIR}/${dir}`, signal);
  }
  await pooled(skill.sandboxFiles, COPY_POOL, async (file) => {
    const bytes = await fs.readFile(path.join(skill.dir, ...file.split("/")));
    await writeSandboxFile(config, id, `${SANDBOX_SKILL_DIR}/${file}`, bytes, signal);
  });
}

/** How long the removal of a sandbox whose creation ended without an answer may take. */
const ORPHAN_TIMEOUT_MS = 30_000;

/**
 * A Create that ended without an answer (the reply was stopped, the call timed out, the answer
 * could not be read) may have made the sandbox: the gateway finishes a creation the caller gave
 * up on. Create is asked once more for the same session, which answers that sandbox, and it is
 * deleted. Best effort: when this fails too, the sandbox lives until its lifetime ends.
 */
async function removeUnconfirmed(config: PluginAgentConfig, task: RunningTask): Promise<void> {
  try {
    const id = await createSandbox(
      config,
      task.id,
      config.sandboxTtlSeconds,
      AbortSignal.timeout(ORPHAN_TIMEOUT_MS),
    );
    await deleteSandbox(config, id);
  } catch (err) {
    logger.warn(
      `pluginAgent: a sandbox of task ${task.id} may be left to its lifetime: ${describe(err)}`,
    );
  }
}

/**
 * Creates the task's sandbox and prepares it. A failure after the sandbox exists deletes it
 * before the failure is passed on, and one of the creation itself that is not a refusal has the
 * sandbox looked for and deleted, so a task that never started leaves nothing behind unless the
 * gateway cannot be reached for that either.
 */
export async function openSandboxSession(
  config: PluginAgentConfig,
  task: RunningTask,
  skill: PluginSkill,
  signal: AbortSignal,
): Promise<SandboxSession> {
  let id: string;
  try {
    id = await createSandbox(config, task.id, config.sandboxTtlSeconds, signal);
  } catch (err) {
    // "No room" says that nothing was made. Nothing else does: no answer at all, an answer
    // that cannot be read, and "not in time", after which the sandbox may still come up.
    if (!(err instanceof SandboxError) || err.code !== "resource_exhausted") {
      await removeUnconfirmed(config, task);
    }
    throw err;
  }
  const lost = new AbortController();

  const renew = async (): Promise<void> => {
    try {
      await renewSandbox(config, id, config.sandboxTtlSeconds);
    } catch (err) {
      logger.warn(`pluginAgent: sandbox ${id} not renewed: ${describe(err)}`);
      if (err instanceof SandboxError && err.kind === "lost") {
        lost.abort(err);
      }
    }
  };
  // Started before the sandbox is prepared: the lifetime counts from its creation, and the
  // preparation takes a part of it.
  const timer = setInterval(
    () => void renew(),
    (config.sandboxTtlSeconds * 1000) / RENEWALS_PER_LIFETIME,
  );
  // A task is ended by its own signal; the timer alone never keeps the process running.
  timer.unref();

  const dispose = async (): Promise<void> => {
    clearInterval(timer);
    try {
      await deleteSandbox(config, id);
    } catch (err) {
      // Left to its lifetime: the gateway removes it when that ends.
      logger.warn(`pluginAgent: sandbox ${id} not deleted: ${describe(err)}`);
    }
  };

  try {
    await copySkill(config, id, skill, signal);
    await writeSandboxFile(
      config,
      id,
      SANDBOX_NPMRC,
      Buffer.from(npmConfiguration(config.serviceUrl, task.registryToken)),
      signal,
    );
  } catch (err) {
    await dispose();
    throw err;
  }

  return { id, env: { NPM_CONFIG_USERCONFIG: SANDBOX_NPMRC, CI: "1" }, lost: lost.signal, dispose };
}

/** A failure for the log: what the gateway said for a sandbox error, the error's text otherwise. */
export function describe(err: unknown): string {
  return err instanceof SandboxError ? `${err.message} (${err.detail})` : String(err);
}
