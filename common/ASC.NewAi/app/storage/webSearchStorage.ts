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

import {
    aiService,
    AiServiceHttpError,
    aiServiceBaseUrl,
} from "./httpClient.js";
import { isObject, getString, getBoolean } from "../narrow.js";
import {
    getForwardedHeaders,
    shouldForwardHeadersToProvider,
} from "../requestContext.js";
import type {
    WebSearchStorage,
    WebSearchConfig,
} from "@onlyoffice/ai-chat/core";
import {
    invalidateChatContext,
    readChatContext,
    reportChatContextMiss,
} from "./chatContextSnapshot.js";

const PATH = "/web-search";
const ONLYOFFICE_GATEWAY_PATH = "/api/2.0/ai/gateway";

export function withWebSearchProviderOverrides(
    config: WebSearchConfig | null,
): WebSearchConfig | null {
    if (
        !config
        || config.provider !== "onlyoffice"
        || !shouldForwardHeadersToProvider()
    ) {
        return config;
    }
    return {
        ...config,
        isCloudProvider: true,
        baseUrl: `${aiServiceBaseUrl}${ONLYOFFICE_GATEWAY_PATH}/`,
        headers: {
            ...getForwardedHeaders(),
            ...(config.headers ?? {}),
        },
    };
}

function toBody(config: WebSearchConfig): Record<string, unknown> {
    const body: Record<string, unknown> = { provider: config.provider };
    if (config.key !== undefined) {
        body["key"] = config.key;
    }
    if (config.baseUrl !== undefined) {
        body["baseUrl"] = config.baseUrl;
    }
    if (config.isCloudProvider !== undefined) {
        body["isCloudProvider"] = config.isCloudProvider;
    }
    return body;
}

export function parseWebSearchConfig(raw: unknown): WebSearchConfig | null {
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
    const isCloudProvider = getBoolean(raw, "isCloudProvider");
    if (isCloudProvider !== undefined) {
        config.isCloudProvider = isCloudProvider;
    }
    return config;
}

export class HttpWebSearchStorage implements WebSearchStorage {
    // The AI service exposes a single global web-search singleton; entityId
    // scoping is not supported server-side and is ignored here.
    async create(config: WebSearchConfig, _entityId?: string): Promise<void> {
        await aiService.put(PATH, toBody(config));
        invalidateChatContext("webSearch");
    }

    async read(_entityId?: string): Promise<WebSearchConfig | null> {
        const snapshot = readChatContext("webSearch");
        if (snapshot) {
            return withWebSearchProviderOverrides(snapshot.webSearch);
        }
        reportChatContextMiss("webSearch.read");
        try {
            const raw = await aiService.get(PATH);
            return withWebSearchProviderOverrides(parseWebSearchConfig(raw));
        } catch (err) {
            if (err instanceof AiServiceHttpError && err.status === 404) {
                return null;
            }
            throw err;
        }
    }

    async update(config: WebSearchConfig, _entityId?: string): Promise<void> {
        await aiService.put(PATH, toBody(config));
        invalidateChatContext("webSearch");
    }

    async upsert(config: WebSearchConfig, _entityId?: string): Promise<void> {
        await aiService.put(PATH, toBody(config));
        invalidateChatContext("webSearch");
    }

    async delete(_entityId?: string): Promise<void> {
        invalidateChatContext("webSearch");
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
