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

extern alias ASCPeople;
extern alias ASCWebApi;

global using System.Diagnostics;
global using System.Net;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;

global using ASC.Tests.Common.ApiFactories;
global using ASC.Tests.Common.Data;
global using ASC.Web.Api.Tests.ApiFactories;

global using DocSpace.API.SDK.Api.Authentication;
global using DocSpace.API.SDK.Api.Capabilities;
global using DocSpace.API.SDK.Api.People;
global using DocSpace.API.SDK.Api.Settings;
global using DocSpace.API.SDK.Client;
global using DocSpace.API.SDK.Model;

global using FluentAssertions;

global using Xunit;

global using ActiveConnectionsApi = DocSpace.API.SDK.Api.Security.ActiveConnectionsApi;
global using ApiKeysApi = DocSpace.API.SDK.Api.ApiKeys.ApiKeysApi;
global using AuditTrailDataApi = DocSpace.API.SDK.Api.Security.AuditTrailDataApi;
global using AuthorizationApi = DocSpace.API.SDK.Api.Settings.AuthorizationApi;
global using CSPApi = DocSpace.API.SDK.Api.Security.CSPApi;
global using FirebaseApi = DocSpace.API.SDK.Api.Security.FirebaseApi;
global using LoginHistoryApi = DocSpace.API.SDK.Api.Security.LoginHistoryApi;
global using MigrationApi = DocSpace.API.SDK.Api.Migration.MigrationApi;
global using OAuth2Api = DocSpace.API.SDK.Api.Security.OAuth2Api;
global using PaymentApi = DocSpace.API.SDK.Api.Portal.PaymentApi;
global using PortalGuestsApi = DocSpace.API.SDK.Api.Portal.GuestsApi;
global using PortalQuotaApi = DocSpace.API.SDK.Api.Portal.QuotaApi;
global using PortalSettingsApi = DocSpace.API.SDK.Api.Portal.SettingsApi;
global using PortalUsersApi = DocSpace.API.SDK.Api.Portal.UsersApi;
global using SecurityAccessToDevToolsApi = DocSpace.API.SDK.Api.Security.AccessToDevToolsApi;
global using SecurityBannersVisibilityApi = DocSpace.API.SDK.Api.Security.BannersVisibilityApi;
global using SettingsQuotaApi = DocSpace.API.SDK.Api.Settings.QuotaApi;
global using SMTPSettingsApi = DocSpace.API.SDK.Api.Security.SMTPSettingsApi;
global using Task = System.Threading.Tasks.Task;
global using User = ASC.Tests.Common.Data.User;
