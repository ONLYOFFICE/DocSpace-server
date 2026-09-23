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

// Telemetry tap for the streaming chat routes; a no-op unless telemetry/sdk.ts
// started the SDK. Timing terminology matches the AI gateway (`aigw.timing.*`):
// `latency` = round start → first MODEL chunk (`message-start`, not the earlier
// `user-message-stored`), `generation` = first chunk → end of stream. The
// engine persists the assistant message before emitting `message-start`, so
// latency includes that one .NET round-trip.

import { context, metrics, trace, SpanStatusCode } from "@opentelemetry/api";
import { isObject } from "../narrow.js";

const tracer = trace.getTracer("asc-new-ai");
const meter = metrics.getMeter("asc-new-ai");

const latencyHistogram = meter.createHistogram("newai.chat.latency", {
  unit: "ms",
  description: "Time from round start to the first model chunk",
});

const generationHistogram = meter.createHistogram("newai.chat.generation", {
  unit: "ms",
  description: "Time from the first model chunk to the end of the stream",
});

const roundsCounter = meter.createCounter("newai.chat.rounds", {
  description: "Streaming chat rounds by terminal outcome",
});

// "chat" = ChatEvent stream; "openai" = chat.completion.chunk stream.
export type StreamDialect = "chat" | "openai";

function isFirstModelChunk(dialect: StreamDialect, event: unknown): boolean {
  if (!isObject(event)) {
    return false;
  }
  if (dialect === "openai") {
    return event["object"] === "chat.completion.chunk";
  }
  return event["type"] === "message-start";
}

// Terminal outcome carried by `event`, or null for non-terminal events.
function terminalOutcome(dialect: StreamDialect, event: unknown): string | null {
  if (!isObject(event)) {
    return null;
  }
  if (dialect === "openai") {
    if (isObject(event["error"])) {
      return "incomplete-error";
    }
    const choices = event["choices"];
    const first = Array.isArray(choices) && isObject(choices[0]) ? choices[0] : null;
    return first && typeof first["finish_reason"] === "string" ? "completed" : null;
  }
  switch (event["type"]) {
    case "message-end":
      return "completed";
    case "message-incomplete": {
      const message = isObject(event["message"]) ? event["message"] : null;
      const status = message && isObject(message["status"]) ? message["status"] : null;
      const reason = status && typeof status["reason"] === "string" ? status["reason"] : "unknown";
      return `incomplete-${reason}`;
    }
    case "tool-call-pending":
      return "tool-pending";
    default:
      return null;
  }
}

/**
 * Wrap a chat event stream with a round span and the latency / generation /
 * rounds metrics. The clock starts at the call, not on first pull; pulls run
 * under the span's context so the engine's outbound calls become children.
 */
export function observeChatStream<T>(
  route: string,
  iter: AsyncIterable<T>,
  dialect: StreamDialect = "chat",
): AsyncIterable<T> {
  const startedAt = performance.now();
  const span = tracer.startSpan(route, { attributes: { "newai.route": route } });
  const spanContext = trace.setSpan(context.active(), span);

  return (async function* () {
    const iterator = iter[Symbol.asyncIterator]();
    let firstChunkAt: number | null = null;
    // "none" = ended without a terminal event (e.g. client abort).
    let outcome = "none";
    try {
      for (;;) {
        const result = await context.with(spanContext, () => iterator.next());
        if (result.done) {
          break;
        }
        const event = result.value;
        if (firstChunkAt === null && isFirstModelChunk(dialect, event)) {
          firstChunkAt = performance.now();
          latencyHistogram.record(firstChunkAt - startedAt, { route });
          span.addEvent("gen_ai.first_token");
          span.setAttribute("newai.timing.latency_ms", Math.round(firstChunkAt - startedAt));
        }
        // The last terminal event wins (in-engine tool rounds emit several).
        outcome = terminalOutcome(dialect, event) ?? outcome;
        yield event;
      }
    } catch (err) {
      outcome = "stream-error";
      span.recordException(err instanceof Error ? err : new Error(String(err)));
      throw err;
    } finally {
      const endedAt = performance.now();
      if (firstChunkAt !== null) {
        generationHistogram.record(endedAt - firstChunkAt, { route });
        span.setAttribute("newai.timing.generation_ms", Math.round(endedAt - firstChunkAt));
      }
      roundsCounter.add(1, { route, outcome });
      span.setAttribute("newai.outcome", outcome);
      if (outcome === "incomplete-error" || outcome === "stream-error") {
        span.setStatus({ code: SpanStatusCode.ERROR });
      }
      span.end();
    }
  })();
}
