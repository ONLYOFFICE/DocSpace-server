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

const THREADS_PATH = "/integration/threads";
const MESSAGES_PATH = "/integration/messages";

function serializeContents(message) {
  return JSON.stringify(message);
}

function parseContents(contents) {
  if (typeof contents !== "string") {
    return contents ?? {};
  }
  try {
    return JSON.parse(contents);
  } catch {
    return { content: contents };
  }
}

function dtoToMessage(dto) {
  if (!dto) {
    return null;
  }
  const body = parseContents(dto.contents);
  return {
    ...body,
    id: dto.id,
    createdAt: dto.timestamp,
  };
}

export class HttpMessagesStorage {
  async create(threadId, message) {
    const dto = await aiService.post(
      `${THREADS_PATH}/${encodeURIComponent(threadId)}/messages`,
      { contents: serializeContents(message) },
    );
    return dtoToMessage(dto);
  }

  async readById(messageId) {
    try {
      const dto = await aiService.get(`${MESSAGES_PATH}/${encodeURIComponent(messageId)}`);
      return dtoToMessage(dto);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return null;
      }
      throw err;
    }
  }

  async readByThread(threadId, limit, startIndex) {
    const query = {};
    if (limit !== undefined && limit !== null) {
      query.limit = limit;
    }
    if (startIndex !== undefined && startIndex !== null) {
      query.startIndex = startIndex;
    }
    const dtos = await aiService.get(
      `${THREADS_PATH}/${encodeURIComponent(threadId)}/messages`,
      Object.keys(query).length > 0 ? { query } : undefined,
    );
    return Array.isArray(dtos) ? dtos.map(dtoToMessage) : [];
  }

  async update(messageId, message) {
    await aiService.put(`${MESSAGES_PATH}/${encodeURIComponent(messageId)}`, {
      contents: serializeContents(message),
    });
  }

  async delete(messageId) {
    try {
      await aiService.delete(`${MESSAGES_PATH}/${encodeURIComponent(messageId)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }

  async deleteByThread(threadId) {
    try {
      await aiService.delete(`${THREADS_PATH}/${encodeURIComponent(threadId)}/messages`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }
}
