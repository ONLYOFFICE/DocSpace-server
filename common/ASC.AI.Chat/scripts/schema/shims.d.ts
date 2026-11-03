// Ambient shim for `@assistant-ui/react`, used ONLY during OpenAPI schema
// generation. The real package is a heavy React UI library and is not a
// runtime or build dependency of this server; the only thing the public
// `@onlyoffice/ai-chat` types borrow from it is `ThreadMessageLike`. We
// declare a faithful-enough, JSON-wire-shaped message here so the schema
// generator can resolve chat message request/response types without pulling
// in the UI library. Not compiled into the app.
declare module "@assistant-ui/react" {
  import type { ChatErrorPayload } from "@onlyoffice/ai-chat/core";

  /** A single chat message as it travels on the wire. */
  export type ThreadMessageLike = {
    /** Storage-assigned message id (absent on inbound drafts). */
    id?: string;
    /** Message author role. */
    role: "user" | "assistant" | "system";
    /**
     * Message content: either plain text or a list of typed content parts.
     * One part schema lists every field of every part kind of the
     * `@assistant-ui/react` 0.11 `ThreadMessageLike` union (text, reasoning,
     * source, image, file, audio, data, tool-call) plus what the chat library
     * adds itself (`name` on an image part, `metadata` on a tool-call part), so
     * a strict client can read any stored part; each field says which kinds
     * carry it.
     */
    content:
      | string
      | Array<{
          /**
           * Part kind: `text`, `reasoning`, `source`, `image`, `file`, `audio`,
           * `data` or `tool-call`.
           */
          type: string;
          /** The text of a `text` or `reasoning` part. */
          text?: string;
          /**
           * Id of the part this one belongs to, on `text`, `reasoning`,
           * `source` and `tool-call` parts. On a `reasoning` part the chat
           * library keeps the provider's thinking signature here.
           */
          parentId?: string;
          /** A `source` part's source kind; always `url`. */
          sourceType?: string;
          /** A `source` part's id. */
          id?: string;
          /** A `source` part's URL. */
          url?: string;
          /** A `source` part's title. */
          title?: string;
          /**
           * An `image` part's picture, as a URL (usually a `data:` URL). For an image the composer
           * attached, a JSON string `{ ref, title, kind: "image" }` naming the
           * stored attachment instead.
           */
          image?: string;
          /**
           * A `file` part's media type. For a file the composer attached, a JSON
           * string `{ ref, title, kind: "file", path, type }` naming the stored
           * attachment instead - the chat library carries the reference here.
           */
          mimeType?: string;
          /**
           * A `file` part's content; empty when the part references an
           * attachment. (A `data` part would carry its payload here too, as any
           * JSON value; neither the chat library nor this service writes one.)
           */
          data?: string;
          /** The file name of a `file` or `image` part. */
          filename?: string;
          /** An `image` part's display name, or a `data` part's name. */
          name?: string;
          /** An `audio` part's sound. */
          audio?: {
            /** The audio data, base64. */
            data: string;
            /** Audio format: `mp3` or `wav`. */
            format: "mp3" | "wav";
          };
          /** A `tool-call` part's call id. */
          toolCallId?: string;
          /** A `tool-call` part's tool name. */
          toolName?: string;
          /** A `tool-call` part's arguments. */
          args?: Record<string, unknown>;
          /** A `tool-call` part's arguments as the model streamed them, JSON text. */
          argsText?: string;
          /** A `tool-call` part's result, once the tool has run. */
          result?: unknown;
          /** Whether a `tool-call` part's result is an error. */
          isError?: boolean;
          /**
           * Extra data of a `tool-call` part. While the tool runs, the chat
           * library keeps its progress here as `{ progress: [{ progress,
           * total?, message? }] }`.
           */
          artifact?: unknown;
          /** A `tool-call` part's pending request for human input. */
          interrupt?: {
            /** Interrupt kind; always `human`. */
            type: string;
            /** What the tool asks the human for. */
            payload?: unknown;
          };
          /** Messages of a sub-agent run inside a `tool-call` part. */
          messages?: Array<Record<string, unknown>>;
          /**
           * Provider-specific data of a `tool-call` part - for a Gemini call,
           * `{ thoughtSignature }`, which has to be sent back with the call.
           */
          metadata?: Record<string, unknown>;
          [key: string]: unknown;
        }>;
    /** Creation timestamp, ISO-8601 on the wire. */
    createdAt?: string;
    /** Delivery/generation status of the message. */
    status?: {
      /** `running`, `requires-action`, `complete` or `incomplete`. */
      type: string;
      /**
       * Why the message is in that state: `tool-calls` or `interrupt` for
       * `requires-action`; `stop` or `unknown` for `complete`; `cancelled`,
       * `tool-calls`, `length`, `content-filter`, `other` or `error` for
       * `incomplete`. An `incomplete` message the chat stored carries
       * `cancelled` (the round was stopped) or `error`.
       */
      reason?: string;
      /** What failed, on an `incomplete` message whose `reason` is `error`. */
      error?: ChatErrorPayload;
      [key: string]: unknown;
    };
    /**
     * Per-message metadata. The chat library writes `custom` (with the
     * round's `contextEntityId`) and `steps` (token usage per provider
     * request); `unstable_state`, `unstable_annotations`, `unstable_data` and
     * `submittedFeedback` may appear as well. Read it as an open map.
     */
    metadata?: Record<string, unknown>;
    /**
     * Attachments linked to the message (`id`, `type`, `name`,
     * `contentType`, `status`, `content`). Read them as open objects.
     */
    attachments?: Array<Record<string, unknown>>;
  };
}
