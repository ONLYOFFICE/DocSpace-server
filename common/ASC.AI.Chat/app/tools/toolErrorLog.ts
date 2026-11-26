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

import { isObject } from "../narrow.js";

// What a log line may say about a tool call that failed (ts-ai-style.md, "State and logging").
// The text of such a failure is partly another server's: the library words a refused HTTP answer
// of an MCP server as `<method>: HTTP <status> <reason>: <up to 500 characters of the body>` and
// a JSON-RPC error as `MCP tool error (<code>): <the server's message>` or
// `initialize refused (<code>): <the server's message>`. What follows the status or the code is
// that server's to word and may quote the request, so it stays out of the log.
const HTTP_REFUSAL = /^(?:[^\n:]+: )?HTTP \d{3}[^\n:]*/;
const JSON_RPC_ERROR = /^(?:MCP tool error|initialize refused) \(-?\d+\)/;

const LINE_LIMIT = 300;

/** The last line of a text that has one, cut at 300 characters. */
export function lastLine(text: string): string {
  const lines = text
    .split("\n")
    .map((line) => line.trim())
    .filter((line) => line.length > 0);
  const last = lines[lines.length - 1] ?? "<no error text>";
  return last.slice(0, LINE_LIMIT);
}

/**
 * The error of a tool result as a log line names it. A text in one of the library's forms above
 * is cut after the status or the code. Any other text is the engine's or the library's own
 * wording ("unknown tool", an image profile that is not set up) and keeps its last line. An
 * error object is named by its `code` and, read the same way, its `message`; anything else by
 * its type alone.
 */
export function toolErrorForLog(error: unknown): string {
  if (typeof error === "string") {
    return (
      HTTP_REFUSAL.exec(error)?.[0].trimEnd() ?? JSON_RPC_ERROR.exec(error)?.[0] ?? lastLine(error)
    );
  }
  if (isObject(error)) {
    const code = error["code"];
    const message = error["message"];
    const named = [
      typeof code === "string" || typeof code === "number" ? `code=${code}` : undefined,
      typeof message === "string" ? toolErrorForLog(message) : undefined,
    ].filter((part) => part !== undefined);
    return named.length > 0 ? named.join(" ") : "an error object";
  }
  return `an error of type ${typeof error}`;
}
