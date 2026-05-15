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

global using System.Collections.Immutable;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Globalization;
global using System.Security;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;

global using ASC.Api.Core;
global using ASC.Api.Core.Convention;
global using ASC.Api.Core.Core;
global using ASC.Api.Core.Extensions;
global using ASC.Api.Core.Routing;
global using ASC.Api.Utils;
global using ASC.Common;
global using ASC.Common.Threading;
global using ASC.Common.Web;
global using ASC.Core;
global using ASC.Core.Billing;
global using ASC.Core.Common;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.Settings;
global using ASC.Core.Tenants;
global using ASC.Core.Users;
global using ASC.Data.Backup.Core.Quota;
global using ASC.Data.Backup.EF.Context;
global using ASC.EventBus.Abstractions;
global using ASC.FederatedLogin.Helpers;
global using ASC.FederatedLogin.LoginProviders;
global using ASC.Files.ApiModels.RequestDto;
global using ASC.Files.Core;
global using ASC.Files.Core.ApiModels;
global using ASC.Files.Core.ApiModels.RequestDto;
global using ASC.Files.Core.ApiModels.ResponseDto;
global using ASC.Files.Core.Core;
global using ASC.Files.Core.Core.Entries;
global using ASC.Files.Core.EF;
global using ASC.Files.Core.Entries;
global using ASC.Files.Core.Helpers;
global using ASC.Files.Core.IntegrationEvents.Events;
global using ASC.Files.Core.Resources;
global using ASC.Files.Core.RoomTemplates;
global using ASC.Files.Core.RoomTemplates.Events;
global using ASC.Files.Core.Security;
global using ASC.Files.Core.Services.DocumentBuilderService;
global using ASC.Files.Core.Services.ExternalDbSync;
global using ASC.Files.Core.VirtualRooms;
global using ASC.Files.Extension;
global using ASC.Files.Helpers;
global using ASC.Files.Log;
global using ASC.MessagingSystem;
global using ASC.MessagingSystem.Core;
global using ASC.Web.Api.Core;
global using ASC.Web.Api.Models;
global using ASC.Web.Api.Routing;
global using ASC.Web.Core;
global using ASC.Web.Core.Files;
global using ASC.Web.Core.PublicResources;
global using ASC.Web.Core.Users;
global using ASC.Web.Files;
global using ASC.Web.Files.Classes;
global using ASC.Web.Files.Configuration;
global using ASC.Files.Core.Core.AI;
global using ASC.Web.Files.Core.Compress;
global using ASC.Web.Files.Core.Entries;
global using ASC.Web.Files.Helpers;
global using ASC.Web.Files.HttpHandlers;
global using ASC.Web.Files.Services.DocumentService;
global using ASC.Web.Files.Services.WCFService;
global using ASC.Web.Files.Services.WCFService.FileOperations;
global using ASC.Web.Files.Utils;
global using ASC.Web.Studio.Core;
global using ASC.Web.Studio.Core.Notify;
global using ASC.Web.Studio.Utility;
global using ASC.Webhooks.Core;

global using Autofac;

global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.RateLimiting;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Hosting.WindowsServices;

global using Newtonsoft.Json.Linq;

global using Riok.Mapperly.Abstractions;

global using Swashbuckle.AspNetCore.Annotations;

global using ZiggyCreatures.Caching.Fusion;

global using FileShare = ASC.Files.Core.Security.FileShare;
global using SecurityContext = ASC.Core.SecurityContext;