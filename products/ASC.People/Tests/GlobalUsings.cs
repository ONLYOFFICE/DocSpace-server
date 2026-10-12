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

global using System.Diagnostics;
global using System.Net;
global using System.Text;
global using System.Text.Json;

global using ASC.People.Tests.ApiFactories;
global using ASC.Tests.Common.ApiFactories;
global using ASC.Tests.Common.Data;

global using DocSpace.API.SDK.Api.Group;
global using DocSpace.API.SDK.Api.People;
global using DocSpace.API.SDK.Api.Portal;
global using DocSpace.API.SDK.Api.Rooms;
global using DocSpace.API.SDK.Api.Settings;
global using DocSpace.API.SDK.Client;
global using DocSpace.API.SDK.Model;

global using FluentAssertions;

global using Xunit;
// Files-service clients the People tests need for room-scoped scenarios. Imported one by one
// instead of the whole namespace: DocSpace.API.SDK.Api.Files also carries QuotaApi and SettingsApi,
// which would clash with the People and Settings ones already in scope here.
global using FilesApi = DocSpace.API.SDK.Api.Files.FilesApi;
global using FileShare = DocSpace.API.SDK.Model.FileShare;
global using FoldersApi = DocSpace.API.SDK.Api.Files.FoldersApi;
// Both the People and the Group areas publish a SearchApi, so neither can be used unqualified.
global using GroupSearchApi = DocSpace.API.SDK.Api.Group.SearchApi;
// QuotaApi and GuestsApi each exist in more than one of the namespaces above, so the People ones
// are reached through an alias rather than unqualified.
global using PeopleGuestsApi = DocSpace.API.SDK.Api.People.GuestsApi;
global using PeopleQuotaApi = DocSpace.API.SDK.Api.People.QuotaApi;
global using PeopleSearchApi = DocSpace.API.SDK.Api.People.SearchApi;
global using RoomType = DocSpace.API.SDK.Model.RoomType;
global using SharingApi = DocSpace.API.SDK.Api.Files.SharingApi;
global using User = ASC.Tests.Common.Data.User;
