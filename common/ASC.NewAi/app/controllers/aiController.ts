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

import type { Response } from "express";
import { AIEngine } from "@onlyoffice/ai-chat/core";
import type {
  SendInput,
  SendCustomInput,
  SendStreamInput,
  RegenerateStreamInput,
  ApproveToolCallInput,
  DenyToolCallInput,
} from "@onlyoffice/ai-chat/core";
import logger from "../log.js";
import { storage } from "../storage/index.js";
import { asyncHandler, streamNdjson } from "./_helpers.js";
import { isObject } from "../narrow.js";

// Client-side code passes `actionArgs.signal: AbortSignal` so it can
// cancel an in-flight stream. Going through JSON the signal collapses
// to `{}` — provider SDKs then crash with
// `signal.addEventListener is not a function`. Replace it with a real
// AbortSignal wired to the HTTP request so client disconnects abort
// the upstream call too.
function responseAbortSignal(res: Response): AbortSignal {
  const controller = new AbortController();
  // Fires both on early client disconnect and on normal response end.
  // Use `writableEnded` to filter out the normal-completion case so we
  // only propagate real cancellations to the upstream provider.
  res.on("close", () => {
    if (!res.writableEnded && !controller.signal.aborted) {
      controller.abort();
    }
  });
  return controller.signal;
}

function withRequestSignal<T>(res: Response, body: T): T {
  if (!isObject(body)) {
    return body;
  }
  const actionArgs = body["actionArgs"];
  if (!isObject(actionArgs)) {
    return body;
  }
  return {
    ...body,
    actionArgs: { ...actionArgs, signal: responseAbortSignal(res) },
  };
}

const engine = new AIEngine({ storage });

function isAsyncIterable(value: unknown): value is AsyncIterable<unknown> {
  if (typeof value !== "object" || value === null) {
    return false;
  }
  if (!(Symbol.asyncIterator in value)) {
    return false;
  }
  return typeof Reflect.get(value, Symbol.asyncIterator) === "function";
}

function describeChatError(error: unknown): string {
  if (!isObject(error)) {
    return String(error);
  }
  const parts: string[] = [];
  if (typeof error["code"] === "string") {
    parts.push(`code=${error["code"]}`);
  }
  if (typeof error["message"] === "string") {
    parts.push(`message=${error["message"]}`);
  }
  if (isObject(error["cause"])) {
    parts.push(`cause=(${describeChatError(error["cause"])})`);
  }
  return parts.length > 0 ? parts.join(", ") : JSON.stringify(error);
}

// Sniff stream events to surface provider failures in server logs. The
// engine emits `message-incomplete` with `status.reason === "error"` and a
// `ChatErrorPayload` instead of throwing, so without this tap the cause of
// a broken stream is invisible from the server side — it only shows up in
// the browser DevTools network tab.
async function* logStreamErrors<T>(
  route: string,
  iter: AsyncIterable<T>,
): AsyncIterable<T> {
  let eventCount = 0;
  try {
    for await (const event of iter) {
      eventCount += 1;
      if (isObject(event) && event["type"] === "message-incomplete") {
        const message = isObject(event["message"]) ? event["message"] : null;
        const status = message && isObject(message["status"]) ? message["status"] : null;
        if (status && status["reason"] === "error") {
          logger.warn(
            `${route}: provider returned message-incomplete — ${describeChatError(status["error"])}`,
          );
        } else if (status) {
          logger.warn(
            `${route}: message-incomplete reason=${String(status["reason"])}`,
          );
        }
      }
      yield event;
    }
  } catch (err) {
    logger.error(
      `${route}: stream threw after ${eventCount} event(s): ${
        err instanceof Error ? `${err.message}\n${err.stack}` : String(err)
      }`,
    );
    throw err;
  }
}

export const aiController = {
  send: asyncHandler<SendInput>(async (req, res) => {
    const result = await engine.send(req.body);
    res.json(result);
  }),

  sendCustom: asyncHandler<SendCustomInput>(async (req, res) => {
    const body = withRequestSignal(res, req.body);
    const result = engine.sendCustom(body);
    if (body.isStream) {
      if (isAsyncIterable(result)) {
        await streamNdjson(res, logStreamErrors("ai/send-custom", result));
      } else {
        res.json(await result);
      }
      return;
    }
    if (isAsyncIterable(result)) {
      await streamNdjson(res, logStreamErrors("ai/send-custom", result));
      return;
    }
    res.json(await result);
  }),

  sendWithStream: asyncHandler<SendStreamInput>(async (req, res) => {
    const body = withRequestSignal(res, req.body);
    await streamNdjson(
      res,
      logStreamErrors("ai/send-with-stream", engine.sendWithStream(body)),
    );
  }),

  regenerateStream: asyncHandler<RegenerateStreamInput>(async (req, res) => {
    const body = withRequestSignal(res, req.body);
    await streamNdjson(
      res,
      logStreamErrors("ai/regenerate-stream", engine.regenerateStream(body)),
    );
  }),

  approveToolCall: asyncHandler<ApproveToolCallInput>(async (req, res) => {
    const body = withRequestSignal(res, req.body);
    await streamNdjson(
      res,
      logStreamErrors("ai/approve-tool-call", engine.approveToolCall(body)),
    );
  }),

  denyToolCall: asyncHandler<DenyToolCallInput>(async (req, res) => {
    const body = withRequestSignal(res, req.body);
    await streamNdjson(
      res,
      logStreamErrors("ai/deny-tool-call", engine.denyToolCall(body)),
    );
  }),
};
