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

extern alias ASCWebApi;
extern alias ASCFiles;
extern alias ASCPeople;
extern alias ASCFilesService;
global using System.Data.Common;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Web;

global using ASC.Files.Tests.Data;

global using Aspire.Hosting;
global using Aspire.Hosting.Testing;

global using Bogus;
global using Bogus.DataSets;

global using DocSpace.API.SDK.Api.Authentication;
global using DocSpace.API.SDK.Api.Files;
global using DocSpace.API.SDK.Api.People;
global using DocSpace.API.SDK.Api.Portal;
global using DocSpace.API.SDK.Api.Rooms;
global using DocSpace.API.SDK.Api.Settings;
global using DocSpace.API.SDK.Client;
global using DocSpace.API.SDK.Model;

global using FluentAssertions;

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;

global using MySql.Data.MySqlClient;

global using Npgsql;

global using Respawn;
global using Respawn.Graph;

global using Xunit;

global using ASC.Core.Common.EF;
global using ASC.Files.Core.Text;
global using ASC.Files.Tests.ApiFactories;

global using Microsoft.AspNetCore.Cryptography.KeyDerivation;

global using ApiDateTime = DocSpace.API.SDK.Model.ApiDateTime;
global using CreateFolder = DocSpace.API.SDK.Model.CreateFolder;
global using CreateRoomRequestDto = DocSpace.API.SDK.Model.CreateRoomRequestDto;
global using ExternalShareRequestParam = DocSpace.API.SDK.Model.ExternalShareRequestParam;
global using FileLinkRequest = DocSpace.API.SDK.Model.FileLinkRequest;
global using FileOperationDto = DocSpace.API.SDK.Model.FileOperationDto;
global using FileShare = DocSpace.API.SDK.Model.FileShare;
global using FolderType = DocSpace.API.SDK.Model.FolderType;
global using RoomType = DocSpace.API.SDK.Model.RoomType;
global using User = ASC.Files.Tests.Data.User;

global using LinkType = DocSpace.API.SDK.Model.LinkType;
global using Task = System.Threading.Tasks.Task;
