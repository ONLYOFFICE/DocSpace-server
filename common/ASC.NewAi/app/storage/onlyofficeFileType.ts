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

// ONLYOFFICE `c_oAscFileType` codes keyed by extension. This is the scale the
// chat widget's attachment chip expects in `Attachment.type`: it picks the icon
// with bitmask predicates (documents 0x40, presentations 0x80, spreadsheets
// 0x100, the exact PDF-family codes, Visio 0x4000), so a value from any other
// scale falls through to the "unknown format" icon.
//
// Mirrors `getOnlyofficeFileType` on the client side
// (`client/libs/ui-kit/ai-agent/providers/files/file-type.ts`) — the two tables
// must stay in sync, since the client sends these codes on attach and this one
// rebuilds them when the record is read back.
const EXT_TO_CODE: Record<string, number> = {
  // Documents (category bit 6 = 64)
  doc: 66,
  docx: 65,
  docm: 75,
  dotx: 76,
  dotm: 77,
  odt: 67,
  ott: 79,
  fodt: 78,
  rtf: 68,
  txt: 69,
  mht: 71,
  html: 70,
  htm: 70,
  xml: 70,
  epub: 72,
  fb2: 73,
  mobi: 74,
  docxf: 83,
  oform: 84,
  md: 69,
  // Presentations (bit 7 = 128)
  ppt: 130,
  pptx: 129,
  pptm: 134,
  ppsx: 132,
  ppsm: 133,
  potx: 135,
  potm: 136,
  odp: 131,
  otp: 138,
  fodp: 137,
  // Spreadsheets (bit 8 = 256)
  xls: 258,
  xlsx: 257,
  xlsm: 261,
  xltx: 262,
  xltm: 263,
  ods: 259,
  ots: 265,
  fods: 264,
  csv: 260,
  // PDF family (exact codes — the widget's predicates compare to literals)
  pdf: 513,
  djvu: 515,
  djv: 515,
  xps: 516,
  oxps: 516,
  // Visio (bit 14 = 16384)
  vsd: 16385,
  vsdx: 16385,
  vsdm: 16391,
  vss: 16387,
  vssx: 16387,
  vssm: 16393,
  vst: 16389,
  vstx: 16389,
  vstm: 16395,
};

function extensionOf(titleOrExt: string): string {
  const dot = titleOrExt.lastIndexOf(".");
  return (dot >= 0 ? titleOrExt.slice(dot + 1) : titleOrExt).toLowerCase();
}

/**
 * Maps a file name (or a bare extension) to its ONLYOFFICE `c_oAscFileType`
 * code; `0` ("unknown") for anything not in the table.
 */
export function getOnlyofficeFileType(titleOrExt: string): number {
  return EXT_TO_CODE[extensionOf(titleOrExt)] ?? 0;
}
