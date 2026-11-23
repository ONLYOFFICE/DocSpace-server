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

import { customSync } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { docspace, docspaceError, http, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { shouldForwardHeadersToProvider } from "../../requestContext.js";
import { AiServiceHttpError } from "../../storage/httpClient.js";
import { storage } from "../../storage/index.js";
import { extractText, getSuggestedQuestions, resolveFormsProfile } from "../suggestedQuestions.js";

vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@onlyoffice/ai-chat/core")>()),
  customSync: vi.fn(),
}));

const upstream = useUpstream();

const PROFILE: Profile = {
  id: "p-forms",
  name: "Forms",
  providerType: "openai",
  baseUrl: "https://api.example.com/v1",
  modelId: "gpt-x",
};

// GET internal/ai/form-analysis/{attachmentId} answers `FormAnalysisDto` of
// products/ASC.AI/Server/Models/ResponseDto/FormAnalysisDto.cs.
const SCHEMA = {
  title: "Feedback",
  rowCount: 12,
  columns: [
    { name: "col_rating", label: "Rating", type: "number", values: ["1", "2", "3"] },
    { name: "comment", label: null, type: "text", values: null },
    { name: "submitted", type: "date", values: [] },
  ],
  culture: "ru-RU",
  cultureName: "Russian",
};
const GENERATE = { status: "generate", schema: SCHEMA, questions: null };

const EXPECTED_PROMPT =
  'Form: "Feedback" — 12 submissions collected.\n' +
  'Columns (name "label" (type) [allowed values]):\n' +
  '- col_rating "Rating" (number) [1/2/3]\n' +
  "- comment (text)\n" +
  "- submitted (date)\n\n" +
  "Write 4 starter questions an analyst would ask about these submissions.\n" +
  "- Every question must be answerable from the columns above.\n" +
  "- Prefer distributions, counts, comparisons and time trends over single-record lookups.\n" +
  "- Never invent a column or a value, and never repeat a question.\n" +
  '- "question": at most 60 characters, phrased as a plain button label a non-technical person ' +
  'understands. Refer to fields in everyday words; never put a raw field key or identifier (a "col_..." ' +
  "name, or a run-together/camelCase key) in it — rephrase it into readable words.\n" +
  '- "prompt": one sentence, the full request sent to the analysis assistant, naming the exact columns it needs.\n' +
  "- Write both fields in Russian (ru-RU).\n" +
  'Reply with exactly this shape: [{"question":"...","prompt":"..."}]';

const SYSTEM_PROMPT =
  "You generate starter analytics questions for a form-submission dataset.\n" +
  "Reply with a JSON array only. No prose, no markdown, no code fences, no reasoning.";

const Q1 = { question: "Average rating", prompt: "Average of col_rating." };
const Q2 = { question: "Comments", prompt: "Summarize comment." };

function answerAnalysis(body: unknown): void {
  upstream.use(http.get("*/internal/ai/form-analysis/:id", () => docspace(body as never)));
}

const saved: unknown[] = [];

function acceptSave(status = 200): void {
  upstream.use(
    http.post("*/internal/ai/form-analysis/:id/questions", async ({ request }) => {
      saved.push(await request.json());
      return status === 200 ? docspace(null) : docspaceError(status);
    }),
  );
}

function useProfile(
  formAnalysis: string | null,
  fallback: string | null = null,
  profile: Profile | null = PROFILE,
): void {
  vi.spyOn(storage.assignments, "readByType").mockImplementation(async (type) =>
    type === "FormAnalysis" ? formAnalysis : fallback,
  );
  vi.spyOn(storage.profiles, "readById").mockResolvedValue(profile as Profile);
}

function modelAnswers(content: unknown): void {
  vi.mocked(customSync).mockResolvedValue({ role: "assistant", content } as never);
}

/** Waits for the background job of the test to reach `done`, polling every millisecond. */
async function settled(done: () => boolean): Promise<void> {
  await vi.waitFor(
    () => {
      if (!done()) {
        throw new Error("not yet");
      }
    },
    { interval: 1, timeout: 1000 },
  );
}

beforeEach(() => {
  saved.length = 0;
  vi.mocked(customSync).mockRejectedValue(new Error("customSync was not expected to run"));
});

describe("extractText", () => {
  it.each<[string, unknown, string]>([
    ["null", null, ""],
    ["a string", "text", ""],
    ["an array", [{ type: "text", text: "a" }], ""],
    ["no content", { role: "assistant" }, ""],
    ["string content", { content: "hello" }, "hello"],
    ["object content", { content: { type: "text", text: "a" } }, ""],
    [
      "the text parts of a list, joined",
      {
        content: [
          { type: "text", text: "a" },
          { type: "reasoning", text: "r" },
          null,
          "b",
          { type: "text", text: 5 },
          { type: "text" },
          { type: "text", text: "c" },
        ],
      },
      "ac",
    ],
  ])("answers for %s", (_name, message, expected) => {
    expect(extractText(message)).toBe(expected);
  });
});

describe("resolveFormsProfile", () => {
  it("reads the FormAnalysis assignment first", async () => {
    useProfile("p-forms", "p-default");

    await expect(resolveFormsProfile()).resolves.toEqual(PROFILE);
    expect(vi.mocked(storage.assignments.readByType).mock.calls).toEqual([["FormAnalysis"]]);
    expect(storage.profiles.readById).toHaveBeenCalledWith("p-forms");
  });

  it("falls back to the Default assignment", async () => {
    useProfile(null, "p-default");

    await expect(resolveFormsProfile()).resolves.toEqual(PROFILE);
    expect(vi.mocked(storage.assignments.readByType).mock.calls).toEqual([
      ["FormAnalysis"],
      ["Default"],
    ]);
    expect(storage.profiles.readById).toHaveBeenCalledWith("p-default");
  });

  it("answers undefined for an empty FormAnalysis assignment without reading Default", async () => {
    vi.spyOn(storage.assignments, "readByType").mockImplementation(async (type) =>
      type === "FormAnalysis" ? "" : "p-default",
    );
    vi.spyOn(storage.profiles, "readById").mockResolvedValue(PROFILE);

    await expect(resolveFormsProfile()).resolves.toBeUndefined();
    expect(vi.mocked(storage.assignments.readByType).mock.calls).toEqual([["FormAnalysis"]]);
    expect(storage.profiles.readById).not.toHaveBeenCalled();
  });

  it("answers undefined when neither slot is assigned", async () => {
    useProfile(null, null);

    await expect(resolveFormsProfile()).resolves.toBeUndefined();
    expect(storage.profiles.readById).not.toHaveBeenCalled();
  });

  it("answers what the profile read answers for a missing profile", async () => {
    useProfile("gone", null, null);

    await expect(resolveFormsProfile()).resolves.toBeNull();
  });
});

describe("getSuggestedQuestions", () => {
  it("answers unavailable without a model call", async () => {
    answerAnalysis({ status: "unavailable" });

    await expect(getSuggestedQuestions("a1")).resolves.toEqual({
      status: "unavailable",
      questions: [],
    });
    expect(customSync).not.toHaveBeenCalled();
  });

  it("answers an analysis without a usable schema as unavailable", async () => {
    answerAnalysis({ status: "generate", schema: { title: "T" } });

    await expect(getSuggestedQuestions("a1")).resolves.toEqual({
      status: "unavailable",
      questions: [],
    });
  });

  it("answers the cached questions", async () => {
    answerAnalysis({ status: "ready", questions: [Q1, Q2] });

    await expect(getSuggestedQuestions("a1")).resolves.toEqual({
      status: "ready",
      questions: [Q1, Q2],
    });
    expect(customSync).not.toHaveBeenCalled();
    expect(upstream.seen.map((r) => r.path)).toEqual(["/internal/ai/form-analysis/a1"]);
  });

  it.each([403, 500])("rethrows the read failing with %i", async (status) => {
    upstream.use(http.get("*/internal/ai/form-analysis/:id", () => docspaceError(status)));

    const error = await getSuggestedQuestions("a1").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect((error as AiServiceHttpError).status).toBe(status);
    expect(customSync).not.toHaveBeenCalled();
  });

  it("answers pending at once and saves the generated questions in the background", async () => {
    answerAnalysis(GENERATE);
    acceptSave();
    useProfile("p-forms");
    modelAnswers(JSON.stringify([Q1, Q2]));

    const { result, forward } = await inRequestContext({ cookie: "asc_auth_key=K" }, async () => {
      const first = await getSuggestedQuestions("att/1");
      await settled(() => saved.length > 0);
      return { result: first, forward: shouldForwardHeadersToProvider() };
    });

    expect(result).toEqual({ status: "pending", questions: [] });
    expect(forward).toBe(true);
    expect(customSync).toHaveBeenCalledTimes(1);
    expect(customSync).toHaveBeenCalledWith({
      profile: PROFILE,
      messages: [{ role: "user", content: EXPECTED_PROMPT }],
      systemPrompt: SYSTEM_PROMPT,
      signal: expect.any(AbortSignal),
    });
    expect(saved).toEqual([{ questions: [Q1, Q2] }]);
    const save = upstream.seen.find((r) => r.method === "POST");
    expect(save).toMatchObject({ path: "/internal/ai/form-analysis/att%2F1/questions" });
    expect(save?.headers["cookie"]).toBe("asc_auth_key=K");
  });

  it("runs one generation for two requests of the same form, and a new one after it settled", async () => {
    answerAnalysis(GENERATE);
    acceptSave();
    useProfile("p-forms");
    let release: (value: unknown) => void = () => undefined;
    vi.mocked(customSync).mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          release = resolve as (value: unknown) => void;
        }),
    );

    await expect(getSuggestedQuestions("a1")).resolves.toEqual({
      status: "pending",
      questions: [],
    });
    await expect(getSuggestedQuestions("a1")).resolves.toEqual({
      status: "pending",
      questions: [],
    });
    await settled(() => vi.mocked(customSync).mock.calls.length === 1);
    expect(customSync).toHaveBeenCalledTimes(1);

    release({ role: "assistant", content: JSON.stringify([Q1]) });
    await settled(() => saved.length === 1);

    modelAnswers(JSON.stringify([Q2]));
    await getSuggestedQuestions("a1");
    await settled(() => saved.length === 2);
    expect(saved).toEqual([{ questions: [Q1] }, { questions: [Q2] }]);
  });

  // The single-flight map is keyed by the attachment id: a request that arrives while a job for
  // the id is running joins it.
  it("lets a second caller of the same attachment id join the first caller's job", async () => {
    answerAnalysis(GENERATE);
    acceptSave();
    useProfile("p-forms");
    let release: (value: unknown) => void = () => undefined;
    vi.mocked(customSync).mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          release = resolve as (value: unknown) => void;
        }),
    );

    await inRequestContext({ cookie: "asc_auth_key=A" }, () => getSuggestedQuestions("a1"));
    await inRequestContext({ cookie: "asc_auth_key=B" }, () => getSuggestedQuestions("a1"));
    await settled(() => vi.mocked(customSync).mock.calls.length === 1);
    release({ role: "assistant", content: JSON.stringify([Q1]) });
    await settled(() => saved.length === 1);

    expect(customSync).toHaveBeenCalledTimes(1);
    expect(
      upstream.seen.filter((r) => r.method === "POST").map((r) => r.headers["cookie"]),
    ).toEqual(["asc_auth_key=A"]);
  });

  it("runs separate generations for different forms", async () => {
    answerAnalysis(GENERATE);
    acceptSave();
    useProfile("p-forms");
    modelAnswers(JSON.stringify([Q1]));

    await getSuggestedQuestions("a1");
    await getSuggestedQuestions("a2");
    await settled(() => saved.length === 2);

    expect(customSync).toHaveBeenCalledTimes(2);
    expect(
      upstream.seen
        .filter((r) => r.method === "POST")
        .map((r) => r.path)
        .sort(),
    ).toEqual([
      "/internal/ai/form-analysis/a1/questions",
      "/internal/ai/form-analysis/a2/questions",
    ]);
  });

  it("does not call the model when no profile is configured", async () => {
    answerAnalysis(GENERATE);
    useProfile(null, null);

    await expect(getSuggestedQuestions("a1")).resolves.toEqual({
      status: "pending",
      questions: [],
    });
    await settled(() => vi.mocked(storage.assignments.readByType).mock.calls.length === 2);

    expect(customSync).not.toHaveBeenCalled();
    expect(upstream.seen.filter((r) => r.method === "POST")).toEqual([]);
  });

  it.each<[string, unknown, string]>([
    ["a model failure", new Error("provider 429"), "provider 429"],
    ["a non-Error model failure", "gone", "gone"],
  ])("warns about %s and saves nothing", async (_name, failure, text) => {
    answerAnalysis(GENERATE);
    useProfile("p-forms");
    vi.mocked(customSync).mockRejectedValue(failure);

    await getSuggestedQuestions("a1");
    await settled(() => vi.mocked(logger.warn).mock.calls.length > 0);

    expect(logger.warn).toHaveBeenCalledWith(
      `suggestedQuestions: generation failed for attachment a1: ${text}`,
    );
    expect(upstream.seen.filter((r) => r.method === "POST")).toEqual([]);
  });

  it("warns when the questions cannot be saved", async () => {
    answerAnalysis(GENERATE);
    acceptSave(500);
    useProfile("p-forms");
    modelAnswers(JSON.stringify([Q1]));

    await getSuggestedQuestions("a1");
    await settled(() => vi.mocked(logger.warn).mock.calls.length > 0);

    expect(logger.warn).toHaveBeenCalledWith(
      expect.stringMatching(
        /^suggestedQuestions: generation failed for attachment a1: AI service 500 /,
      ),
    );
  });

  it.each<[string, unknown, string]>([
    ["an assignment read failing", new Error("assignments down"), "assignments down"],
    ["a non-Error failure of the assignment read", "down", "down"],
  ])(
    "warns about %s as a failed background job and lets the next call retry",
    async (_name, failure, text) => {
      answerAnalysis(GENERATE);
      acceptSave();
      vi.spyOn(storage.assignments, "readByType").mockRejectedValueOnce(failure);

      await getSuggestedQuestions("a1");
      await settled(() => vi.mocked(logger.warn).mock.calls.length > 0);

      expect(logger.warn).toHaveBeenCalledWith(
        `suggestedQuestions: background generation failed for attachment a1: ${text}`,
      );
      expect(customSync).not.toHaveBeenCalled();

      vi.spyOn(storage.assignments, "readByType").mockResolvedValue("p-forms");
      vi.spyOn(storage.profiles, "readById").mockResolvedValue(PROFILE);
      modelAnswers(JSON.stringify([Q1]));
      await getSuggestedQuestions("a1");
      await settled(() => saved.length === 1);
    },
  );

  describe("the model answer", () => {
    const long = (n: number) => "x".repeat(n);

    it.each<[string, unknown, unknown[]]>([
      ["a bare JSON array", JSON.stringify([Q1, Q2]), [Q1, Q2]],
      [
        "an array inside prose and a code fence",
        `Sure!\n\`\`\`json\n${JSON.stringify([Q1])}\n\`\`\`\nDone.`,
        [Q1],
      ],
      [
        "the text after the last </think>",
        `<think>[1]</think> <think>[2]</think>${JSON.stringify([Q2])}`,
        [Q2],
      ],
      [
        "parts of a message",
        [
          { type: "text", text: "[" },
          { type: "text", text: `${JSON.stringify(Q1)}]` },
        ],
        [Q1],
      ],
      [
        "trimmed fields",
        JSON.stringify([{ question: "  Q  ", prompt: " P " }]),
        [{ question: "Q", prompt: "P" }],
      ],
      [
        "items that are not objects or lack a field left out",
        JSON.stringify([
          null,
          "q",
          [Q1],
          { question: "q" },
          { prompt: "p" },
          { question: 1, prompt: "p" },
          { question: " ", prompt: "p" },
          Q2,
        ]),
        [Q2],
      ],
      [
        "fields at the length limits kept, over them left out",
        JSON.stringify([
          { question: long(121), prompt: "p" },
          { question: "q", prompt: long(321) },
          { question: long(120), prompt: long(320) },
        ]),
        [{ question: long(120), prompt: long(320) }],
      ],
      [
        "only the first four",
        JSON.stringify([Q1, Q2, Q1, { question: "bad" }, Q2, { question: "5th", prompt: "p" }]),
        [Q1, Q2, Q1, Q2],
      ],
    ])("saves the questions of %s", async (_name, content, expected) => {
      answerAnalysis(GENERATE);
      acceptSave();
      useProfile("p-forms");
      modelAnswers(content);

      await getSuggestedQuestions("a1");
      await settled(() => saved.length > 0);

      expect(saved).toEqual([{ questions: expected }]);
    });

    it.each<[string, unknown]>([
      ["no array", "I cannot help with that."],
      ["brackets in the wrong order", "] then ["],
      ["an array that is not JSON", "[{question: 'q'}]"],
      ["an array with no usable item", JSON.stringify([{ question: "q" }])],
      ["an empty array", "[]"],
      ["object content", { text: JSON.stringify([Q1]) }],
      ["nothing after </think>", `<think>${JSON.stringify([Q1])}</think>`],
    ])("saves nothing for %s", async (_name, content) => {
      answerAnalysis(GENERATE);
      acceptSave();
      useProfile("p-forms");
      modelAnswers(content);

      await getSuggestedQuestions("a1");
      await settled(() => vi.mocked(customSync).mock.calls.length === 1);
      await new Promise((resolve) => setImmediate(resolve));

      expect(saved).toEqual([]);
      expect(logger.warn).not.toHaveBeenCalled();
    });
  });

  describe("time budget", () => {
    beforeEach(() => {
      vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    });
    afterEach(() => {
      vi.useRealTimers();
    });

    it("aborts the model call after 45 seconds and saves nothing", async () => {
      answerAnalysis(GENERATE);
      acceptSave();
      useProfile("p-forms");
      let signal: AbortSignal | undefined;
      vi.mocked(customSync).mockImplementation(
        (args) =>
          new Promise((_resolve, reject) => {
            signal = args.signal;
            args.signal?.addEventListener("abort", () => reject(new Error("aborted by budget")));
          }),
      );

      const timers = vi.spyOn(globalThis, "setTimeout");

      await getSuggestedQuestions("a1");
      // vi.waitFor moves the fake clock by its interval on every check, so the budget is read
      // from the timer the job set, not from the clock.
      await vi.waitFor(() => expect(signal).toBeDefined(), { interval: 1, timeout: 1000 });
      expect(signal?.aborted).toBe(false);
      expect(timers).toHaveBeenCalledWith(expect.any(Function), 45_000);
      await vi.advanceTimersByTimeAsync(45_000);

      expect(signal?.aborted).toBe(true);
      await vi.waitFor(
        () =>
          expect(logger.warn).toHaveBeenCalledWith(
            "suggestedQuestions: generation failed for attachment a1: aborted by budget",
          ),
        { interval: 1, timeout: 1000 },
      );
      expect(saved).toEqual([]);
    });

    it("clears the budget timer once the model answered", async () => {
      answerAnalysis(GENERATE);
      acceptSave();
      useProfile("p-forms");
      modelAnswers(JSON.stringify([Q1]));

      await getSuggestedQuestions("a1");
      await vi.waitFor(() => expect(saved).toHaveLength(1), { interval: 1, timeout: 1000 });

      expect(vi.getTimerCount()).toBe(0);
    });
  });
});
