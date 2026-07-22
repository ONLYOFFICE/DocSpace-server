// Ambient shim for `@assistant-ui/react`, used ONLY during OpenAPI schema
// generation. The real package is a heavy React UI library and is not a
// runtime or build dependency of this server; the only thing the public
// `@onlyoffice/ai-chat` types borrow from it is `ThreadMessageLike`. We
// declare a faithful-enough, JSON-wire-shaped message here so the schema
// generator can resolve chat message request/response types without pulling
// in the UI library. Not compiled into the app.
declare module "@assistant-ui/react" {
  export type ThreadMessageLike = {
    /** Storage-assigned message id (absent on inbound drafts). */
    id?: string;
    /** Message author role. */
    role: "user" | "assistant" | "system";
    /**
     * Message content: either plain text or a list of typed content parts
     * (text, image, tool-call, …). Parts are open-ended by content type.
     */
    content: string | Array<{
      type: string;
      text?: string;
      [key: string]: unknown;
    }>;
    /** Creation timestamp, ISO-8601 on the wire. */
    createdAt?: string;
    /** Delivery/generation status of the message. */
    status?: { type: string; [key: string]: unknown };
    /** Arbitrary per-message metadata. */
    metadata?: Record<string, unknown>;
    /** Attachments linked to the message. */
    attachments?: Array<Record<string, unknown>>;
  };
}
