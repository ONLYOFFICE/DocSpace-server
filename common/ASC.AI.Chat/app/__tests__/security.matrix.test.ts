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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// The guard against outbound requests to internal addresses, with the real configuration
// reader (`AI__ALLOW_PRIVATE_BASE_URL`) and DNS mocked at `dns/promises`: no lookup leaves the
// process.

const dns = vi.hoisted(() => ({ lookup: vi.fn() }));
vi.mock("dns/promises", () => ({ lookup: dns.lookup, default: { lookup: dns.lookup } }));

const { assertSafeBaseUrl, InvalidUrlError } = await import("../security.js");

const allowPrivate = (value: string) => {
  vi.stubEnv("AI__ALLOW_PRIVATE_BASE_URL", value);
};

beforeEach(() => {
  allowPrivate("");
  dns.lookup.mockReset();
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.useRealTimers();
});

async function refusal(raw: unknown): Promise<string> {
  try {
    await assertSafeBaseUrl(raw);
  } catch (error) {
    expect(error).toBeInstanceOf(InvalidUrlError);
    return (error as Error).message;
  }
  return "accepted";
}

const NOT_ALLOWED = "baseUrl host is not allowed";

describe("InvalidUrlError", () => {
  it("is a 400 whose message the caller may see", () => {
    const error = new InvalidUrlError("bad");

    expect(error).toBeInstanceOf(Error);
    expect(error.name).toBe("InvalidUrlError");
    expect(error.message).toBe("bad");
    expect(error.status).toBe(400);
    expect(error.expose).toBe(true);
  });
});

describe("assertSafeBaseUrl on the shape of the value", () => {
  it.each([undefined, null, ""])(
    "accepts an absent endpoint (%j) without a lookup",
    async (raw) => {
      await expect(assertSafeBaseUrl(raw)).resolves.toBeUndefined();
      expect(dns.lookup).not.toHaveBeenCalled();
    },
  );

  it.each([0, 42, false, true, {}, [], ["http://8.8.8.8"], { href: "http://8.8.8.8" }])(
    "refuses the non-string %j",
    async (raw) => {
      expect(await refusal(raw)).toBe("baseUrl must be a string");
    },
  );

  it.each([" ", "not a url", "8.8.8.8", "//8.8.8.8/v1", "http://", "http://[::1", "http://a b/"])(
    "refuses %j as not a URL",
    async (raw) => {
      expect(await refusal(raw)).toBe("baseUrl is not a valid URL");
    },
  );

  it.each([
    "ftp://8.8.8.8/",
    "file:///etc/passwd",
    "ws://8.8.8.8/",
    "wss://8.8.8.8/",
    "gopher://8.8.8.8/",
    "javascript:alert(1)",
    "data:text/plain,x",
    "mailto:a@b.c",
  ])("refuses the scheme of %s", async (raw) => {
    expect(await refusal(raw)).toBe("baseUrl must use http or https");
  });

  it.each(["HTTP://8.8.8.8/", "Https://8.8.8.8:8443/v1?x=1#f"])(
    "accepts %s, the scheme in any case",
    async (raw) => {
      await expect(assertSafeBaseUrl(raw)).resolves.toBeUndefined();
    },
  );

  it.each(["http://user@8.8.8.8/", "http://:pass@8.8.8.8/", "https://user:pass@api.example.com/"])(
    "refuses credentials in %s",
    async (raw) => {
      expect(await refusal(raw)).toBe("baseUrl must not contain credentials");
      expect(dns.lookup).not.toHaveBeenCalled();
    },
  );
});

describe("assertSafeBaseUrl on an address literal", () => {
  const ALWAYS = [
    "http://0.0.0.0/",
    "http://0.255.255.255/",
    "http://0/",
    "http://169.254.0.1/",
    "http://169.254.169.254/latest/meta-data",
    "http://169.254.255.255/",
    "http://[fe80::1]/",
    "http://[febf:ffff::1]/",
    "http://[::]/",
    "http://[::ffff:169.254.169.254]/",
    "http://[::ffff:0.0.0.0]/",
  ];

  const PRIVATE = [
    "http://127.0.0.1:11434/v1",
    "http://127.255.255.254/",
    "http://10.0.0.1/",
    "http://10.255.255.255/",
    "http://100.64.0.1/",
    "http://100.127.255.255/",
    "http://172.16.0.1/",
    "http://172.31.255.255/",
    "http://192.168.0.1/",
    "http://192.168.255.255/",
    "http://[::1]/",
    "http://[fc00::1]/",
    "http://[fdff:ffff::1]/",
    "http://[::ffff:127.0.0.1]/",
    "http://[::ffff:10.0.0.1]/",
    "http://[0:0:0:0:0:0:0:1]/",
    // Numeric, hex, octal and short spellings that WHATWG URL normalises to dotted form.
    "http://2130706433/",
    "http://0x7f000001/",
    "http://0x7f.0.0.1/",
    "http://0177.0.0.1/",
    "http://127.1/",
    "http://10.1/",
    "http://167772161/",
    "http://3232235521/",
  ];

  const PUBLIC = [
    "http://8.8.8.8/",
    "http://9.255.255.255/",
    "http://11.0.0.0/",
    "http://100.63.255.255/",
    "http://100.128.0.0/",
    "http://126.255.255.255/",
    "http://128.0.0.0/",
    "http://169.253.255.255/",
    "http://169.255.0.0/",
    "http://172.15.255.255/",
    "http://172.32.0.0/",
    "http://192.167.255.255/",
    "http://192.169.0.0/",
    "http://1.0.0.0/",
    "http://[2001:4860:4860::8888]/",
    "http://[fbff:ffff::1]/",
    "http://[fe00::1]/",
    "http://[::ffff:8.8.8.8]/",
    "http://134744072/",
  ];

  it.each(ALWAYS)("refuses %s by default", async (raw) => {
    expect(await refusal(raw)).toBe(NOT_ALLOWED);
  });

  it.each(ALWAYS)("refuses %s even where private ranges are allowed", async (raw) => {
    allowPrivate("true");
    expect(await refusal(raw)).toBe(NOT_ALLOWED);
  });

  it.each(PRIVATE)("refuses %s by default", async (raw) => {
    expect(await refusal(raw)).toBe(NOT_ALLOWED);
    expect(dns.lookup).not.toHaveBeenCalled();
  });

  it.each(PRIVATE)("accepts %s where private ranges are allowed", async (raw) => {
    allowPrivate("1");
    await expect(assertSafeBaseUrl(raw)).resolves.toBeUndefined();
    expect(dns.lookup).not.toHaveBeenCalled();
  });

  it.each(["yes", "TRUE", "0", "false", ""])(
    "keeps private ranges closed for AI__ALLOW_PRIVATE_BASE_URL=%j",
    async (value) => {
      allowPrivate(value);
      expect(await refusal("http://10.0.0.1/")).toBe(NOT_ALLOWED);
    },
  );

  it.each(PUBLIC)("accepts %s without a lookup", async (raw) => {
    await expect(assertSafeBaseUrl(raw)).resolves.toBeUndefined();
    expect(dns.lookup).not.toHaveBeenCalled();
  });
});

describe("assertSafeBaseUrl on a host name", () => {
  const answer = (...addresses: [string, number][]) => {
    dns.lookup.mockResolvedValue(addresses.map(([address, family]) => ({ address, family })));
  };

  it("resolves the host name with every address and accepts public ones", async () => {
    answer(["93.184.216.34", 4], ["2606:2800:220:1::1", 6]);

    await expect(assertSafeBaseUrl("https://API.Example.com:8443/v1")).resolves.toBeUndefined();
    expect(dns.lookup).toHaveBeenCalledExactlyOnceWith("api.example.com", { all: true });
  });

  it("looks up a trailing-dot name as it is written", async () => {
    answer(["8.8.8.8", 4]);

    await assertSafeBaseUrl("http://localhost./");
    expect(dns.lookup).toHaveBeenCalledWith("localhost.", { all: true });
  });

  it.each([
    ["a loopback address", [["127.0.0.1", 4]]],
    ["an IPv6 loopback address", [["::1", 6]]],
    [
      "a private address after a public one",
      [
        ["8.8.8.8", 4],
        ["10.0.0.5", 4],
      ],
    ],
    [
      "a unique-local IPv6 address",
      [
        ["8.8.8.8", 4],
        ["fd00::5", 6],
      ],
    ],
    ["a mapped private address", [["::ffff:192.168.1.1", 6]]],
  ] as [string, [string, number][]][])(
    "refuses a name resolving to %s",
    async (_what, addresses) => {
      answer(...addresses);

      expect(await refusal("http://internal.example.com/")).toBe(NOT_ALLOWED);
    },
  );

  it("accepts a name resolving to a private address where private ranges are allowed", async () => {
    allowPrivate("true");
    answer(["10.0.0.5", 4], ["::1", 6]);

    await expect(assertSafeBaseUrl("http://ollama.local:11434/")).resolves.toBeUndefined();
  });

  it.each([
    ["the metadata address", [["169.254.169.254", 4]]],
    ["an IPv6 link-local address", [["fe80::1", 6]]],
    ["the unspecified address", [["0.0.0.0", 4]]],
  ] as [string, [string, number][]][])(
    "refuses a name resolving to %s where private ranges are allowed",
    async (_what, addresses) => {
      allowPrivate("true");
      answer(...addresses);

      expect(await refusal("http://metadata.example.com/")).toBe(NOT_ALLOWED);
    },
  );

  it("checks an address the lookup reports with another family as IPv6", async () => {
    answer(["::1", 0]);

    expect(await refusal("http://odd.example.com/")).toBe(NOT_ALLOWED);
  });

  it("accepts a name the lookup answers with no address", async () => {
    answer();

    await expect(assertSafeBaseUrl("http://empty.example.com/")).resolves.toBeUndefined();
  });

  it.each([
    ["ENOTFOUND", Object.assign(new Error("getaddrinfo ENOTFOUND x"), { code: "ENOTFOUND" })],
    ["a plain rejection", "boom"],
  ])("refuses a name whose lookup fails with %s", async (_what, failure) => {
    dns.lookup.mockRejectedValue(failure);

    expect(await refusal("http://nowhere.example.com/")).toBe("baseUrl host could not be resolved");
  });

  it("refuses a name whose lookup takes longer than 3 seconds", async () => {
    vi.useFakeTimers();
    dns.lookup.mockReturnValue(new Promise(() => undefined));
    const outcome = refusal("http://slow.example.com/");
    let settled = false;
    void outcome.then(() => {
      settled = true;
    });

    await vi.advanceTimersByTimeAsync(2999);
    expect(settled).toBe(false);
    await vi.advanceTimersByTimeAsync(1);
    expect(await outcome).toBe("baseUrl host could not be resolved");
  });

  it("accepts a lookup that answers just before the timeout", async () => {
    vi.useFakeTimers();
    dns.lookup.mockReturnValue(
      new Promise((resolve) =>
        setTimeout(() => resolve([{ address: "8.8.8.8", family: 4 }]), 2999),
      ),
    );
    const outcome = assertSafeBaseUrl("http://slowish.example.com/");

    await vi.advanceTimersByTimeAsync(2999);
    await expect(outcome).resolves.toBeUndefined();
  });
});
