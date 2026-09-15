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

import { BlockList, isIP } from "net";
import { lookup } from "dns/promises";
import { allowPrivateBaseUrl } from "../config/index.js";

// A client-correctable error. `asyncHandler` surfaces the message verbatim
// (because `expose` is set) under the carried `status`, unlike the generic
// 500 it returns for unexpected failures.
export class InvalidUrlError extends Error {
  public readonly status = 400;
  public readonly expose = true;
  constructor(message: string) {
    super(message);
    this.name = "InvalidUrlError";
  }
}

// Cloud-metadata / link-local and the unspecified "this host" range: never a
// legitimate LLM or web-search endpoint, so blocked even when an on-prem
// install opts into private ranges via `AI__ALLOW_PRIVATE_BASE_URL`.
// `BlockList` matches IPv4-mapped IPv6 (`::ffff:169.254.169.254`) against the
// IPv4 rules automatically, so a mapped metadata address is caught too.
const alwaysBlocked = new BlockList();
alwaysBlocked.addSubnet("0.0.0.0", 8, "ipv4"); // "this host"
alwaysBlocked.addSubnet("169.254.0.0", 16, "ipv4"); // link-local — IMDS metadata
alwaysBlocked.addSubnet("fe80::", 10, "ipv6"); // IPv6 link-local

// Loopback + RFC1918 private ranges. Blocked by default (anti-SSRF), allowed
// when `AI__ALLOW_PRIVATE_BASE_URL` is set for on-prem model servers reachable
// only on the internal network. Mirrors the private entries of the C#
// `UrlValidator` default blacklist.
const privateBlocked = new BlockList();
privateBlocked.addSubnet("127.0.0.0", 8, "ipv4"); // loopback
privateBlocked.addSubnet("10.0.0.0", 8, "ipv4");
privateBlocked.addSubnet("100.64.0.0", 10, "ipv4"); // carrier-grade NAT
privateBlocked.addSubnet("172.16.0.0", 12, "ipv4");
privateBlocked.addSubnet("192.168.0.0", 16, "ipv4");
privateBlocked.addAddress("::1", "ipv6"); // loopback
privateBlocked.addSubnet("fc00::", 7, "ipv6"); // unique local

// Cap on the DNS lookup so an unresolvable / slow name can't hang the guard.
// Matches the 3s the C# `UrlValidator` allows for resolution.
const DNS_TIMEOUT_MS = 3000;

function ipFamily(address: string): "ipv4" | "ipv6" | undefined {
  const family = isIP(address);
  return family === 4 ? "ipv4" : family === 6 ? "ipv6" : undefined;
}

// True when `address` falls in a range we refuse to reach. `privateBlocked` is
// consulted unless the on-prem override is set; `alwaysBlocked` always is.
function isBlockedAddress(address: string, family: "ipv4" | "ipv6"): boolean {
  if (alwaysBlocked.check(address, family)) {
    return true;
  }
  return !allowPrivateBaseUrl() && privateBlocked.check(address, family);
}

async function resolveHost(
  host: string,
): Promise<{ address: string; family: number }[]> {
  const timeout = new Promise<never>((_, reject) => {
    setTimeout(
      () => reject(new Error("DNS resolution timed out")),
      DNS_TIMEOUT_MS,
    ).unref();
  });
  return Promise.race([lookup(host, { all: true }), timeout]);
}

/**
 * Validate a user-supplied provider / web-search `baseUrl` before the engine
 * makes an outbound call to it (anti-SSRF). No-op for an absent endpoint
 * (cloud providers use their built-in URL). Throws {@link InvalidUrlError}
 * for a malformed URL, a non-http(s) scheme, embedded credentials, or a host
 * that is — or resolves to — a loopback / private / link-local / metadata
 * address.
 *
 * WHATWG `URL` normalises decimal / hex / octal / short-form IPv4 literals to
 * dotted form (`2130706433`, `0x7f000001`, `127.1` all become `127.0.0.1`), so
 * a literal host is range-checked directly. A name is resolved via DNS first
 * and every returned address is checked, so a hostname pointing at an internal
 * address (`localhost`, or an attacker-controlled record) cannot slip a private
 * target past the guard. Resolution failure is treated as unsafe: if the host
 * can't be verified, the call is refused rather than left to the engine.
 */
export async function assertSafeBaseUrl(raw: unknown): Promise<void> {
  if (raw === undefined || raw === null || raw === "") {
    return;
  }
  if (typeof raw !== "string") {
    throw new InvalidUrlError("baseUrl must be a string");
  }
  let parsed: URL;
  try {
    parsed = new URL(raw);
  } catch {
    throw new InvalidUrlError("baseUrl is not a valid URL");
  }
  if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
    throw new InvalidUrlError("baseUrl must use http or https");
  }
  if (parsed.username || parsed.password) {
    throw new InvalidUrlError("baseUrl must not contain credentials");
  }

  const host = parsed.hostname.replace(/^\[|\]$/g, "");
  const literalFamily = ipFamily(host);
  if (literalFamily) {
    if (isBlockedAddress(host, literalFamily)) {
      throw new InvalidUrlError("baseUrl host is not allowed");
    }
    return;
  }

  let resolved: { address: string; family: number }[];
  try {
    resolved = await resolveHost(host);
  } catch {
    throw new InvalidUrlError("baseUrl host could not be resolved");
  }
  for (const { address, family } of resolved) {
    if (isBlockedAddress(address, family === 4 ? "ipv4" : "ipv6")) {
      throw new InvalidUrlError("baseUrl host is not allowed");
    }
  }
}
