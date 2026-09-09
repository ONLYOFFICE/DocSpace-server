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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { EXAMPLE_AT_ISO, EXAMPLE_AT_MS, EXAMPLE_IDS } from "../../app/exampleIds.js";

// Prose for the schemas derived from `@onlyoffice/ai-chat`, plus the cleanup
// of the TSDoc syntax that survives the derivation.
//
// `ts-json-schema-generator` copies the TSDoc of each type and property into
// `description`, so whatever the library declares without a doc comment
// reaches the document - and the generated SDK - undocumented. Those are
// OpenAPI lint findings (`schema-description`, `schema-property-description`)
// that cannot be closed at the true source from this repository: the types
// live in a published package.
//
// The prose therefore lives here, in the same spirit as `PARAM_DOCS` /
// `OPERATION_DOCS` in `app/openapi.ts`, whose engine entries are likewise
// distilled from the library's own JSDoc. Two rules keep the table honest:
//
//   - Fill-in only, never override. A description the library declares always
//     wins, so a future release that documents a type turns the entry here
//     dead rather than putting the two in conflict.
//   - A dead entry is reported rather than silently ignored, because the same
//     silence would also swallow a typo. See `applySchemaDocs`.
//
// Keys are the final, namespaced component names - exactly what the lint
// report cites (`components.schemas.AiChatEvent.properties.idx`).

interface SchemaDoc {
  /** Prose for the schema itself (`schema-description`). */
  description?: string;
  /** Prose per property name (`schema-property-description`). */
  properties?: Readonly<Record<string, string>>;
  /**
   * One realistic value per property name, emitted as the property's
   * `examples`. The library declares none, and a generated SDK's own docs plus
   * any assistant reading this document take their sample payloads from here,
   * so a placeholder (`"string"`, `0`) actively misleads. Same two rules as
   * `properties`: fill-in only, and a dead entry is reported.
   */
  examples?: Readonly<Record<string, unknown>>;
}

const SCHEMA_DOCS: Readonly<Record<string, SchemaDoc>> = {
  AiActionType: {
    description:
      "The AI action a request or an assignment applies to. Each action has its own assignment slot; `Default` is the profile used when an action's own slot is empty.",
  },

  /* --- Request-side types: what a caller has to construct --------------- */

  AiProfile: {
    examples: {
      id: EXAMPLE_IDS.profile,
      name: "OpenAI GPT-4o",
      providerType: "openai",
      basedOn: "openai",
      baseUrl: "https://api.openai.com/v1",
      key: "sk-your-provider-api-key",
      headers: { "X-Organization": "acme" },
      modelId: "gpt-4o",
      reasoning: false,
      capabilities: 7,
      canUseTool: true,
      useResponsesApi: false,
      isCloudProvider: true,
      useProxy: false,
      createdAt: EXAMPLE_AT_MS,
    },
  },

  AiCreateProfileInput: {
    examples: {
      name: "OpenAI GPT-4o",
      providerType: "openai",
      basedOn: "openai",
      baseUrl: "https://api.openai.com/v1",
      key: "sk-your-provider-api-key",
      headers: { "X-Organization": "acme" },
      modelId: "gpt-4o",
      reasoning: false,
      capabilities: 7,
      canUseTool: true,
      useResponsesApi: false,
      isCloudProvider: true,
      useProxy: false,
    },
  },

  AiThreadMessageLike: {
    examples: {
      id: EXAMPLE_IDS.message,
      role: "user",
      content: "Summarise the attached contract.",
      createdAt: EXAMPLE_AT_ISO,
      status: { type: "complete" },
      metadata: {},
      attachments: [EXAMPLE_IDS.attachment],
    },
  },

  AiAiSendStreamBody: {
    examples: {
      threadId: EXAMPLE_IDS.thread,
      userMessage: { role: "user", content: "Summarise the attached contract." },
      actionArgs: { isReasoning: false },
      entityId: EXAMPLE_IDS.room,
      profileId: EXAMPLE_IDS.profile,
    },
  },

  AiAiActionArgs: {
    examples: {
      tools: [],
      isReasoning: false,
      prompt: { mode: "append", text: "Answer in British English." },
    },
  },

  AiAiToolCallData: {
    examples: {
      threadId: EXAMPLE_IDS.thread,
      messageId: EXAMPLE_IDS.message,
      idx: 0,
      message: { role: "assistant", content: "" },
      actionArgs: { isReasoning: false },
      entityId: EXAMPLE_IDS.room,
      profileId: EXAMPLE_IDS.profile,
    },
  },

  AiPrompt: {
    examples: {
      id: EXAMPLE_IDS.prompt,
      name: "Contract summary",
      text: "Summarise the key obligations and dates in the attached contract.",
      folderId: EXAMPLE_IDS.promptFolder,
      createdAt: EXAMPLE_AT_MS,
      updatedAt: EXAMPLE_AT_MS,
    },
  },

  AiPromptFolder: {
    examples: {
      id: EXAMPLE_IDS.promptFolder,
      name: "Contract review",
      createdAt: EXAMPLE_AT_MS,
      updatedAt: EXAMPLE_AT_MS,
    },
  },

  AiTMCPItem: {
    examples: {
      name: "docspace_get_folder",
      description: "Read the contents of a DocSpace folder.",
      inputSchema: {
        type: "object",
        properties: { folderId: { type: "string" } },
        required: ["folderId"],
      },
      enabled: true,
      serverType: "docspace",
      requireApproval: false,
    },
  },

  AiWebSearchConfig: {
    examples: {
      provider: "exa",
      key: "your-web-search-api-key",
      baseUrl: "https://api.exa.ai",
      isCloudProvider: true,
      headers: {},
    },
  },

  /* --- Response-side types: what a caller has to parse ------------------ */

  AiErrorResponse: { examples: { error: "threadId required" } },
  AiSuccessResponse: { examples: { success: true } },

  AiThread: {
    examples: {
      threadId: EXAMPLE_IDS.thread,
      title: "Contract review",
      lastEditDate: EXAMPLE_AT_MS,
      profileId: EXAMPLE_IDS.profile,
    },
  },

  AiModel: {
    examples: {
      id: "gpt-4o",
      name: "GPT-4o",
      provider: "openai",
      reasoning: false,
      capabilities: 7,
    },
  },

  AiTProvider: {
    examples: {
      type: "openai",
      name: "OpenAI GPT-4o",
      key: "sk-your-provider-api-key",
      baseUrl: "https://api.openai.com/v1",
    },
  },

  AiAttachment: {
    examples: {
      id: EXAMPLE_IDS.attachment,
      kind: "file",
      source: "user",
      title: "contract.docx",
      content: "This agreement is made on 1 January 2026 between …",
      base64: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg==",
      path: "file_1234",
      type: 7,
      messageId: EXAMPLE_IDS.message,
      threadId: EXAMPLE_IDS.thread,
      entityId: EXAMPLE_IDS.room,
      createdAt: EXAMPLE_AT_MS,
      canAnalyze: false,
      formKeys: [],
    },
  },

  AiTErrorData: {
    description: "A field-scoped validation error: which form field was rejected, and why.",
    properties: {
      field: "The rejected field.",
      message: "The human-readable reason the field was rejected.",
    },
  },

  /* --- OpenAI-compatible streaming envelope ----------------------------- */

  AiOpenAIChatCompletionChunk: {
    description:
      "One `chat.completion.chunk` of an OpenAI-compatible streaming response. Only the fields this service can populate are emitted - an OpenAI client tolerates the rest as absent.",
    properties: {
      id: "The completion identifier, stable across every chunk of one response.",
      object: "Always `chat.completion.chunk`.",
      created: "When the completion started, in Unix seconds.",
      model: "The model that produced the completion - the resolved profile's model.",
      choices: "The choices carried by this chunk. This service emits exactly one.",
    },
  },

  AiOpenAIChunkChoice: {
    description: "One choice of a streaming completion, carrying the part this chunk adds.",
    properties: {
      index:
        "The zero-based position of the choice. This service emits a single choice, so always 0.",
      delta: "What this chunk adds to the choice.",
      finish_reason: "Why the completion stopped, or null while it is still streaming.",
    },
  },

  AiOpenAIChoiceDelta: {
    description:
      "The incremental part of one choice - what this chunk adds to the assistant message.",
    properties: {
      role: "Sent on the first chunk only, always `assistant`.",
      content: "The text this chunk appends. Null when the chunk carries no text.",
      tool_calls: "The tool calls the model requested, emitted in place of text.",
    },
  },

  AiOpenAIToolCallDelta: {
    description: "The incremental part of one tool call the model requested.",
    properties: {
      index: "The zero-based position of the tool call within the message.",
      id: "The tool call identifier, quoted back when its result is submitted.",
      type: "Always `function` - the only tool kind the API defines.",
      function: "The call itself: the function name and its JSON-encoded arguments.",
    },
  },

  AiOpenAIStreamError: {
    properties: {
      error:
        "The error that ended the stream: its message, type, code and the offending parameter.",
    },
  },

  /* --- Chat stream ------------------------------------------------------ */

  AiChatEvent: {
    examples: {
      type: "message-delta",
      messageId: EXAMPLE_IDS.message,
      idx: 0,
      threadId: EXAMPLE_IDS.thread,
      autoAllow: false,
      serverExecuted: false,
      title: "Contract review",
      profileId: EXAMPLE_IDS.profile,
    },
    properties: {
      message: "The message the event is about, in the state it has reached.",
      messageId: "The storage identifier of that message.",
      threadId: "The thread the event belongs to.",
      idx: "The zero-based position of the pending tool call within the message.",
      title: "The generated thread title.",
      profileId: "The profile that generated the title, when one was used.",
    },
  },

  /* --- Mutation outcomes ------------------------------------------------ */

  AiProfileMutationResult: {
    examples: { success: true },
    properties: {
      success: "True when the profile was persisted.",
      profile: "The persisted profile. Present on success.",
      error:
        "Why the profile was rejected - the name check or the provider credential check. Present on failure.",
    },
  },

  AiPromptMutationResult: {
    examples: { success: true },
    properties: {
      success: "True when the prompt was persisted.",
      prompt: "The persisted prompt. Present on success.",
      error: "Why the prompt was rejected. Present on failure.",
    },
  },

  AiFolderMutationResult: {
    examples: { success: true },
    properties: {
      success: "True when the folder was persisted.",
      folder: "The persisted folder. Present on success.",
      error: "Why the folder was rejected. Present on failure.",
    },
  },

  AiAssignmentMutationResult: {
    examples: { success: true },
    properties: {
      success: "True when the assignment was persisted.",
      error: "Why the assignment was rejected. Present on failure.",
    },
  },

  AiToolsMutationResult: {
    examples: { success: true },
    properties: {
      success: "True when the MCP server was persisted.",
      error: "Why the MCP server was rejected. Present on failure.",
    },
  },

  AiWebSearchMutationResult: {
    examples: { success: true },
    properties: {
      success: "True when the configuration was persisted.",
      config: "The persisted web-search configuration. Present on success.",
      error: "Why the configuration was rejected. Present on failure.",
    },
  },

  /* --- Bulk outcomes: on failure nothing at all was persisted ------------ */

  AiBulkAssignmentResult: {
    examples: { success: true, errors: [] },
    properties: {
      success: "True when every entry was persisted.",
      errors:
        "What was rejected, per action. Present on failure - and then no entry was persisted.",
    },
  },

  AiToolsBulkResult: {
    examples: { success: true, errors: [] },
    properties: {
      success: "True when every custom MCP server was persisted.",
      errors:
        "What was rejected, per server. Present on failure - and then no server was persisted.",
    },
  },

  AiImportResult: {
    examples: { success: true, imported: { folders: 2, prompts: 12 }, errors: [] },
    properties: {
      success: "True when the whole bundle was imported.",
      imported: "How many folders and prompts were created. Present on success.",
      errors: "What was rejected, per entry. Present on failure - and then nothing was imported.",
    },
  },

  AiImportError: {
    examples: {
      kind: "prompt",
      ref: EXAMPLE_IDS.prompt,
      error: "a prompt of that name already exists",
    },
    properties: {
      ref: "The offending entry - its name or its id.",
      error: "Why the entry was rejected.",
    },
  },

  /* --- Prompts and threads ---------------------------------------------- */

  AiCreatePromptInput: {
    examples: {
      name: "Contract summary",
      text: "Summarise the key obligations and dates in the attached contract.",
      folderId: EXAMPLE_IDS.promptFolder,
    },
    properties: {
      name: "The prompt name.",
      text: "The prompt body.",
      folderId:
        "The folder to file the prompt under. Omit or send null to leave it outside any folder.",
    },
  },

  AiPromptBundle: {
    examples: { version: 1, folders: [], prompts: [] },
    properties: {
      version: "The bundle format version, so an import can migrate an older export.",
      folders: "Every exported prompt folder.",
      prompts: "Every exported prompt.",
    },
  },

  AiOpenOrCreateResult: {
    properties: {
      threadId: "The thread that was opened, or the one just created.",
      priorMessages:
        "The messages already in the thread - empty for a thread that was just created.",
    },
  },

  AiResolvedAssignment: {
    properties: {
      profileId: "The identifier of the resolved profile.",
      profile: "The resolved profile itself.",
    },
  },
};

// `{@link Symbol}`, `{@link Symbol | label}` and `{@link Symbol label}` are
// TSDoc markup: an IDE renders them as a cross-reference, a reader of the API
// reference sees the braces verbatim. They also arrive space-mangled, because
// the generator strips the newlines a wrapped tag spanned. The symbol itself
// is worth keeping - it names the engine method an outcome belongs to - so it
// is unwrapped into code markup rather than dropped, and any label the tag
// carried wins over the symbol.
const TSDOC_LINK = /\{@link\s+([^}|\s]+)(?:\s*[|]\s*|\s+)?([^}]*)\}/g;

function unwrapTsdocLinks(text: string): string {
  return text
    .replace(TSDOC_LINK, (_match, symbol: string, label: string) => {
      const trimmed = label.trim();
      return trimmed.length > 0 ? trimmed : `\`${symbol}\``;
    })
    .replace(/[ \t]+/g, " ")
    .replace(/\s+([.,;:)])/g, "$1")
    .trim();
}

function isObject(node: unknown): node is Record<string, unknown> {
  return typeof node === "object" && node !== null && !Array.isArray(node);
}

// Unwrap TSDoc markup in every `description` of the tree, wherever it sits -
// a component, a property, an inlined operation schema or a union member.
function cleanDescriptions(node: unknown): unknown {
  if (Array.isArray(node)) {
    return node.map(cleanDescriptions);
  }
  if (!isObject(node)) {
    return node;
  }
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(node)) {
    out[key] =
      key === "description" && typeof value === "string"
        ? unwrapTsdocLinks(value)
        : cleanDescriptions(value);
  }
  return out;
}

/** One table entry that changed nothing - see the note above `SCHEMA_DOCS`. */
export interface UnusedSchemaDoc {
  schema: string;
  /** The property the entry described, or `undefined` for a type-level entry. */
  property?: string;
  reason: "no such schema" | "no such property" | "already described";
}

/**
 * Clean up the derived descriptions, then fill the gaps `SCHEMA_DOCS`
 * describes. Returns the entries that changed nothing, for the caller to
 * report - a dead entry is either a library release that now documents the
 * type (fine, delete it) or a typo (not fine).
 */
export function applySchemaDocs(components: Record<string, unknown>): {
  components: Record<string, unknown>;
  unused: UnusedSchemaDoc[];
} {
  const out = cleanDescriptions(components) as Record<string, unknown>;
  const unused: UnusedSchemaDoc[] = [];

  for (const [name, doc] of Object.entries(SCHEMA_DOCS)) {
    const schema = out[name];
    if (!isObject(schema)) {
      unused.push({ schema: name, reason: "no such schema" });
      continue;
    }

    if (doc.description !== undefined) {
      if (schema["description"] === undefined) {
        schema["description"] = doc.description;
      } else {
        unused.push({ schema: name, reason: "already described" });
      }
    }

    const properties = schema["properties"];
    for (const [property, description] of Object.entries(doc.properties ?? {})) {
      const target = isObject(properties) ? properties[property] : undefined;
      if (!isObject(target)) {
        unused.push({ schema: name, property, reason: "no such property" });
        continue;
      }
      if (target["description"] === undefined) {
        target["description"] = description;
      } else {
        unused.push({ schema: name, property, reason: "already described" });
      }
    }

    for (const [property, example] of Object.entries(doc.examples ?? {})) {
      const target = isObject(properties) ? properties[property] : undefined;
      if (!isObject(target)) {
        unused.push({ schema: name, property, reason: "no such property" });
        continue;
      }
      if (target["examples"] === undefined && target["example"] === undefined) {
        target["examples"] = [example];
      } else {
        unused.push({ schema: name, property, reason: "already described" });
      }
    }
  }

  return { components: out, unused };
}

/** Unwrap TSDoc markup in the inlined per-operation schemas. */
export function cleanOperationDescriptions<T>(operations: T): T {
  return cleanDescriptions(operations) as T;
}
