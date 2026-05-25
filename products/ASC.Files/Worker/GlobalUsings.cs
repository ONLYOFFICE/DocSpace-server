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

global using System.Globalization;
global using System.Text.Json;
global using System.Threading.Channels;

global using ASC.Api.Core;
global using ASC.Api.Core.Extensions;
global using ASC.Common;
global using ASC.Common.DependencyInjection;
global using ASC.Common.Log;
global using ASC.Common.Threading;
global using ASC.Core;
global using ASC.Core.Billing;
global using ASC.Core.ChunkedUploader;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.Hosting;
global using ASC.Core.Tenants;
global using ASC.ElasticSearch;
global using ASC.ElasticSearch.Service;
global using ASC.EventBus.Abstractions;
global using ASC.EventBus.Exceptions;
global using ASC.EventBus.Log;
global using ASC.Files.Core;
global using ASC.Files.Core.Core;
global using ASC.Files.Core.EF;
global using ASC.Files.Core.Helpers;
global using ASC.Files.Core.IntegrationEvents.Events;
global using ASC.Files.Core.Log;
global using ASC.Files.Core.Resources;
global using ASC.Files.Core.RoomTemplates.Events;
global using ASC.Files.Core.Security;
global using ASC.Files.Core.Services.DocumentBuilderService;
global using ASC.Files.Core.Services.ExternalDbSync;
global using ASC.Files.Core.VirtualRooms;
global using ASC.Files.Worker;
global using ASC.Files.Worker.Extension;
global using ASC.Files.Worker.IntegrationEvents.EventHandling;
global using ASC.Files.Worker.Log;
global using ASC.Files.Worker.Services.Thumbnail;
global using ASC.Files.ThumbnailBuilder;
global using ASC.Web.Core;
global using ASC.Web.Files.Classes;
global using ASC.Web.Files.Services.DocumentService;
global using ASC.Web.Files.Services.FFmpegService;
global using ASC.Web.Files.Services.WCFService.FileOperations;
global using ASC.Web.Files.Utils;
global using ASC.Web.Studio.Utility;

global using Autofac;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Hosting.WindowsServices;
global using Microsoft.Extensions.Logging;

global using static ASC.Files.Core.Helpers.DocumentService;