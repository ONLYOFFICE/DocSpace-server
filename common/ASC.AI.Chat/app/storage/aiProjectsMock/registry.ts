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

// The switch of the AI projects mock and the ids it owns. Kept free of other
// imports so the Files API helpers can ask "is this a mock project?" without
// pulling the mock (and its Files API use) into a cycle.
//
// The mock is on when `AI_PROJECTS_MOCK=1` (or `aiProjects:mock: true` in the
// config) and the app is not running as production. Every
// `/api/2.0/ai/projects…` route is then answered in process from per-user
// in-memory data instead of the .NET AI service, and a chat round inside a
// mock project reads the mock's chat context. See README.md.

import nconf from "../../../config/index.js";
import type { AppConfig } from "../../types.js";

const app: AppConfig | undefined = nconf.get("app");
const IS_PRODUCTION = (app?.environment ?? "").toLowerCase() === "production";

function readFlag(): boolean {
  const fromEnv = process.env["AI_PROJECTS_MOCK"];
  if (fromEnv !== undefined) {
    return fromEnv === "1" || fromEnv.toLowerCase() === "true";
  }
  return nconf.get("aiProjects:mock") === true;
}

const ENABLED = !IS_PRODUCTION && readFlag();

if (ENABLED) {
  console.warn("AI projects mock is ON: /api/2.0/ai/projects is answered in process (see README.md)");
}

/** Whether the AI projects routes are answered by the mock. */
export function isAiProjectsMockEnabled(): boolean {
  return ENABLED;
}

/** Mock project ids start here, far above any real folder id of a dev portal. */
export const MOCK_PROJECT_ID_BASE = 900_000_000;
/** Mock linked-room ids start here (Instructions and Knowledge rooms that exist only in the mock). */
export const MOCK_ROOM_ID_BASE = 950_000_000;
/** The mock projects root (`FolderType.AiProjects`). */
export const MOCK_PROJECTS_ROOT_ID = MOCK_PROJECT_ID_BASE;

/** Titles of the live mock projects, for the folder lookups a chat round makes. */
const projectTitles = new Map<number, string>();

export function registerMockProject(id: number, title: string): void {
  projectTitles.set(id, title);
}

export function unregisterMockProject(id: number): void {
  projectTitles.delete(id);
}

/** The title of a live mock project, or `undefined` for any other id. */
export function mockProjectTitle(id: string | number): string | undefined {
  if (!ENABLED) {
    return undefined;
  }
  const n = typeof id === "number" ? id : Number.parseInt(id, 10);
  return Number.isInteger(n) ? projectTitles.get(n) : undefined;
}
