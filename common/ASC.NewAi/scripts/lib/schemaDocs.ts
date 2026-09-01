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
}

const SCHEMA_DOCS: Readonly<Record<string, SchemaDoc>> = {
  AiActionType: {
    description:
      "The AI action a request or an assignment applies to. Each action has its own assignment slot; `Default` is the profile used when an action's own slot is empty.",
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
    properties: {
      success: "True when the profile was persisted.",
      profile: "The persisted profile. Present on success.",
      error:
        "Why the profile was rejected - the name check or the provider credential check. Present on failure.",
    },
  },

  AiPromptMutationResult: {
    properties: {
      success: "True when the prompt was persisted.",
      prompt: "The persisted prompt. Present on success.",
      error: "Why the prompt was rejected. Present on failure.",
    },
  },

  AiFolderMutationResult: {
    properties: {
      success: "True when the folder was persisted.",
      folder: "The persisted folder. Present on success.",
      error: "Why the folder was rejected. Present on failure.",
    },
  },

  AiAssignmentMutationResult: {
    properties: {
      success: "True when the assignment was persisted.",
      error: "Why the assignment was rejected. Present on failure.",
    },
  },

  AiToolsMutationResult: {
    properties: {
      success: "True when the MCP server was persisted.",
      error: "Why the MCP server was rejected. Present on failure.",
    },
  },

  AiWebSearchMutationResult: {
    properties: {
      success: "True when the configuration was persisted.",
      config: "The persisted web-search configuration. Present on success.",
      error: "Why the configuration was rejected. Present on failure.",
    },
  },

  /* --- Bulk outcomes: on failure nothing at all was persisted ------------ */

  AiBulkAssignmentResult: {
    properties: {
      success: "True when every entry was persisted.",
      errors:
        "What was rejected, per action. Present on failure - and then no entry was persisted.",
    },
  },

  AiToolsBulkResult: {
    properties: {
      success: "True when every custom MCP server was persisted.",
      errors:
        "What was rejected, per server. Present on failure - and then no server was persisted.",
    },
  },

  AiImportResult: {
    properties: {
      success: "True when the whole bundle was imported.",
      imported: "How many folders and prompts were created. Present on success.",
      errors: "What was rejected, per entry. Present on failure - and then nothing was imported.",
    },
  },

  AiImportError: {
    properties: {
      ref: "The offending entry - its name or its id.",
      error: "Why the entry was rejected.",
    },
  },

  /* --- Prompts and threads ---------------------------------------------- */

  AiCreatePromptInput: {
    properties: {
      name: "The prompt name.",
      text: "The prompt body.",
      folderId:
        "The folder to file the prompt under. Omit or send null to leave it outside any folder.",
    },
  },

  AiPromptBundle: {
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
  }

  return { components: out, unused };
}

/** Unwrap TSDoc markup in the inlined per-operation schemas. */
export function cleanOperationDescriptions<T>(operations: T): T {
  return cleanDescriptions(operations) as T;
}
