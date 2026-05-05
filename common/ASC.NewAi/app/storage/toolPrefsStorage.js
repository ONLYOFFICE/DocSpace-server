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

const DISABLED_PATH = "/integration/tool-prefs/disabled";

// `allowAlways` is not exposed by the AI service yet — keep it in-memory until
// the backend grows the matching endpoints.
export class HttpToolPrefsStorage {
  #allowAlways = null;

  async createDisabled(disabled) {
    await aiService.put(DISABLED_PATH, { disabled });
  }

  async readDisabled() {
    try {
      const map = await aiService.get(DISABLED_PATH);
      return map && typeof map === "object" ? map : {};
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return {};
      }
      throw err;
    }
  }

  async updateDisabled(disabled) {
    await aiService.put(DISABLED_PATH, { disabled });
  }

  async upsertDisabled(disabled) {
    await aiService.put(DISABLED_PATH, { disabled });
  }

  async deleteDisabled() {
    try {
      await aiService.delete(DISABLED_PATH);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }

  async createAllowAlways(tokens) {
    if (this.#allowAlways !== null) {
      throw new Error("allowAlways tokens already set");
    }
    this.#allowAlways = [...tokens];
  }

  async readAllowAlways() {
    return this.#allowAlways ? [...this.#allowAlways] : [];
  }

  async updateAllowAlways(tokens) {
    if (this.#allowAlways === null) {
      throw new Error("allowAlways tokens not set");
    }
    this.#allowAlways = [...tokens];
  }

  async upsertAllowAlways(tokens) {
    this.#allowAlways = [...tokens];
  }

  async deleteAllowAlways() {
    this.#allowAlways = null;
  }

  _clear() {
    this.#allowAlways = null;
  }
}
