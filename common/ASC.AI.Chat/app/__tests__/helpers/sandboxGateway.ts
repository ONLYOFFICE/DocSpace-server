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

import { http, HttpResponse } from "./upstream.js";
import type { useUpstream } from "./upstream.js";

export const GATEWAY_URL = "http://gateway:9090";
const RPC = `${GATEWAY_URL}/gateway.v1.SandboxService`;

/** One Connect envelope: the flag byte, the length, the JSON. */
export const frame = (message: unknown, flags = 0): Uint8Array => {
  const json = Buffer.from(JSON.stringify(message), "utf8");
  const header = Buffer.alloc(5);
  header.writeUInt8(flags, 0);
  header.writeUInt32BE(json.length, 1);
  return Buffer.concat([header, json]);
};

/** An answer whose body arrives in the given chunks, without a declared length. */
export const chunked = (...chunks: Uint8Array[]): Response =>
  new Response(
    new ReadableStream<Uint8Array>({
      start(controller) {
        for (const chunk of chunks) {
          controller.enqueue(chunk);
        }
        controller.close();
      },
    }),
    { headers: { "Content-Type": "application/connect+json" } },
  );

export interface ExecCall {
  command: string;
  cwd?: string;
  env?: Record<string, string>;
  timeout: string;
}

export interface ExecOutcome {
  stdout?: string;
  stderr?: string;
  code?: number;
}

/**
 * A sandbox gateway that keeps files in memory and answers commands through `run`: enough of
 * the gateway of the document agent's repository for the code that drives a plugin task and the
 * chat's own sandbox tools.
 */
export interface FakeGateway {
  /** Every call in the order it was made: `Create`, `Renew`, `Delete`, `MakeDir /path`, `PUT /path`, `GET /path`, `Exec command`. */
  calls: string[];
  files: Map<string, Buffer>;
  execs: ExecCall[];
  /** What a command comes to; exit status 0 without output unless replaced. */
  run: (call: ExecCall) => ExecOutcome | Response | Promise<ExecOutcome | Response>;
  /** A method or a file route that answers the given response instead of its own. */
  fail: Map<string, () => Response | Promise<Response>>;
}

export function fakeGateway(upstream: ReturnType<typeof useUpstream>, id = "sb-1"): FakeGateway {
  const gateway: FakeGateway = {
    calls: [],
    files: new Map(),
    execs: [],
    run: () => ({}),
    fail: new Map(),
  };
  const unary = (method: string, answer: (body: Record<string, string>) => unknown) =>
    http.post(`${RPC}/${method}`, async ({ request }) => {
      const body = (await request.json()) as Record<string, string>;
      gateway.calls.push(method === "MakeDir" ? `MakeDir ${body["path"]}` : method);
      return (await gateway.fail.get(method)?.()) ?? HttpResponse.json(answer(body) as object);
    });
  upstream.use(
    unary("Create", () => ({ id })),
    unary("Renew", () => ({ sandbox: {} })),
    unary("Delete", () => ({})),
    unary("MakeDir", () => ({})),
    http.put(`${GATEWAY_URL}/sandboxes/:id/files`, async ({ request }) => {
      const target = new URL(request.url).searchParams.get("path") ?? "";
      gateway.calls.push(`PUT ${target}`);
      const failure = gateway.fail.get(`PUT ${target}`) ?? gateway.fail.get("PUT");
      if (failure) {
        return failure();
      }
      gateway.files.set(target, Buffer.from(await request.arrayBuffer()));
      return new HttpResponse(null, { status: 204 });
    }),
    http.get(`${GATEWAY_URL}/sandboxes/:id/files`, ({ request }) => {
      const target = new URL(request.url).searchParams.get("path") ?? "";
      gateway.calls.push(`GET ${target}`);
      const failure = gateway.fail.get(`GET ${target}`) ?? gateway.fail.get("GET");
      if (failure) {
        return failure();
      }
      const bytes = gateway.files.get(target);
      return bytes ? chunked(bytes) : HttpResponse.json({ error: "not found" }, { status: 404 });
    }),
    http.post(`${RPC}/Exec`, async ({ request }) => {
      const sent = Buffer.from(await request.arrayBuffer());
      const call = JSON.parse(sent.subarray(5).toString()) as ExecCall;
      gateway.calls.push(`Exec ${call.command}`);
      gateway.execs.push(call);
      const outcome = await gateway.run(call);
      if (outcome instanceof Response) {
        return outcome;
      }
      return chunked(
        ...(outcome.stdout ? [frame({ stdout: outcome.stdout })] : []),
        ...(outcome.stderr ? [frame({ stderr: outcome.stderr })] : []),
        frame({ exit: { code: outcome.code ?? 0 } }),
        frame({}, 2),
      );
    }),
  );
  return gateway;
}

/** A refusal as the gateway's RPC methods answer it. */
export const refusedWith =
  (code: string, status = 500) =>
  (): Response =>
    HttpResponse.json({ code, message: "refused" }, { status });
