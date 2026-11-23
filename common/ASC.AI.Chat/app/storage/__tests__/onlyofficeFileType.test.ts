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

import { describe, expect, it } from "vitest";
import { getOnlyofficeFileType } from "../onlyofficeFileType.js";

// The ONLYOFFICE `c_oAscFileType` table the widget's attachment chip reads its icon from.
describe("getOnlyofficeFileType", () => {
  it.each([
    ["doc", 66],
    ["docx", 65],
    ["docm", 75],
    ["dotx", 76],
    ["dotm", 77],
    ["odt", 67],
    ["ott", 79],
    ["fodt", 78],
    ["rtf", 68],
    ["txt", 69],
    ["mht", 71],
    ["html", 70],
    ["htm", 70],
    ["xml", 70],
    ["epub", 72],
    ["fb2", 73],
    ["mobi", 74],
    ["docxf", 83],
    ["oform", 84],
    ["md", 69],
    ["ppt", 130],
    ["pptx", 129],
    ["pptm", 134],
    ["ppsx", 132],
    ["ppsm", 133],
    ["potx", 135],
    ["potm", 136],
    ["odp", 131],
    ["otp", 138],
    ["fodp", 137],
    ["xls", 258],
    ["xlsx", 257],
    ["xlsm", 261],
    ["xltx", 262],
    ["xltm", 263],
    ["ods", 259],
    ["ots", 265],
    ["fods", 264],
    ["csv", 260],
    ["pdf", 513],
    ["djvu", 515],
    ["djv", 515],
    ["xps", 516],
    ["oxps", 516],
    ["vsd", 16385],
    ["vsdx", 16385],
    ["vsdm", 16391],
    ["vss", 16387],
    ["vssx", 16387],
    ["vssm", 16393],
    ["vst", 16389],
    ["vstx", 16389],
    ["vstm", 16395],
  ])("maps the extension %s to %i", (ext, code) => {
    expect(getOnlyofficeFileType(ext)).toBe(code);
    expect(getOnlyofficeFileType(`Report.${ext}`)).toBe(code);
    expect(getOnlyofficeFileType(`REPORT.${ext.toUpperCase()}`)).toBe(code);
  });

  it.each([
    ["a name with a dot extension", ".docx", 65],
    ["a name with several dots, by the last one", "archive.pdf.docx", 65],
    ["a name with a dot in a folder part", "v1.2/notes", 0],
    ["an unknown extension", "Pic.png", 0],
    ["a name without an extension", "README", 0],
    ["a name ending in a dot", "Report.", 0],
    ["an empty name", "", 0],
    ["a name of Object.prototype that is not lower case", "a.toString", 0],
  ])("answers %s", (_case, name, code) => {
    expect(getOnlyofficeFileType(name)).toBe(code);
  });

  // DEFECT stor1-6: the table is a plain object, so an extension that names a member of Object.prototype answers that member instead of 0 — "notes.constructor" answers the Object function and "x.__proto__" Object.prototype, which dtoToAttachment then stores as Attachment.type — expected 0 for any name not in the table (the function's own doc comment, onlyofficeFileType.ts:110)
  it("answers a member of Object.prototype for an extension that names one", () => {
    expect(getOnlyofficeFileType("notes.constructor")).toBe(Object);
    expect(getOnlyofficeFileType("x.__proto__")).toBe(Object.prototype);
    expect(getOnlyofficeFileType("hasOwnProperty")).toBe(0);
  });
});
