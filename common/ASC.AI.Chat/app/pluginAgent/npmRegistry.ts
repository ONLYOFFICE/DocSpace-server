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

// The npm registry a plugin task's sandbox installs from. A sandbox has no way out but this
// service, so `npm install` in it is pointed here, and this route forwards to the registry the
// installation names (the public one by default) and keeps the archives it fetched.
//
// A machine endpoint outside the API prefix, like the document agent's file routes: the portal's
// router does not forward to it. The gate is the token of a running task; it dies with the task.
//
// Only two things are served, both read-only: a package document and a package archive. The
// archive links of a document are rewritten to this route, and an archive request is mapped back
// to a path of the configured registry. No address is taken from a request or from an answer:
// a redirect of the registry is not followed either, it is a failed fetch.
//
// A document and an archive are held in memory while they are served, up to the limits below,
// so requests are served a few at a time: one takes its turn before it fetches or reads
// anything and keeps it until its answer has left or its client is gone.

import { createHash, randomUUID } from "crypto";
import fs from "fs/promises";
import path from "path";
import type { Application, Response } from "express";

import { getPluginAgent } from "../../config/index.js";
import type { PluginAgentConfig } from "../../config/index.js";
import { asyncHandler } from "../controllers/_helpers.js";
import logger from "../log.js";
import { isObject } from "../narrow.js";
import {
  providerFetch,
  readBodyCapped,
  readTextCapped,
  withTimeout,
} from "../storage/httpClient.js";
import { isRunningTaskToken } from "./tasks.js";

/** Where the route is served; a sandbox reaches it under the configured address of this service. */
export const PLUGIN_AGENT_NPM_PATH = "/plugin-agent/npm";

/** A package document with every version of a much-released package is a few megabytes. */
const MAX_DOCUMENT_BYTES = 64 * 1024 * 1024;
/** The largest archives of the registry are packages that ship prebuilt binaries. */
const MAX_ARCHIVE_BYTES = 128 * 1024 * 1024;
const UPSTREAM_TIMEOUT_MS = 120_000;
/**
 * Requests served at a time, for the whole process: each may hold an archive of the size above,
 * fetched or read from the cache, and whoever holds a task's token can ask for many at once. npm
 * itself asks for more in parallel; the rest wait their turn.
 */
export const MAX_REGISTRY_FETCHES = 4;
/** A half-written archive younger than this may be another request's, still being written. */
const PARTIAL_GRACE_MS = 10 * 60 * 1000;
/** Eviction goes down to this share of the limit, so that it does not run on every write after it. */
const EVICT_TO_SHARE = 0.9;
/** The cache's own files: an archive by the hash of its path, and one being written. */
const KEPT_NAME = /^[0-9a-f]{64}\.tgz(\.[0-9a-f-]{36}\.partial)?$/;
const REFUSAL_BODY_LIMIT = 4096;
/** What npm asks a registry for on install: the document without what an install does not read. */
const INSTALL_DOCUMENT = "application/vnd.npm.install-v1+json";

// A package name as the registry allows it, each part on its own: never empty, `.` or `..`.
const NAME = /^[a-z0-9][a-z0-9._~-]*$/i;
const SCOPE = /^@[a-z0-9~][a-z0-9._~-]*$/i;
const ARCHIVE = /^[a-z0-9][a-z0-9._+~-]*\.tgz$/i;

/** What a request path names: a package document or one of its archives; undefined for anything else. */
export interface RegistryTarget {
  kind: "document" | "archive";
  /** The path under the registry, as the registry spells it. */
  path: string;
}

export function registryTarget(rawPath: string): RegistryTarget | undefined {
  let decoded: string;
  try {
    decoded = decodeURIComponent(rawPath);
  } catch {
    return undefined;
  }
  const parts = decoded.split("/").filter((part, index) => !(index === 0 && part === ""));
  const scope = SCOPE.test(parts[0] ?? "") ? parts.shift() : undefined;
  const [name, dash, file, ...rest] = parts;
  if (name === undefined || !NAME.test(name) || rest.length > 0) {
    return undefined;
  }
  // A scoped document is asked for as `@scope%2fname`, its archive as `@scope/name/-/file.tgz`.
  const packagePath = scope ? `${scope}/${name}` : name;
  if (dash === undefined) {
    return { kind: "document", path: scope ? `${scope}%2f${name}` : name };
  }
  if (dash !== "-" || file === undefined || !ARCHIVE.test(file)) {
    return undefined;
  }
  return { kind: "archive", path: `${packagePath}/-/${file}` };
}

/** The token a request presents: npm sends the `_authToken` of its configuration as a bearer. */
function presentedToken(authorization: string | undefined): string {
  const header = authorization ?? "";
  return header.startsWith("Bearer ") ? header.slice("Bearer ".length) : "";
}

export function registerPluginAgentRegistry(app: Application): void {
  app.use(
    PLUGIN_AGENT_NPM_PATH,
    asyncHandler(async (req, res) => {
      const config = getPluginAgent();
      if (!config || !isRunningTaskToken(presentedToken(req.headers.authorization))) {
        res.status(401).json({ error: "Unauthorized" });
        return;
      }
      if (req.method !== "GET" && req.method !== "HEAD") {
        res.setHeader("Allow", "GET, HEAD");
        res.status(405).json({ error: "method not allowed" });
        return;
      }
      const target = registryTarget(req.path);
      if (!target) {
        res.status(404).json({ error: "Not Found" });
        return;
      }
      // The fetch ends with the client: a sandbox that hung up waits for nothing.
      const gone = new AbortController();
      res.on("close", () => {
        if (!res.writableEnded) {
          gone.abort();
        }
      });
      await inTurn(async () => {
        // Gone while it waited: there is nobody to fetch or read for.
        if (gone.signal.aborted) {
          return;
        }
        if (target.kind === "document") {
          await serveDocument(config, target.path, req.baseUrl, res, gone.signal);
        } else {
          await serveArchive(config, target.path, res, gone.signal);
        }
        // The turn is kept until the bytes have left this process, or the client has.
        await answered(res);
      });
    }),
  );
}

/** Settles when the answer has been written out, or the connection is closed. */
function answered(res: Response): Promise<void> {
  if (res.writableFinished || res.destroyed) {
    return Promise.resolve();
  }
  return new Promise((resolve) => {
    res.once("finish", resolve);
    res.once("close", resolve);
  });
}

// The requests being served and the ones waiting for a place. Process-wide on purpose: the bound is
// on this process's memory, whatever portal or task a request belongs to (ts-ai-failure-paths.md
// §8); an entry is a callback of a request that is still open, removed when its turn comes.
let fetching = 0;
const waiting: (() => void)[] = [];

async function inTurn<T>(run: () => Promise<T>): Promise<T> {
  if (fetching >= MAX_REGISTRY_FETCHES) {
    await new Promise<void>((resolve) => waiting.push(resolve));
  } else {
    fetching += 1;
  }
  try {
    return await run();
  } finally {
    // The place goes straight to the next in line, or is given back.
    const next = waiting.shift();
    if (next) {
      next();
    } else {
      fetching -= 1;
    }
  }
}

/** The registry's answer to one `GET`: its bytes on 200, its absence on 404; anything else is unknown. */
async function fromRegistry(
  config: PluginAgentConfig,
  targetPath: string,
  accept: string,
  limit: number,
  client: AbortSignal,
): Promise<{ bytes: Uint8Array; contentType: string } | "absent" | "failed"> {
  const bounded = withTimeout(client, UPSTREAM_TIMEOUT_MS);
  try {
    const res = await providerFetch(
      `${config.registryUrl}/${targetPath}`,
      {
        method: "GET",
        headers: { Accept: accept },
        // Another address is never asked: a redirect is a failure of the fetch.
        redirect: "error",
        signal: bounded.signal,
      },
      "none",
    );
    if (!res.ok) {
      // The body of a refusal is the registry's business and is not relayed; it is read so
      // that the connection is free for the next request, up to a limit.
      await readTextCapped(res, REFUSAL_BODY_LIMIT);
      if (res.status !== 404) {
        logger.warn(`pluginAgent registry: ${targetPath} answered ${res.status}`);
      }
      return res.status === 404 ? "absent" : "failed";
    }
    const bytes = await readBodyCapped(res, limit);
    if (bytes === null) {
      logger.warn(`pluginAgent registry: ${targetPath} is over ${limit} bytes`);
      return "failed";
    }
    return { bytes, contentType: res.headers.get("content-type") ?? "application/octet-stream" };
  } catch (err) {
    logger.warn(`pluginAgent registry: ${targetPath} not fetched: ${String(err)}`);
    return "failed";
  } finally {
    bounded.cancel();
  }
}

function refuse(res: Response, outcome: "absent" | "failed"): void {
  if (outcome === "absent") {
    res.status(404).json({ error: "Not Found" });
  } else {
    res.status(502).json({ error: "Bad Gateway" });
  }
}

async function serveDocument(
  config: PluginAgentConfig,
  targetPath: string,
  mountPath: string,
  res: Response,
  client: AbortSignal,
): Promise<void> {
  const answer = await fromRegistry(
    config,
    targetPath,
    INSTALL_DOCUMENT,
    MAX_DOCUMENT_BYTES,
    client,
  );
  if (typeof answer === "string") {
    refuse(res, answer);
    return;
  }
  let document: unknown;
  try {
    document = JSON.parse(Buffer.from(answer.bytes).toString("utf8"));
  } catch {
    logger.warn(`pluginAgent registry: the document of ${targetPath} is not JSON`);
    refuse(res, "failed");
    return;
  }
  // Where the caller reached this route, so that an archive link leads back to it.
  rewriteArchiveLinks(document, config.registryUrl, `${config.serviceUrl}${mountPath}`);
  res.status(200).type(answer.contentType).send(JSON.stringify(document));
}

/** Points every archive link that leads to the registry at this route; any other link is left alone. */
export function rewriteArchiveLinks(
  document: unknown,
  registryUrl: string,
  routeUrl: string,
): void {
  const versions = isObject(document) ? document["versions"] : undefined;
  if (!isObject(versions)) {
    return;
  }
  for (const version of Object.values(versions)) {
    const dist = isObject(version) ? version["dist"] : undefined;
    const link = isObject(dist) ? dist["tarball"] : undefined;
    if (isObject(dist) && typeof link === "string" && link.startsWith(`${registryUrl}/`)) {
      dist["tarball"] = routeUrl + link.slice(registryUrl.length);
    }
  }
}

async function serveArchive(
  config: PluginAgentConfig,
  targetPath: string,
  res: Response,
  client: AbortSignal,
): Promise<void> {
  // The file is named by a hash of the path, so no spelling of a package name leaves the directory.
  const file = path.join(
    config.cacheDir,
    `${createHash("sha256").update(targetPath).digest("hex")}.tgz`,
  );
  let bytes = await readKept(file);
  if (bytes === undefined) {
    const answer = await fromRegistry(config, targetPath, "*/*", MAX_ARCHIVE_BYTES, client);
    if (typeof answer === "string") {
      refuse(res, answer);
      return;
    }
    bytes = answer.bytes;
    await keepArchive(config, file, bytes);
  }
  // A view of the same bytes: an archive is not copied once more to be sent.
  res
    .status(200)
    .type("application/octet-stream")
    .send(Buffer.from(bytes.buffer, bytes.byteOffset, bytes.byteLength));
}

async function readKept(file: string): Promise<Uint8Array | undefined> {
  try {
    const bytes = await fs.readFile(file);
    // Read again: the one read longest ago is the first to go when the directory is full.
    const now = new Date();
    await fs.utimes(file, now, now);
    return bytes;
  } catch {
    // Not kept, or no longer readable: either way the registry is asked.
    return undefined;
  }
}

// What the kept archives weigh together, per directory: counted by listing it on the first
// write and after every eviction, and added to by the writes of this process in between. An
// estimate: it runs ahead when an archive is written twice, and a write that lands while another
// request evicts is missing from it until the next listing, so the limit can be passed by what
// the requests in flight wrote. Shared by every portal the process serves: the archives are public packages, the same
// for all of them (ts-ai-failure-paths.md §8). One entry per cache directory the configuration
// ever named, which is one.
const keptBytes = new Map<string, number>();

/** For tests: forget what the directories were counted to hold. */
export function forgetKeptArchives(): void {
  keptBytes.clear();
}

/** Keeps an archive for later requests. A failure here costs a later download, never the answer. */
export async function keepArchive(
  config: Pick<PluginAgentConfig, "cacheDir" | "cacheMaxBytes">,
  file: string,
  bytes: Uint8Array,
): Promise<void> {
  // Written under another name and renamed: a reader never sees half an archive.
  const partial = `${file}.${randomUUID()}.partial`;
  try {
    await fs.mkdir(config.cacheDir, { recursive: true });
    await fs.writeFile(partial, bytes);
    await fs.rename(partial, file);
    const known = keptBytes.get(config.cacheDir);
    // The first write lists the directory, the archive just written included.
    const total = known === undefined ? await weigh(config.cacheDir) : known + bytes.length;
    keptBytes.set(config.cacheDir, total);
    if (total > config.cacheMaxBytes) {
      keptBytes.set(config.cacheDir, await evict(config.cacheDir, config.cacheMaxBytes, file));
    }
  } catch (err) {
    logger.warn(`pluginAgent registry: an archive was not kept: ${String(err)}`);
    // Half a file is not left behind, and the next write counts the directory again.
    keptBytes.delete(config.cacheDir);
    try {
      await fs.rm(partial, { force: true });
    } catch (leftover) {
      logNotRemoved(leftover);
    }
  }
}

function logNotRemoved(err: unknown): void {
  logger.warn(`pluginAgent registry: a file of the cache was not removed: ${String(err)}`);
}

interface KeptFile {
  file: string;
  size: number;
  readAt: number;
}

/** The cache's own files in the directory; anything else there is not its to count or delete. */
async function listKept(dir: string): Promise<KeptFile[]> {
  const kept: KeptFile[] = [];
  for (const name of await fs.readdir(dir)) {
    if (!KEPT_NAME.test(name)) {
      continue;
    }
    const file = path.join(dir, name);
    try {
      const stat = await fs.stat(file);
      kept.push({ file, size: stat.size, readAt: stat.mtimeMs });
    } catch (err) {
      // Renamed or evicted by another request since the listing: it is not there to count.
      logger.debug(`pluginAgent registry: ${name} left the cache during a listing: ${String(err)}`);
    }
  }
  return kept;
}

async function weigh(dir: string): Promise<number> {
  return (await listKept(dir)).reduce((sum, entry) => sum + entry.size, 0);
}

/**
 * Deletes the files read longest ago until the directory is well under its limit; answers what
 * is left. A file that cannot be deleted is skipped and stays counted.
 */
async function evict(dir: string, maxBytes: number, justWritten: string): Promise<number> {
  const kept = (await listKept(dir)).sort((a, b) => a.readAt - b.readAt);
  let total = kept.reduce((sum, entry) => sum + entry.size, 0);
  for (const entry of kept) {
    if (total <= maxBytes * EVICT_TO_SHARE) {
      break;
    }
    // The archive of this request stays even when it alone is over the limit: it is being
    // served. So does a half-written one that may still be another request's.
    if (
      entry.file === justWritten ||
      (entry.file.endsWith(".partial") && entry.readAt > Date.now() - PARTIAL_GRACE_MS)
    ) {
      continue;
    }
    try {
      await fs.rm(entry.file, { force: true });
      total -= entry.size;
    } catch (err) {
      logNotRemoved(err);
    }
  }
  return total;
}
