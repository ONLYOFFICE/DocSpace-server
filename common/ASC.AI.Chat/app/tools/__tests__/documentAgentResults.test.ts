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

vi.mock("../../storage/docspaceFileTransfer.js", () => ({
  uploadFile: vi.fn(),
  mediaTypeOf: () => "application/octet-stream",
}));
vi.mock("../../storage/threadFiles.js", () => ({ uploadThreadArtifact: vi.fn() }));
const getFileInfo = vi.fn();
vi.mock("../../storage/docspaceFilesApi.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../storage/docspaceFilesApi.js")>()),
  getFileInfo,
}));

const { uploadFile } = await import("../../storage/docspaceFileTransfer.js");
const { uploadThreadArtifact } = await import("../../storage/threadFiles.js");
const { saveResult } = await import("../documentAgentFiles.js");
const { savedFilesOf } = await import("../documentAgent.js");

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

describe("the files a document task names in its result", () => {
  const editorLink = (id: string) => `https://agent.local/doceditor?fileId=${id}`;

  beforeEach(() => {
    getFileInfo.mockReset();
  });

  it("describes each saved file for the model and for the document card", async () => {
    getFileInfo.mockResolvedValue({
      title: "Proposal (1).docx",
      folderId: "77",
      fileExst: ".docx",
      webUrl: "https://portal/doceditor?fileid=30",
    });
    expect(
      await savedFilesOf([
        {
          name: "Proposal.docx",
          mediaType: "application/docx",
          url: editorLink("30"),
        },
      ]),
    ).toEqual([
      {
        id: "30",
        title: "Proposal (1).docx",
        url: "/doceditor?fileId=30",
        folderId: "77",
        fileExst: ".docx",
        webUrl: "https://portal/doceditor?fileid=30",
      },
    ]);
    expect(getFileInfo).toHaveBeenCalledWith("30");
  });

  it("names a file it cannot read back by the artifact's name and extension", async () => {
    getFileInfo.mockRejectedValue(new Error("403"));
    expect(
      await savedFilesOf([
        { name: "Budget.XLSX", mediaType: "application/xlsx", url: editorLink("31") },
      ]),
    ).toEqual([{ id: "31", title: "Budget.XLSX", url: "/doceditor?fileId=31", fileExst: ".xlsx" }]);
  });

  it("leaves out the task's log and an artifact that names no file of ours", async () => {
    getFileInfo.mockResolvedValue({ title: "Report.docx" });
    const files = await savedFilesOf([
      { name: "task-log.md", mediaType: "text/markdown", url: editorLink("40"), kind: "log" },
      { name: "elsewhere.docx", mediaType: "application/docx", url: "https://example.com/x" },
      { name: "Report.docx", mediaType: "application/docx", url: editorLink("41") },
    ]);
    expect(files.map((file) => file.id)).toEqual(["41"]);
  });
});

describe("the files a document task names, read back or not", () => {
  const editorLink = (id: string) => `https://agent.local/doceditor?fileId=${id}`;

  beforeEach(() => {
    getFileInfo.mockReset();
  });

  it.each([
    ["no extension", "README"],
    ["a leading dot only", ".env"],
    ["a trailing dot", "draft."],
  ])("gives a file that is not found and whose name has %s no extension", async (_what, name) => {
    getFileInfo.mockResolvedValue(undefined);

    expect(await savedFilesOf([{ name, mediaType: "text/plain", url: editorLink("50") }])).toEqual([
      { id: "50", title: name, url: "/doceditor?fileId=50" },
    ]);
  });

  it("keeps the read-back title and takes the extension from it when the Files API gives none", async () => {
    getFileInfo.mockResolvedValue({ title: "Plan (2).PPTX" });

    expect(
      await savedFilesOf([{ name: "Plan.pptx", mediaType: "x", url: editorLink("51") }]),
    ).toEqual([
      { id: "51", title: "Plan (2).PPTX", url: "/doceditor?fileId=51", fileExst: ".pptx" },
    ]);
  });

  it.each([
    ["not a URL", "not a url"],
    ["another path", "https://agent.local/files?fileId=52"],
    ["no file id", "https://agent.local/doceditor"],
    ["an empty file id", "https://agent.local/doceditor?fileId="],
  ])("leaves out an artifact whose link is %s, and says so", async (_what, url) => {
    const { default: logger } = await import("../../log.js");

    expect(await savedFilesOf([{ name: "x.docx", mediaType: "x", url }])).toEqual([]);
    expect(getFileInfo).not.toHaveBeenCalled();
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "document_agent: artifact x.docx names no file of ours",
    );
  });

  // The outputs endpoint answers a host-relative Location, which the agent resolves against
  // the endpoint's address; one left relative does not parse.
  it("leaves out an editor link the agent did not resolve to an absolute one", async () => {
    expect(
      await savedFilesOf([{ name: "a.docx", mediaType: "x", url: "/doceditor?fileId=53" }]),
    ).toEqual([]);
    expect(getFileInfo).not.toHaveBeenCalled();
  });
});
