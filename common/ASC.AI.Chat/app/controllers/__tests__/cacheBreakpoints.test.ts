import { describe, expect, it } from "vitest";

import {
  hasCacheBreakpoints,
  markCacheBreakpoints,
  withCacheControl,
} from "../cacheBreakpoints.js";

const EPHEMERAL = { type: "ephemeral" };

const marks = (message: unknown): boolean[] => {
  const content = (message as { content: unknown }).content;
  if (!Array.isArray(content)) return [];
  return content.map((part) => "cache_control" in (part as object));
};

describe("withCacheControl", () => {
  it("turns a string content into one marked text part", () => {
    expect(withCacheControl({ role: "user", content: "hi" })).toEqual({
      role: "user",
      content: [{ type: "text", text: "hi", cache_control: EPHEMERAL }],
    });
  });

  it("marks the last text part of an array content", () => {
    const out = withCacheControl({
      role: "user",
      content: [
        { type: "text", text: "a" },
        { type: "text", text: "b" },
        { type: "image_url", image_url: { url: "data:," } },
      ],
    });
    expect(marks(out)).toEqual([false, true, false]);
  });

  it("returns the same object when there is nothing to mark", () => {
    const empty = { role: "assistant", content: "" };
    expect(withCacheControl(empty)).toBe(empty);
    const images = { role: "user", content: [{ type: "image_url", image_url: { url: "data:," } }] };
    expect(withCacheControl(images)).toBe(images);
    const odd = { role: "user", content: 42 };
    expect(withCacheControl(odd)).toBe(odd);
  });
});

describe("markCacheBreakpoints", () => {
  it("marks the leading system message and the last message", () => {
    const out = markCacheBreakpoints([
      { role: "system", content: "rules" },
      { role: "user", content: "one" },
      { role: "assistant", content: "two" },
      { role: "user", content: "three" },
    ]);
    expect(marks(out[0])).toEqual([true]);
    expect(out[1]).toEqual({ role: "user", content: "one" });
    expect(out[2]).toEqual({ role: "assistant", content: "two" });
    expect(marks(out[3])).toEqual([true]);
  });

  it("never marks a tool message, and walks back past an assistant turn with no text", () => {
    const toolCalls = [
      { id: "call_1", type: "function", function: { name: "t", arguments: "{}" } },
    ];
    const out = markCacheBreakpoints([
      { role: "system", content: "rules" },
      { role: "user", content: "run it" },
      { role: "assistant", content: "", tool_calls: toolCalls },
      { role: "tool", tool_call_id: "call_1", content: "ok" },
    ]);
    expect(marks(out[0])).toEqual([true]);
    expect(marks(out[1])).toEqual([true]);
    expect(out[2]).toEqual({ role: "assistant", content: "", tool_calls: toolCalls });
    expect(out[3]).toEqual({ role: "tool", tool_call_id: "call_1", content: "ok" });
  });

  it("marks a lone system message once and skips non-object entries", () => {
    const out = markCacheBreakpoints([{ role: "system", content: "rules" }, "junk"]);
    expect(marks(out[0])).toEqual([true]);
    expect(out[1]).toBe("junk");
    expect(markCacheBreakpoints([])).toEqual([]);
  });

  it("does not mutate the input", () => {
    const input = [
      { role: "system", content: "rules" },
      { role: "user", content: [{ type: "text", text: "hi" }] },
    ];
    const snapshot = JSON.parse(JSON.stringify(input));
    markCacheBreakpoints(input);
    expect(input).toEqual(snapshot);
  });
});

describe("hasCacheBreakpoints", () => {
  it("sees a marker on any content part", () => {
    expect(
      hasCacheBreakpoints([
        { role: "system", content: [{ type: "text", text: "s", cache_control: EPHEMERAL }] },
        { role: "user", content: "q" },
      ]),
    ).toBe(true);
  });

  it("sees none on string contents, unmarked parts or odd shapes", () => {
    expect(hasCacheBreakpoints([{ role: "user", content: "q" }])).toBe(false);
    expect(hasCacheBreakpoints([{ role: "user", content: [{ type: "text", text: "q" }] }])).toBe(
      false,
    );
    expect(hasCacheBreakpoints(["junk", { role: "user", content: ["junk"] }])).toBe(false);
    expect(hasCacheBreakpoints([])).toBe(false);
  });
});
