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

const PATH = "/integration/profiles";

function dtoToProfile(dto) {
  if (!dto) {
    return undefined;
  }
  return {
    id: dto.id,
    name: dto.name,
    providerType: dto.providerType,
    baseUrl: dto.baseUrl,
    key: dto.key ?? undefined,
    modelId: dto.modelId,
    reasoning: dto.reasoning ?? undefined,
    capabilities: dto.capabilities ?? undefined,
    createdAt: dto.createdAt,
  };
}

function toCreateBody(input) {
  return {
    name: input.name,
    providerType: input.providerType,
    baseUrl: input.baseUrl,
    key: input.key ?? null,
    modelId: input.modelId,
    reasoning: input.reasoning ?? null,
    capabilities: input.capabilities ?? null,
  };
}

export class HttpProfilesStorage {
  async create(profile) {
    const dto = await aiService.post(PATH, toCreateBody(profile));
    return dtoToProfile(dto);
  }

  async createMany(profiles) {
    const dtos = await aiService.post(`${PATH}/batch`, {
      profiles: profiles.map(toCreateBody),
    });
    return dtos.map(dtoToProfile);
  }

  async readById(id) {
    try {
      const dto = await aiService.get(`${PATH}/${encodeURIComponent(id)}`);
      return dtoToProfile(dto);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return undefined;
      }
      throw err;
    }
  }

  async readAll() {
    const dtos = await aiService.get(PATH);
    return Array.isArray(dtos) ? dtos.map(dtoToProfile) : [];
  }

  async update(profile) {
    await aiService.put(`${PATH}/${encodeURIComponent(profile.id)}`, toCreateBody(profile));
  }

  async delete(id) {
    try {
      await aiService.delete(`${PATH}/${encodeURIComponent(id)}`);
    } catch (err) {
      if (err instanceof AiServiceHttpError && err.status === 404) {
        return;
      }
      throw err;
    }
  }
}
