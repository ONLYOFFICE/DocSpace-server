// (c) Copyright Ascensio System SIA 2009-2024
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

global using System.Data;
global using System.IO.Compression;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;

global using ASC.Common;
global using ASC.Common.Log;
global using ASC.Migration.Core.Core.Providers.ASC.Models;
global using ASC.Common.Threading;
global using ASC.Common.Web;
global using ASC.Core;
global using ASC.Core.Common.Quota;
global using ASC.Core.Common.Quota.Features;
global using ASC.Core.Users;
global using ASC.Data.Storage;
global using ASC.Data.Storage.DataOperators;
global using ASC.Data.Storage.DiscStorage;
global using ASC.EventBus.Abstractions;
global using ASC.EventBus.Events;
global using ASC.Files.Core;
global using ASC.Files.Core.Helpers;
global using ASC.Migration.Core.Core.Providers.ASC.Models.Parse;
global using ASC.Files.Core.Resources;
global using ASC.Files.Core.Security;
global using ASC.Migration.Core;
global using ASC.Migration.Core.Core;
global using ASC.Migration.Core.Core.Providers;
global using ASC.Migration.Core.Core.Providers.Models;
global using ASC.Migration.Core.Core.Providers.Models.Parse;
global using ASC.Migration.Core.Models;
global using ASC.Migration.Core.Models.Api;
global using ASC.Migration.Core.Resources;
global using ASC.Migration.GoogleWorkspace;
global using ASC.Migration.GoogleWorkspace.Models;
global using ASC.Migration.GoogleWorkspace.Models.Parse;
global using ASC.Migration.NextcloudWorkspace;
global using ASC.Migration.NextcloudWorkspace.Models;
global using ASC.Migration.NextcloudWorkspace.Models.Parse;
global using ASC.Migration.OwnCloud;
global using ASC.Migration.OwnCloud.Models;
global using ASC.Web.Files.Classes;
global using ASC.Web.Files.Services.WCFService;
global using ASC.Files.Core.ApiModels;
global using ASC.Files.Core.ApiModels.RequestDto;

global using HtmlAgilityPack;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.IdentityModel.Tokens;

global using ProtoBuf;
