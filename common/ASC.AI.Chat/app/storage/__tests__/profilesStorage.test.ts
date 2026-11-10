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

import { describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));
vi.mock("../httpClient.js", () => ({
  aiServiceBaseUrl: "http://ai-service",
  aiService: {},
  AiServiceHttpError: class extends Error {},
  withTimeout: () => ({ signal: undefined, cancel: () => {} }),
}));
vi.mock("../../requestContext.js", () => ({
  countUpstreamCall: vi.fn(),
  countUpstreamRead: vi.fn(),
  getForwardedHeaders: () => ({}),
  shouldForwardHeadersToProvider: () => false,
}));
vi.mock("../chatContextSnapshot.js", () => ({
  invalidateChatContext: vi.fn(),
  readChatContext: () => undefined,
  reportChatContextMiss: vi.fn(),
}));

const { onlyofficeGatewayPath, withOnlyofficeProviderOverrides } =
  await import("../profilesStorage.js");
type DocspaceProfile = import("../profilesStorage.js").DocspaceProfile;

const profile = (overrides: Partial<DocspaceProfile> = {}): DocspaceProfile => ({
  id: "0f2c6d8e-1a2b-4c3d-9e8f-123456789abc",
  name: "Claude",
  providerType: "onlyoffice",
  baseUrl: "https://portal.example",
  modelId: "claude-sonnet",
  createdAt: 0,
  ...overrides,
});

describe("onlyofficeGatewayPath", () => {
  it("is the gateway proxy for a profile on the built-in key", () => {
    expect(onlyofficeGatewayPath(profile())).toBe("/api/2.0/ai/gateway");
  });

  it("is the BYOK route of the profile when it runs on the portal's own key", () => {
    expect(onlyofficeGatewayPath(profile({ byokProvider: "anthropic" }))).toBe(
      "/api/2.0/ai/gateway/byok/0f2c6d8e-1a2b-4c3d-9e8f-123456789abc",
    );
  });

  it("encodes the profile id in the path", () => {
    expect(onlyofficeGatewayPath(profile({ id: "a/b", byokProvider: "openai" }))).toBe(
      "/api/2.0/ai/gateway/byok/a%2Fb",
    );
  });
});

describe("withOnlyofficeProviderOverrides", () => {
  it("points a BYOK profile at the BYOK route of the AI service", () => {
    const resolved = withOnlyofficeProviderOverrides(profile({ byokProvider: "anthropic" }));
    expect(resolved?.baseUrl).toBe(
      "http://ai-service/api/2.0/ai/gateway/byok/0f2c6d8e-1a2b-4c3d-9e8f-123456789abc",
    );
    expect(resolved?.byokProvider).toBe("anthropic");
  });

  it("points a built-in profile at the gateway proxy of the AI service", () => {
    expect(withOnlyofficeProviderOverrides(profile())?.baseUrl).toBe(
      "http://ai-service/api/2.0/ai/gateway",
    );
  });

  it("leaves a third-party profile as stored", () => {
    const stored = profile({ providerType: "openai", baseUrl: "https://api.openai.com/v1" });
    expect(withOnlyofficeProviderOverrides(stored)).toBe(stored);
  });
});
