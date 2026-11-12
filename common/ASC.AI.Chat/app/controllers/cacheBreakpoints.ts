// SPDX-License-Identifier: AGPL-3.0-only

import { isObject } from "../narrow.js";

// Anthropic-style prompt-caching breakpoints for the OpenAI-format bodies the
// plugin sends through the ONLYOFFICE route. The Claude backend behind the
// route reuses a cached prefix only when the request marks where it ends; the
// marker travels as a `cache_control` field on a content part. This mirrors
// `providers/cache-control.ts` in `@onlyoffice/ai-chat`, which runs on typed
// input — here the input is untrusted JSON, so every shape is checked rather
// than dereferenced.

const CACHE_CONTROL = { type: "ephemeral" } as const;

// Mark one message: a string content becomes a single text part carrying
// `cache_control`, an array content gets it on its LAST text part, and a
// message with no text at all (an empty string, images only) is returned as
// the same object, which lets the caller tell "nothing to mark" apart.
export function withCacheControl(message: Record<string, unknown>): Record<string, unknown> {
  const content = message["content"];
  if (typeof content === "string") {
    if (!content) {
      return message;
    }
    return {
      ...message,
      content: [{ type: "text", text: content, cache_control: CACHE_CONTROL }],
    };
  }
  if (!Array.isArray(content)) {
    return message;
  }
  let lastText = -1;
  for (let i = content.length - 1; i >= 0; i--) {
    const part = content[i];
    if (isObject(part) && part["type"] === "text") {
      lastText = i;
      break;
    }
  }
  if (lastText === -1) {
    return message;
  }
  return {
    ...message,
    content: content.map((part, i) =>
      i === lastText && isObject(part) ? { ...part, cache_control: CACHE_CONTROL } : part,
    ),
  };
}

// Two breakpoints on the message list: the leading system prompt, and the
// last message of the history that can carry one — the static prefix the
// Claude backend reuses across turns. Same two positions as the library.
//
// A `tool` message is never marked: in the chat-completion wire format it
// carries a plain string or `text` parts only, and the tool result itself is
// a single message not worth a breakpoint — the history before it still is.
// The assistant turn that issued the calls usually has no text either
// (`content: ""` next to `tool_calls`), so the breakpoint keeps walking back,
// typically to the user turn that started the round; otherwise an agent loop
// would resend its whole history uncached on every round.
export function markCacheBreakpoints(messages: unknown[]): unknown[] {
  if (messages.length === 0) {
    return messages;
  }
  let last = -1;
  let marked: Record<string, unknown> | null = null;
  for (let i = messages.length - 1; i >= 0; i--) {
    const message = messages[i];
    if (!isObject(message) || message["role"] === "tool") {
      continue;
    }
    const candidate = withCacheControl(message);
    if (candidate !== message) {
      last = i;
      marked = candidate;
      break;
    }
  }
  return messages.map((message, i) => {
    if (i === last && marked) {
      return marked;
    }
    if (i === 0 && isObject(message) && message["role"] === "system") {
      return withCacheControl(message);
    }
    return message;
  });
}

// Whether the caller placed its own breakpoints, in which case the host adds
// none: the library's `OnlyOfficeProvider` and `OpenRouterProvider` mark the
// message list themselves, and layering the host's markers on top would add
// a third breakpoint and move one onto a message the library chose to skip.
//
// Through this route the plugin currently sends none: the host plugin maps an
// ONLYOFFICE profile to an External profile based on OpenAI, whose provider
// never marks — so today the host's markers are what every request gets. The
// check is for a plugin build that marks on this route; a marker that is
// `null` counts as "placed" and is forwarded as sent.
export function hasCacheBreakpoints(messages: unknown[]): boolean {
  return messages.some((message) => {
    if (!isObject(message) || !Array.isArray(message["content"])) {
      return false;
    }
    return message["content"].some((part) => isObject(part) && "cache_control" in part);
  });
}
