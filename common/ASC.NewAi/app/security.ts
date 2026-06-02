// (c) Copyright Ascensio System SIA 2009-2025
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
