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

// How much of a refused answer is read: enough for any error envelope and the log line built
// from it, while a body of a gigabyte from a provider or a gateway is cut off at the start.
const MAX_ERROR_BODY_BYTES = 64 * 1024;

/**
 * The body of an answer that was refused, for the error object and the log, read up to
 * `MAX_ERROR_BODY_BYTES` (ts-ai-failure-paths.md §7). The status has already decided the outcome,
 * so a body that cannot be read is an empty text, not a second failure that would hide the first.
 */
export async function errorBodyText(res: Response): Promise<string> {
  const pending = readTextCapped(res, MAX_ERROR_BODY_BYTES);
  try {
    return await pending;
  } catch {
    return "";
  }
}

/**
 * A refused answer as a log line names it: the status, the content type and how much body came
 * with it. The body itself is another party's text and may carry a key, a header or the request
 * it answers, so it is read (which frees the connection) and left out
 * (ts-ai-style.md, "State and logging").
 */
export async function refusalForLog(res: Response): Promise<string> {
  const body = await errorBodyText(res);
  const type = res.headers.get("content-type") ?? "no content-type";
  return `${res.status} ${type}, ${body.length} characters of body`;
}

/**
 * The body decoded as UTF-8, at most `limit` bytes of it. The stream is
 * cancelled once the limit is reached, so a body far larger than the limit
 * costs neither the download nor the memory beyond that point.
 */
export async function readTextCapped(res: Response, limit: number): Promise<string> {
  if (!res.body) {
    return "";
  }
  const reader = res.body.getReader();
  const decoder = new TextDecoder("utf-8");
  let text = "";
  let received = 0;
  try {
    while (received < limit) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }
      const room = limit - received;
      const chunk = value.length > room ? value.subarray(0, room) : value;
      received += chunk.length;
      text += decoder.decode(chunk, { stream: true });
    }
  } finally {
    // Past the limit, or on an error: drop the rest of the download.
    await cancelReader(reader);
  }
  return text + decoder.decode();
}

// The read is over either way: a stream that refuses to be cancelled changes nothing for the
// caller, which already has its answer or its error.
export async function cancelReader(reader: { cancel(): Promise<void> }): Promise<void> {
  const pending = reader.cancel();
  try {
    await pending;
  } catch {
    // nothing to do: see above
  }
}
