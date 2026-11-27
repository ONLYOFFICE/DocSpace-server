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

import type { PluginAgentConfig } from "../../../config/index.js";
import type { ToolContext } from "../agentTools.js";
import type { PluginSkill } from "../skill.js";
import { GATEWAY_URL } from "../../__tests__/helpers/sandboxGateway.js";

export const CONFIG: PluginAgentConfig = {
  sandboxUrl: GATEWAY_URL,
  sandboxToken: "gw-secret",
  taskTimeoutSeconds: 1800,
  maxToolCalls: 150,
  maxConcurrentTasks: 2,
  sandboxTtlSeconds: 600,
  serviceUrl: "http://chat:9837",
  registryUrl: "https://registry.npmjs.org",
  cacheDir: "/nowhere",
  cacheMaxBytes: 1,
};

export const SESSION_ENV = { NPM_CONFIG_USERCONFIG: "/workspace/.npmrc", CI: "1" };

/** A tool context over the fake gateway's sandbox `sb-1`. */
export function toolContext(skill: Partial<PluginSkill> = {}, signal?: AbortSignal): ToolContext {
  return {
    config: CONFIG,
    session: {
      id: "sb-1",
      env: SESSION_ENV,
      lost: new AbortController().signal,
      dispose: () => Promise.resolve(),
    },
    skill: { dir: "/no-skill", instructions: "", sandboxFiles: [], ...skill },
    signal: signal ?? new AbortController().signal,
  };
}
