// (c) Copyright Ascensio System SIA 2009-2026
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

extern alias ASCWebApi;
extern alias ASCFiles;
extern alias ASCPeople;
extern alias ASCFilesService;
global using System.Data.Common;
global using System.Net;
global using System.Net.Http.Headers;
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

global using MySql.Data.MySqlClient;

global using Npgsql;

global using Respawn;
global using Respawn.Graph;

global using Xunit;

global using ASC.Core.Common.EF;
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
