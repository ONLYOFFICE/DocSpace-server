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

/**
 * Prose that replaces a library description, pinned to the text it was
 * written against: `replacing` is the start of the library's description. A
 * library release that rewrites the text no longer starts with it, and the
 * entry is reported instead of silently covering the new text - which may
 * already have fixed what the replacement was for.
 */
interface Replacement {
  replacing: string;
  text: string;
}

interface SchemaDoc {
  /** Prose for the schema itself (`schema-description`). */
  description?: string;
  /**
   * The one exception to "fill-in only": prose that replaces the library's own
   * description of the schema. Only for a library text written for the
   * library's callers - one that names its internal functions and adapters
   * (`StorageAdapter.getRoomSkill`, `toChatError`) - which a caller of this API
   * can neither see nor use. See `Replacement` for how a library change is
   * caught.
   */
  replaceDescription?: Replacement;
  /** Prose per property name (`schema-property-description`). */
  properties?: Readonly<Record<string, string>>;
  /** `replaceDescription` for a property's library description. */
  replaceProperties?: Readonly<Record<string, Replacement>>;
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
    replaceProperties: {
      useProxy: {
        replacing: "Route every provider request through the host's `fetchProxy`",
        text: "Whether the chat client sends this profile's provider requests through the portal's proxy instead of directly. Has no effect when no proxy is configured.",
      },
    },
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
    replaceDescription: {
      replacing: "Input for creating a new profile",
      text: "Input for creating a new profile: the shape of a profile without the fields the service fills in (`id`, `createdAt`).",
    },
    replaceProperties: {
      useProxy: {
        replacing: "Route every provider request through the host's `fetchProxy`",
        text: "Whether the chat client sends this profile's provider requests through the portal's proxy instead of directly. Has no effect when no proxy is configured.",
      },
    },
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

  AiSendStreamBody: {
    examples: {
      threadId: EXAMPLE_IDS.thread,
      userMessage: { role: "user", content: "Summarise the attached contract." },
      actionArgs: { isReasoning: false },
      entityId: EXAMPLE_IDS.room,
      profileId: EXAMPLE_IDS.profile,
    },
  },

  AiActionArgs: {
    description:
      "Per-request options of an AI round: extra tools, the depth of reasoning, a system prompt override and the rooms and folder connected as context.",
    examples: {
      tools: [],
      isReasoning: false,
      prompt: { mode: "append", text: "Answer in British English." },
    },
  },

  AiToolCallData: {
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

  AiMCPItem: {
    replaceProperties: {
      serverType: {
        replacing: "Server type (MCP server name / host tool group id)",
        text: "The server or tool group the tool belongs to (an MCP server name or a host tool group ID): the key a disabled tool is stored under, so a tool sent in `actionArgs.tools` is still matched to its group.",
      },
      requireApproval: {
        replacing: "Whether the consumer must show an approval dialog",
        text: "Whether the tool asks for approval before it runs. Under the `auto` tool permission mode `false` runs it without asking, `true` or absent leaves it to the tools the user always allows; `ask` asks for every tool, `allow` for none. Host tools default to `false`; MCP and custom-server tools leave it unset.",
      },
      annotations: {
        replacing: "The MCP tool annotations as the server declared them",
        text: "The MCP tool annotations as the server declared them in `tools/list`; never sent to the model. Under the `auto` tool permission mode a tool annotated `readOnlyHint: true` and `destructiveHint: false` runs without approval, a destructive or unannotated one asks.",
      },
    },
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

  AiContextRoomRef: {
    replaceDescription: {
      replacing: "One room a round runs with as context",
      text: "One Instructions room a round runs with as context: the account and room ID from `GET api/2.0/ai/context/get-context-folders`, plus the skills of the room the round may use.",
    },
    properties: {
      cloud: "The account the room belongs to; `docspace` for a room of this portal.",
      id: "The room ID.",
      skills:
        "The skills of the room the round may use, as `GET api/2.0/ai/context/get-room-skills` lists them.",
    },
    examples: { cloud: "docspace", id: EXAMPLE_IDS.room, skills: [] },
  },

  AiKnowledgeRoomRef: {
    replaceDescription: {
      replacing: "One knowledge base a round searches",
      text: "One Knowledge room a round searches: the account and room ID from `GET api/2.0/ai/context/get-knowledge-folders`.",
    },
    properties: {
      cloud: "The account the room belongs to; `docspace` for a room of this portal.",
      id: "The room ID; anything that is not a positive integer is ignored.",
    },
    examples: { cloud: "docspace", id: EXAMPLE_IDS.room },
  },

  AiFolderContextRef: {
    replaceDescription: {
      replacing: "A folder connected as the round's context",
      text: "A folder connected as the round's context - any folder of the portal, not only a room - named by its account and ID.",
    },
    properties: {
      cloud: "The account the folder belongs to; `docspace` for a folder of this portal.",
      id: "The folder ID.",
    },
    examples: { cloud: "docspace", id: EXAMPLE_IDS.room },
  },

  AiRoomSkill: {
    replaceDescription: {
      replacing: "A skill configured inside a room",
      text: "A skill of an Instructions room: the frontmatter of one Markdown skill file. `name` and `description` are what the model is shown; the body is read through `GET api/2.0/ai/context/get-room-skill`.",
    },
    properties: {
      id: "The skill's ID, as `GET api/2.0/ai/context/get-room-skill` takes it.",
      name: "The skill's name, from its frontmatter.",
      description: "What the skill is for, from its frontmatter; empty when it declares none.",
    },
    examples: {
      id: "5678",
      name: "contract-review",
      description: "How to review a supply contract.",
    },
  },

  AiContextFolder: {
    replaceDescription: {
      replacing: "One cloud account's rooms",
      text: "The rooms of one account the caller can connect as context, grouped under the account.",
    },
    properties: {
      cloud: "The account the rooms belong to; `docspace` for this portal.",
      rooms: "The rooms of that account the caller can connect.",
    },
    examples: { cloud: "docspace", rooms: [] },
  },

  AiContextRoom: {
    replaceDescription: {
      replacing: "A room the user can pull into the conversation",
      text: "A room the caller can connect to a chat as context.",
    },
    properties: {
      id: "The room ID.",
      name: "The room title.",
    },
    examples: { id: EXAMPLE_IDS.room, name: "Contract templates" },
  },

  AiSuggestedQuestion: {
    examples: {
      question: "Which region had the most responses?",
      prompt: "Group the submissions by region and show the count for each.",
    },
  },

  AiChatErrorPayload: {
    replaceDescription: {
      replacing: "Serializable error payload",
      text: "Why a round failed or stopped early, as the message status and the stream report it: a stable `code`, a message safe to show the user, and the cause when there is one.",
    },
    examples: { code: "rate_limit", message: "The provider is rate limiting requests." },
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

  AiOpenAIFinishReason: {
    replaceDescription: {
      replacing: "OpenAI Chat Completions streaming shapes.",
      text: "Why a choice of an OpenAI-compatible streamed completion ended, as the OpenAI Chat Completions format spells it; null while the choice is still streaming.",
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
    replaceProperties: {
      reasoningSupport: {
        replacing: "What the model can do with extended thinking",
        text: "What the model can do with extended thinking, when the provider's catalogue reports it (OpenRouter and the ONLYOFFICE route do). Copied onto the profile when it is saved; when absent, the abilities are taken from the model ID.",
      },
    },
    examples: {
      id: "gpt-4o",
      name: "GPT-4o",
      provider: "openai",
      reasoning: false,
      capabilities: 7,
    },
  },

  AiProvider: {
    examples: {
      type: "openai",
      name: "OpenAI GPT-4o",
      key: "sk-your-provider-api-key",
      baseUrl: "https://api.openai.com/v1",
    },
  },

  // `kind` decides which half of this shape is populated: `content`, `path` and
  // `type` belong to a file, `base64` to an image. Every renderer builds its
  // sample payload by taking one example per property, so giving both halves an
  // example would describe an attachment that cannot exist. This example is a
  // file, so `base64` deliberately has none.
  //
  // The rule for any type whose fields are mutually exclusive: give an example
  // only to the fields the illustrated variant actually carries, and leave the
  // rest without one rather than filling the gap. `AiChatEvent` below is the
  // other such type here.
  AiAttachment: {
    examples: {
      id: EXAMPLE_IDS.attachment,
      kind: "file",
      source: "user",
      title: "contract.docx",
      content: "This agreement is made on 1 January 2026 between …",
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

  AiErrorData: {
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
    replaceDescription: {
      replacing: "Discriminated event emitted by the streaming methods",
      text: "One event of a chat stream, told apart by `type`: `message-start`, `message-delta` and `message-end` frame the assistant reply, `message-incomplete` reports a provider error or an unfinished answer, `thread-title` carries a generated title, and `tool-call-pending` pauses the round until the call is approved (`POST api/2.0/ai/ai/approve-tool-call`) or denied (`POST api/2.0/ai/ai/deny-tool-call`).",
    },
    replaceProperties: {
      autoAllow: {
        replacing: "The consumer should execute the tool without prompting the user.",
        text: "The tool runs without asking the user, as the user's tool permission mode decides: under `ask` never; under `auto` a tool the user always allows, one that opts out of approval (`requireApproval: false`) or one annotated read-only and non-destructive; under `allow` every tool.",
      },
      serverExecuted: {
        replacing: "Set when the tool is served by a server-side system source",
        text: "The tool is run by the service itself: the client must not run it, only ask for approval (unless `autoAllow`) and approve or deny the call - an approval needs no `result`.",
      },
    },
    // A union over seven `type` values, and most fields belong to one variant
    // only: `idx`, `autoAllow` and `serverExecuted` to `tool-call-pending`,
    // `title` and `profileId` to `thread-title`. Only the fields every event
    // carries get an example, so an assembled sample stays a payload that can
    // actually occur.
    examples: {
      type: "message-delta",
      messageId: EXAMPLE_IDS.message,
      threadId: EXAMPLE_IDS.thread,
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
    replaceDescription: {
      replacing: "Input for creating a prompt",
      text: "Input for creating a prompt; the service fills in `id`, `createdAt` and `updatedAt`.",
    },
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
    replaceProperties: {
      title: {
        replacing: "Empty string for existing threads",
        text: "The title generated for a thread this call created; an empty string when it opened an existing thread.",
      },
    },
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
  reason:
    | "no such schema"
    | "no such property"
    | "already described"
    | "nothing to replace"
    | "library text changed";
}

/**
 * Applies one pinned replacement to a schema node; the reason it was not
 * applied, or `undefined` when it was.
 */
function replace(
  node: Record<string, unknown>,
  replacement: Replacement,
): UnusedSchemaDoc["reason"] | undefined {
  const current = node["description"];
  if (typeof current !== "string") {
    return "nothing to replace";
  }
  if (!current.startsWith(replacement.replacing)) {
    return "library text changed";
  }
  node["description"] = replacement.text;
  return undefined;
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

    if (doc.replaceDescription !== undefined) {
      const reason = replace(schema, doc.replaceDescription);
      if (reason) {
        unused.push({ schema: name, reason });
      }
    }

    for (const [property, replacement] of Object.entries(doc.replaceProperties ?? {})) {
      const props = schema["properties"];
      const target = isObject(props) ? props[property] : undefined;
      if (!isObject(target)) {
        unused.push({ schema: name, property, reason: "no such property" });
        continue;
      }
      const reason = replace(target, replacement);
      if (reason) {
        unused.push({ schema: name, property, reason });
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

/**
 * Unwrap TSDoc markup in the inlined per-operation schemas, and give them the
 * same pinned replacements as the components: a library type inlined into an
 * operation (`Omit<Profile, "key">`) carries the same library text, matched by
 * the start it is pinned to.
 */
export function cleanOperationDescriptions<T>(operations: T): T {
  const replacements = Object.values(SCHEMA_DOCS).flatMap((doc) => [
    ...(doc.replaceDescription ? [doc.replaceDescription] : []),
    ...Object.values(doc.replaceProperties ?? {}),
  ]);
  return replaceInlinedLibraryTexts(cleanDescriptions(operations), replacements) as T;
}

function replaceInlinedLibraryTexts(node: unknown, replacements: readonly Replacement[]): unknown {
  if (Array.isArray(node)) {
    return node.map((item) => replaceInlinedLibraryTexts(item, replacements));
  }
  if (!isObject(node)) {
    return node;
  }
  const out: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(node)) {
    const replacement =
      key === "description" && typeof value === "string"
        ? replacements.find((candidate) => value.startsWith(candidate.replacing))
        : undefined;
    out[key] = replacement ? replacement.text : replaceInlinedLibraryTexts(value, replacements);
  }
  return out;
}
