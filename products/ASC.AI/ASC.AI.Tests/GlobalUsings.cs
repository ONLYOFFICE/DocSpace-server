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

extern alias ASCAi;

global using System.Data.Common;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Net.Http.Json;
global using System.Text;
global using System.Text.Encodings.Web;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Web;

global using ASC.AI.Tests.ApiFactories;
global using ASC.AI.Tests.Data;
global using ASC.Core.Common.EF;

global using Aspire.Hosting;
global using Aspire.Hosting.Testing;

global using Bogus;

global using FluentAssertions;

global using Microsoft.AspNetCore.Cryptography.KeyDerivation;
global using Microsoft.Extensions.Configuration;

global using MySql.Data.MySqlClient;

global using Npgsql;

global using Respawn;
global using Respawn.Graph;

global using Xunit;

global using ASC.AI.Integration.Profiles;
global using ASC.AI.Integration.ToolPrefs;
global using ASC.Core.Users;

global using CreateProfileRequestDto = ASCAi::ASC.AI.Models.RequestDto.Integration.CreateProfileRequestDto;
global using CreateProfilesRequestDto = ASCAi::ASC.AI.Models.RequestDto.Integration.CreateProfilesRequestDto;
global using McpServerDto = ASCAi::ASC.AI.Models.ResponseDto.Integration.McpServerDto;
global using MessageDto = ASCAi::ASC.AI.Models.ResponseDto.Integration.MessageDto;
global using PreferencesDto = ASCAi::ASC.AI.Models.ResponseDto.Integration.PreferencesDto;
global using ProfileDto = ASCAi::ASC.AI.Models.ResponseDto.Integration.ProfileDto;
global using ThreadDto = ASCAi::ASC.AI.Models.ResponseDto.Integration.ThreadDto;
global using UpdateProfileBody = ASCAi::ASC.AI.Models.RequestDto.Integration.UpdateProfileBody;

global using Task = System.Threading.Tasks.Task;
