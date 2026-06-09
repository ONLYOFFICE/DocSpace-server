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

export type ForwardedHeaders = Record<string, string>;

export interface RequestContext {
  headers: ForwardedHeaders;
}

export interface AppConfig {
  name: string;
  port: number;
  appsettings: string;
  environment: string;
  logName?: string;
  proxy?: { url?: string };
  aiService?: { url?: string };
}

// A host-preconfigured MCP server from `appsettings.json` (`ai.mcp`).
// Wired into the engine as a server-side "system tools" source.
export interface McpServerSetting {
  id: string;
  name: string;
  endpoint: string;
}

export interface AwsCloudWatchConfig {
  accessKeyId?: string;
  secretAccessKey?: string;
  region?: string;
  logGroupName?: string;
  logStreamName?: string;
}

export interface RootConfig {
  app: AppConfig;
  aws?: { cloudWatch?: AwsCloudWatchConfig };
  logConsole?: boolean;
  logPath?: string;
  logLevel?: string;
  // Merged from the shared DocSpace `appsettings.json` `ai` section.
  ai?: { mcp?: McpServerSetting[] };
}

export interface ThreadDto {
  id: string;
  title: string;
  lastEditDate: number;
  profileId?: string | null;
}

export interface MessageDto {
  id: string;
  contents: string;
  timestamp: number;
}

export interface ProfileDto {
  id: string;
  name: string;
  providerType: string;
  baseUrl: string;
  key?: string | null;
  modelId: string;
  reasoning?: unknown;
  capabilities?: unknown;
  createdAt: number;
}

export interface McpServerDto {
  name: string;
  config: string;
}
