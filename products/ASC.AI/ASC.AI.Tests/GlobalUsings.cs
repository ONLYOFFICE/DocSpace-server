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

global using System.Diagnostics;
global using System.Net;
global using System.Net.Http.Json;
global using System.Text.Json;

global using ASC.AI.Integration.Profiles;
global using ASC.AI.Integration.ToolPrefs;
global using ASC.AI.Tests.ApiFactories;
global using ASC.Core.Users;
global using ASC.Tests.Common.ApiFactories;
global using ASC.Tests.Common.Data;

global using FluentAssertions;

global using Xunit;

global using CreateProfileRequestDto = ASCAi::ASC.AI.Models.RequestDto.Profiles.CreateProfileRequestDto;
global using CreateProfilesRequestDto = ASCAi::ASC.AI.Models.RequestDto.Profiles.CreateProfilesRequestDto;
global using McpServerDto = ASCAi::ASC.AI.Models.ResponseDto.McpServerDto;
global using MessageDto = ASCAi::ASC.AI.Models.ResponseDto.MessageDto;
global using MessagesPageDto = ASCAi::ASC.AI.Models.ResponseDto.MessagesPageDto;
global using PreferencesDto = ASCAi::ASC.AI.Models.ResponseDto.PreferencesDto;
global using ProfileDto = ASCAi::ASC.AI.Models.ResponseDto.ProfileDto;
global using Task = System.Threading.Tasks.Task;
global using PromptDto = ASCAi::ASC.AI.Models.ResponseDto.PromptDto;
global using PromptFolderDto = ASCAi::ASC.AI.Models.ResponseDto.PromptFolderDto;
global using ThreadDto = ASCAi::ASC.AI.Models.ResponseDto.ThreadDto;
global using ThreadsPageDto = ASCAi::ASC.AI.Models.ResponseDto.ThreadsPageDto;
global using UpdateProfileBody = ASCAi::ASC.AI.Models.RequestDto.Profiles.UpdateProfileBody;
global using User = ASC.Tests.Common.Data.User;
