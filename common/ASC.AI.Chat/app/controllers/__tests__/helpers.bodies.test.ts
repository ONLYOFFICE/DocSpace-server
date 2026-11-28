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

// The body checks the controllers share: the attachment cap of a user message, the message
// the thread storage routes store, and the two argument shapes of the chat library.

import { describe, expect, it } from "vitest";
import {
  MAX_ATTACHMENTS_PER_REQUEST,
  attachmentLimitError,
  invalidMessageError,
  unpackPositional,
} from "../_helpers.js";

const file = { type: "file", data: "", mimeType: "{}" };
const image = { type: "image", image: "data:," };
const many = (part: unknown, count: number): unknown[] => Array.from({ length: count }, () => part);

describe("MAX_ATTACHMENTS_PER_REQUEST", () => {
  it("is 100", () => {
    expect(MAX_ATTACHMENTS_PER_REQUEST).toBe(100);
  });
});

describe("attachmentLimitError", () => {
  it.each([
    ["null", null],
    ["a string", "hi"],
    ["an array", [file]],
    ["a message without content", { role: "user" }],
    ["a string content", { role: "user", content: "hi" }],
    ["an object content", { role: "user", content: { 0: file } }],
    ["no parts", { role: "user", content: [] }],
    [
      "five files and five images",
      { role: "user", content: [...many(file, 5), ...many(image, 5)] },
    ],
    ["parts that are not objects", { role: "user", content: [...many("file", 9), null, 3] }],
    ["parts of other kinds", { role: "user", content: many({ type: "audio" }, 9) }],
  ])("finds nothing wrong with %s", (_what, message) => {
    expect(attachmentLimitError(message)).toBeNull();
  });

  it.each([
    [many(file, 6), "(got 6 file(s), 0 image(s))"],
    [many(image, 6), "(got 0 file(s), 6 image(s))"],
    [[...many(file, 6), ...many(image, 7)], "(got 6 file(s), 7 image(s))"],
    [[...many(file, 2), ...many(image, 6), "x"], "(got 2 file(s), 6 image(s))"],
  ])("refuses more than five of a kind %#", (content, counts) => {
    expect(attachmentLimitError({ role: "user", content })).toBe(
      `a message may carry at most 5 file and 5 image attachments ${counts}`,
    );
  });
});

describe("invalidMessageError", () => {
  const ROLE = "message.role must be one of user, assistant, system";
  const CONTENT =
    "message.content must be a non-empty string or a list with at least one non-empty part";

  it.each([
    ["undefined", undefined],
    ["null", null],
    ["a string", "hi"],
    ["a number", 3],
    ["an array", [{ role: "user", content: "hi" }]],
  ])("refuses a message that is %s", (_what, message) => {
    expect(invalidMessageError(message)).toBe("message is required and must be an object");
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["a number", 1],
    ["empty", ""],
    ["tool", "tool"],
    ["in another case", "User"],
  ])("refuses a role that is %s", (_what, role) => {
    expect(invalidMessageError({ role, content: "hi" })).toBe(ROLE);
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["a number", 5],
    ["an object", { type: "text", text: "hi" }],
    ["empty", ""],
    ["whitespace", " \n "],
    ["an empty list", []],
    ["blank strings", ["", "  "]],
    ["a blank text part", [{ type: "text", text: "  " }]],
    ["a text part with a number", [{ type: "text", text: 3 }]],
    ["a part without a type", [{ text: "hi" }]],
    ["a part with an empty type", [{ type: "", data: "x" }]],
    ["a part whose type is a number", [{ type: 1 }]],
    ["parts that are not objects", [null, 4, true]],
  ])("refuses content that is %s", (_what, content) => {
    expect(invalidMessageError({ role: "assistant", content })).toBe(CONTENT);
  });

  it.each([
    ["user", "hi"],
    ["assistant", ["  ok  "]],
    ["system", [{ type: "text", text: "rules" }]],
    ["assistant", [{ type: "tool-call", toolCallId: "c1", toolName: "t" }]],
    ["assistant", [{ type: "reasoning" }]],
    ["user", [{ type: "text", text: " " }, file]],
    ["user", [null, image]],
  ])("accepts a %s message with content %j", (role, content) => {
    expect(invalidMessageError({ role, content })).toBeNull();
  });
});

describe("unpackPositional", () => {
  it("answers nothing for a route without arguments", () => {
    expect(unpackPositional({ a: 1 }, [])).toEqual({});
  });

  describe("with one argument", () => {
    const names = ["threadId"] as const;

    it("reads a named object as it came, other keys included", () => {
      const body = { threadId: "t1", other: 2 };
      expect(unpackPositional(body, names)).toEqual(body);
    });

    it.each([
      ["a string", "t1"],
      ["a number", 5],
      ["false", false],
      ["an array", ["a", "b"]],
      ["an object without the name", { id: "t1" }],
    ])("wraps a body that is %s under the name", (_what, body) => {
      expect(unpackPositional(body, names)).toEqual({ threadId: body });
    });

    it.each([
      ["null", null],
      ["undefined", undefined],
    ])("answers nothing for %s", (_what, body) => {
      expect(unpackPositional(body, names)).toEqual({});
    });
  });

  describe("with several arguments", () => {
    const names = ["actionType", "profileId"] as const;

    it("maps a positional array onto the names", () => {
      expect(unpackPositional(["Chat", "p1", "extra"], names)).toEqual({
        actionType: "Chat",
        profileId: "p1",
      });
    });

    it("leaves a name without a position undefined", () => {
      const out = unpackPositional(["Chat"], names);
      expect(out).toEqual({ actionType: "Chat", profileId: undefined });
      expect(Object.keys(out)).toEqual(["actionType", "profileId"]);
    });

    it("reads a named object as it came, other keys included", () => {
      const body = { actionType: "Chat", stray: true };
      expect(unpackPositional(body, names)).toEqual(body);
    });

    it("keeps a __proto__ key of a parsed body as a field, not as the prototype", () => {
      const body: unknown = JSON.parse('{"actionType":"Chat","__proto__":{"profileId":"p9"}}');

      const out = unpackPositional(body, names);

      expect(out.profileId).toBeUndefined();
      expect(Object.getPrototypeOf(out)).toBe(Object.prototype);
      expect(Object.keys(out)).toEqual(["actionType", "__proto__"]);
    });

    it.each([
      ["null", null],
      ["undefined", undefined],
      ["a string", "Chat"],
      ["a number", 1],
    ])("answers nothing for %s", (_what, body) => {
      expect(unpackPositional(body, names)).toEqual({});
    });
  });
});
