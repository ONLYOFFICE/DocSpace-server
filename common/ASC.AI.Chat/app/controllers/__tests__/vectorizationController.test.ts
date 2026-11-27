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

import { describe, expect, it, vi } from "vitest";
import { invoke } from "../../__tests__/helpers/http.js";
import {
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { listen, send } from "../../__tests__/helpers/rawHttp.js";

// The vectorization proxy: the body goes as it came to `POST internal/ai/vectorization/tasks`
// (products/ASC.AI/Server/Api/VectorizationController.cs, StartTaskAsync), stubbed with msw,
// and its answer comes back in the DocSpace envelope.

const upstream = useUpstream();
const { aiServiceBaseUrl } = await import("../../storage/httpClient.js");
const { vectorizationController } = await import("../vectorizationController.js");

const { createApp } = await import("../../createApp.js");

const TASKS = `${aiServiceBaseUrl}/internal/ai/vectorization/tasks`;

// The envelope the .NET API wraps the empty result of StartTaskAsync in.
const envelope = { count: 0, status: 0, statusCode: 200 };

const startTask = (body: unknown) => invoke(vectorizationController.startTask, { body });

describe("vectorizationController.startTask", () => {
  it("forwards the body and answers the envelope as it came", async () => {
    upstream.use(http.post(TASKS, () => HttpResponse.json(envelope)));

    const reply = await startTask({ files: [1, 2] });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(envelope);
    expect(responseViolations("aiVectorizationStartTask", 200, reply.body)).toEqual([]);
    expect(upstream.seen).toEqual([
      {
        method: "POST",
        path: "/internal/ai/vectorization/tasks",
        headers: { "content-type": "application/json" },
        body: { files: [1, 2] },
      },
    ]);
  });

  it("keeps a response field of the envelope", async () => {
    upstream.use(http.post(TASKS, () => HttpResponse.json({ ...envelope, response: null })));

    const reply = await startTask({ files: [1] });

    expect(reply.body).toEqual({ ...envelope, response: null });
  });

  it.each([
    ["a page of a proxy", () => HttpResponse.text("<html>proxy</html>")],
    ["an empty object", () => HttpResponse.json({})],
    ["an error sent with 200", () => HttpResponse.json({ error: "proxy detail" })],
    ["a list", () => HttpResponse.json([envelope])],
  ])("answers 502 when the AI service answers %s, not the envelope", async (_what, answer) => {
    upstream.use(http.post(TASKS, answer));

    const reply = await startTask({ files: [1] });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Bad Gateway" });
    expect(JSON.stringify(reply.body)).not.toContain("proxy");
    expect(responseViolations("aiVectorizationStartTask", 502, reply.body)).toEqual([]);
  });

  // HANDLED DOWNSTREAM passthrough-15: the body is not checked here (`req.body ?? {}` is forwarded whatever it is). In production the AI service binds `files` as a set of integers and refuses anything else, an empty list included, with 400 (VectorizationStartRequestDto, VectorizationTaskPublisher); it shows only with the engine or the service stubbed.
  it.each([
    ["a string", "files"],
    ["an array", [1, 2]],
    ["an empty object", {}],
    ["files that are not a list", { files: "1" }],
    ["an empty files list", { files: [] }],
  ])("forwards a body that is %s to the AI service unchecked", async (_what, body) => {
    upstream.use(http.post(TASKS, () => HttpResponse.json(envelope)));

    await startTask(body);

    expect(upstream.seen[0]?.body).toEqual(body);
  });

  it.each([
    [400, "Bad Request"],
    [404, "Not Found"],
  ])("relays the AI service's %i, as published", async (status, error) => {
    upstream.use(http.post(TASKS, () => docspaceError(status)));

    const reply = await startTask({ files: [1] });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiVectorizationStartTask", status, reply.body)).toEqual([]);
  });

  it.each([
    [403, "Forbidden"],
    [500, "Internal Server Error"],
  ])("answers the AI service's %i with its reason phrase only", async (status, error) => {
    upstream.use(http.post(TASKS, () => docspaceError(status, "at Vectorization.Publish()")));

    const reply = await startTask({ files: [1] });

    expect(reply.status).toBe(status);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiVectorizationStartTask", status, reply.body)).toEqual([]);
  });

  // The C# action answers through the response wrapper: always 200 with the envelope. A 204 or
  // a text body came from something else and is not relayed as a 200.
  it.each([
    ["204 without a body", () => new HttpResponse(null, { status: 204 })],
    ["a text body", () => new HttpResponse("ok", { status: 200 })],
  ])("answers 502 for an answer of %s", async (_what, answer) => {
    upstream.use(http.post(TASKS, answer));

    const reply = await startTask({ files: [1] });

    expect(reply.status).toBe(502);
    expect(reply.body).toEqual({ error: "Bad Gateway" });
    expect(responseViolations("aiVectorizationStartTask", 502, reply.body)).toEqual([]);
  });
});

describe("POST vectorization/tasks through the application", () => {
  // Through a raw socket: supertest's own request would be intercepted by msw.
  it("forwards an empty object for a JSON null body", async () => {
    upstream.use(http.post(TASKS, () => HttpResponse.json(envelope)));
    const server = await listen(createApp());
    try {
      const reply = await send(server.port, {
        path: "/api/2.0/ai/vectorization/tasks",
        headers: { Authorization: "Bearer caller-token", "Content-Type": "application/json" },
        body: "null",
      }).done;

      expect(reply.status).toBe(200);
      expect(JSON.parse(reply.body)).toEqual(envelope);
      expect(upstream.seen.map((r) => r.body)).toEqual([{}]);
    } finally {
      await server.close();
      vi.unstubAllEnvs();
    }
  });
});
