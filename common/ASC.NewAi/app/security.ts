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

function isIpv4Literal(host: string): number[] | null {
  const m = /^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$/.exec(host);
  if (!m) {
    return null;
  }
  const octets = m.slice(1).map(Number);
  return octets.some((o) => o > 255) ? null : octets;
}

// Block cloud-metadata / link-local addresses that are never a legitimate
// LLM or web-search endpoint. RFC1918 private ranges and loopback are
// allowed on purpose: DocSpace supports on-prem model servers reachable
// only on the internal network (e.g. a local Ollama at 127.0.0.1, or an
// inference box on 10.x / 192.168.x). Network-layer egress filtering is the
// right place to restrict those; here we only stop the obvious SSRF targets.
function isBlockedHost(hostname: string): boolean {
  const host = hostname.replace(/^\[|\]$/g, "").toLowerCase();
  const v4 = isIpv4Literal(host);
  if (v4) {
    const [a, b] = v4;
    if (a === 169 && b === 254) {
      return true; // 169.254.0.0/16 link-local — AWS/GCP/Azure metadata (IMDS)
    }
    if (a === 0) {
      return true; // 0.0.0.0/8 "this host"
    }
    return false;
  }
  if (host.startsWith("fe80:")) {
    return true; // IPv6 link-local
  }
  if (host.includes("169.254.")) {
    return true; // IPv4-mapped IPv6 of link-local, e.g. ::ffff:169.254.169.254
  }
  return false;
}

/**
 * Validate a user-supplied provider / web-search `baseUrl` before the engine
 * makes an outbound call to it (anti-SSRF). No-op for an absent endpoint
 * (cloud providers use their built-in URL). Throws {@link InvalidUrlError}
 * for a malformed URL, a non-http(s) scheme, embedded credentials, or a
 * link-local / metadata host.
 */
export function assertSafeBaseUrl(raw: unknown): void {
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
  if (isBlockedHost(parsed.hostname)) {
    throw new InvalidUrlError("baseUrl host is not allowed");
  }
}
