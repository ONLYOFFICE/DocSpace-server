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

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
vi.mock("../../storage/docspaceFileTransfer.js", () => ({
  uploadFile: vi.fn(),
  mediaTypeOf: () => "application/octet-stream",
}));
vi.mock("../../storage/threadFiles.js", () => ({ uploadThreadArtifact: vi.fn() }));

const { uploadFile } = await import("../../storage/docspaceFileTransfer.js");
const { uploadThreadArtifact } = await import("../../storage/threadFiles.js");
const { saveResult } = await import("../documentAgentFiles.js");

const bytes = new Uint8Array([1, 2, 3]);
const THREAD = "0198c3f4-6a2e-7c41-9b8d-2f5e7a1c3d90";

beforeEach(() => {
  vi.mocked(uploadFile).mockReset().mockResolvedValue({ id: "20", title: "Report.docx" });
  vi.mocked(uploadThreadArtifact).mockReset();
});

describe("a result file of the document agent", () => {
  it("is kept among the artifacts of its conversation", async () => {
    vi.mocked(uploadThreadArtifact).mockResolvedValue({ id: "30", title: "Report (1).docx" });
    const file = await saveResult(THREAD, "5", "Report.docx", bytes, "application/docx");
    expect(uploadThreadArtifact).toHaveBeenCalledWith(
      THREAD,
      "Report.docx",
      bytes,
      "application/docx",
    );
    expect(file).toMatchObject({ id: "30", title: "Report (1).docx" });
    expect(uploadFile).not.toHaveBeenCalled();
  });

  it("goes into the run's folder when the conversation takes no artifacts", async () => {
    vi.mocked(uploadThreadArtifact).mockResolvedValue(undefined);
    const file = await saveResult(THREAD, "5", "Report.docx", bytes, "application/docx");
    expect(uploadFile).toHaveBeenCalledWith("5", "Report.docx", bytes, "application/docx");
    expect(file).toMatchObject({ id: "20" });
  });

  it("goes into the run's folder when no conversation is named", async () => {
    await saveResult(undefined, "5", "Report.docx", bytes, "application/docx");
    expect(uploadThreadArtifact).not.toHaveBeenCalled();
    expect(uploadFile).toHaveBeenCalledTimes(1);
  });

  it("fails as the artifacts do on any other refusal, without a second copy", async () => {
    vi.mocked(uploadThreadArtifact).mockRejectedValue(
      Object.assign(new Error("boom"), { status: 500 }),
    );
    await expect(
      saveResult(THREAD, "5", "Report.docx", bytes, "application/docx"),
    ).rejects.toMatchObject({
      status: 500,
    });
    expect(uploadFile).not.toHaveBeenCalled();
  });
});
