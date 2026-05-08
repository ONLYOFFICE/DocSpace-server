// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

import { aiService, AiServiceHttpError } from "./httpClient.js";
import { isObject, getString } from "../narrow.js";
import type { WebSearchStorage, WebSearchConfig } from "@onlyoffice/ai-chat/core";

const PATH = "/integration/web-search";

function toBody(config: WebSearchConfig): Record<string, unknown> {
  const body: Record<string, unknown> = { provider: config.provider };
  if (config.key !== undefined) {
    body["key"] = config.key;
  }
  if (config.baseUrl !== undefined) {
    body["baseUrl"] = config.baseUrl;
  }
  return body;
}

function parseConfig(raw: unknown): WebSearchConfig | null {
  if (!isObject(raw)) {
    return null;
  }
  const provider = getString(raw, "provider");
  if (!provider) {
    return null;
  }
  const config: WebSearchConfig = { provider };
  const key = getString(raw, "key");
  if (key !== undefined) {
    config.key = key;
  }
  const baseUrl = getString(raw, "baseUrl");
  if (baseUrl !== undefined) {
    config.baseUrl = baseUrl;
  }
  return config;
}

export class HttpWebSearchStorage implements WebSearchStorage {
  // The AI service exposes a single global web-search singleton; entityId
  // scoping is not supported server-side and is ignored here.
  async create(config: WebSearchConfig, _entityId?: string): Promise<void> {
    await aiService.put(PATH, toBody(config));
  }

  async read(_entityId?: string): Promise<WebSearchConfig | null> {
    try {
      const raw = await aiService.get(PATH);
      return parseConfig(raw);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async update(config: WebSearchConfig, _entityId?: string): Promise<void> {
    await aiService.put(PATH, toBody(config));
  }

  async upsert(config: WebSearchConfig, _entityId?: string): Promise<void> {
    await aiService.put(PATH, toBody(config));
  }

  async delete(_entityId?: string): Promise<void> {
    try {
      await aiService.delete(PATH);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }
}
