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

import { beforeEach, describe, expect, it, vi } from "vitest";

const post = vi.fn<(path: string, body: unknown) => Promise<unknown>>();
vi.mock("../httpClient.js", () => {
  class AiServiceHttpError extends Error {
    constructor(public readonly status: number) {
      super(`AI service ${status}`);
    }
  }
  return { AiServiceHttpError, aiService: { post } };
});
const { AiServiceHttpError } = await import("../httpClient.js");
const { uploadThreadArtifact } = await import("../threadFiles.js");

const refusal = (status: number): Error =>
  new (AiServiceHttpError as unknown as new (status: number) => Error)(status);

beforeEach(() => {
  post.mockReset();
});

describe("uploadThreadArtifact", () => {
  it("posts the file as a form to the thread's artifacts", async () => {
    post.mockResolvedValue({ id: 55, title: "image (1).png" });
    const file = await uploadThreadArtifact(
      "t-1",
      "image.png",
      new Uint8Array([1, 2]),
      "image/png",
    );
    expect(file).toEqual({ id: "55", title: "image (1).png" });
    const [path, form] = post.mock.calls[0] ?? [];
    expect(path).toBe("/threads/t-1/artifacts");
    expect(form).toBeInstanceOf(FormData);
    expect((form as FormData).get("title")).toBe("image.png");
    expect((form as FormData).get("file")).toBeInstanceOf(Blob);
  });

  it.each([400, 403, 404])(
    "answers nothing for a thread that takes no artifacts (%s)",
    async (status) => {
      post.mockRejectedValue(refusal(status));
      expect(
        await uploadThreadArtifact("t-1", "image.png", new Uint8Array([1]), "image/png"),
      ).toBeUndefined();
    },
  );

  it.each([
    ["any other refusal", refusal(500)],
    ["an error of another class with a 404", Object.assign(new Error("x"), { status: 404 })],
  ])("throws on %s", async (_case, error) => {
    post.mockRejectedValue(error);
    await expect(
      uploadThreadArtifact("t-1", "image.png", new Uint8Array([1]), "image/png"),
    ).rejects.toBe(error);
  });
});
