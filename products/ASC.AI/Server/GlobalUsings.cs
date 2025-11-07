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

global using ASC.Api.Core;
global using ASC.Api.Core.Core;
global using ASC.Api.Core.Convention;
global using ASC.Api.Utils;

global using ASC.AI.Core.Chat;
global using ASC.AI.Core.Chat.Data;
global using ASC.AI.Core.Chat.Completion;
global using ASC.AI.Core.Provider;
global using ASC.AI.Core.Provider.Data;
global using ASC.AI.Models.ResponseDto;
global using ASC.AI.Models.RequestDto;
global using ASC.AI.Core.MCP;
global using ASC.AI.Core.MCP.Data;
global using ASC.AI.Core.Settings;
global using ASC.AI.Core.Retrieval.Web;
global using ASC.AI.Core.Retrieval.Knowledge;
global using ASC.Core;

global using ASC.Common;
global using ASC.Common.Log;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.EF.Model.Ai;
global using ASC.Core.Common.Settings;
global using ASC.Core.Tenants;
global using ASC.Files.Core;
global using ASC.Files.Core.ApiModels.RequestDto;
global using ASC.Files.Core.ApiModels.ResponseDto;
global using ASC.Files.Core.Core;
global using ASC.Files.Core.EF;
global using ASC.Files.Core.VirtualRooms;
global using ASC.Files.Core.Vectorization;
global using ASC.Files.Core.Vectorization.Settings;
global using ASC.MessagingSystem.Core;
global using ASC.Web.Api.Models;
global using ASC.Web.Api.Routing;
global using ASC.Web.Files.Classes;
global using ASC.Web.Files.Helpers;
global using ASC.Web.Files.Services.WCFService;
global using ASC.Web.Files.Services.WCFService.FileOperations;

global using Microsoft.AspNetCore.Mvc;

global using System.ComponentModel.DataAnnotations;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading.Channels;

global using Swashbuckle.AspNetCore.Annotations;

global using Riok.Mapperly.Abstractions;
