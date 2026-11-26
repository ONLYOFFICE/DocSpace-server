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

import { describe, expect, it } from "vitest";
import { corsOptions } from "../cors.js";

describe("corsOptions", () => {
  it.each([undefined, ""])("is off for %j", (value) => {
    expect(corsOptions(value)).toBeNull();
  });

  it("answers any origin without credentials for *", () => {
    expect(corsOptions("*")).toEqual({ origin: "*" });
  });

  it("answers one origin, lowercased, with credentials", () => {
    expect(corsOptions("https://Portal.Example.com")).toEqual({
      origin: ["https://portal.example.com"],
      credentials: true,
    });
  });

  it("keeps a port and a path of a single origin as they are", () => {
    expect(corsOptions("http://localhost:8092/x")).toEqual({
      origin: ["http://localhost:8092/x"],
      credentials: true,
    });
  });

  it("keeps a * that is not followed by a dot literal", () => {
    expect(corsOptions("https://*example.com")).toEqual({
      origin: ["https://*example.com"],
      credentials: true,
    });
  });

  describe("a wildcard subdomain pattern", () => {
    const patternOf = (value: string): RegExp => {
      const options = corsOptions(value);
      const origin = (options?.origin as unknown[] | undefined)?.[0];
      if (!(origin instanceof RegExp)) {
        throw new Error(`no pattern for ${value}`);
      }
      return origin;
    };

    it("turns into an anchored pattern with credentials", () => {
      const options = corsOptions("https://*.Example.com");

      expect(options).toEqual({
        origin: [/^https:\/\/(?:[^./]+\.)+example\.com$/],
        credentials: true,
      });
    });

    it.each([
      "https://a.example.com",
      "https://a.b.example.com",
      "https://a-1.example.com",
      "https://xn--80a.example.com",
      "https://A.example.com",
    ])("matches %s", (origin) => {
      expect(patternOf("https://*.example.com").test(origin)).toBe(true);
    });

    it.each([
      "https://example.com",
      "http://a.example.com",
      "https://a.example.com:8443",
      "https://a.example.com.evil.com",
      "https://evil-example.com",
      "https://aexample.com",
      "https://a.examplexcom",
      "https://.example.com",
      "https://a/.example.com",
      " https://a.example.com",
    ])("does not match %s", (origin) => {
      expect(patternOf("https://*.example.com").test(origin)).toBe(false);
    });

    it("keeps the port of the pattern", () => {
      const pattern = patternOf("https://*.example.com:8443");

      expect(pattern.test("https://a.example.com:8443")).toBe(true);
      expect(pattern.test("https://a.example.com")).toBe(false);
      expect(pattern.test("https://a.example.com:84430")).toBe(false);
    });

    it("escapes every regular-expression character of the pattern", () => {
      const pattern = patternOf("https://*.ex+ample(1).com/[x]?$^|{2}\\");

      expect(pattern.source).toBe(
        "^https:\\/\\/(?:[^./]+\\.)+ex\\+ample\\(1\\)\\.com\\/\\[x\\]\\?\\$\\^\\|\\{2\\}\\\\$",
      );
      expect(pattern.test("https://a.ex+ample(1).com/[x]?$^|{2}\\")).toBe(true);
    });

    it("drops whatever follows a second *. of the pattern", () => {
      expect(patternOf("https://*.example.com*.evil.com").source).toBe(
        "^https:\\/\\/(?:[^./]+\\.)+example\\.com$",
      );
    });

    it("matches no browser origin when the pattern has no scheme", () => {
      const pattern = patternOf("*.example.com");

      expect(pattern.test("https://a.example.com")).toBe(false);
      expect(pattern.test("a.example.com")).toBe(true);
    });
  });
});
