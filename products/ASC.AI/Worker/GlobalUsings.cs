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

global using ASC.Api.Core;
global using ASC.AI.Core.Chat.Data;
global using ASC.AI.Core.Chat.Deletion;
global using ASC.Api.Core.Extensions;
global using ASC.AI.Core.Export;
global using ASC.AI.Worker;
global using ASC.AI.Worker.BackgroundServices;
global using ASC.AI.Worker.Extensions;
global using ASC.AI.Worker.Handlers;
global using ASC.AI.Worker.Log;

global using ASC.Core;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.Hosting;
global using ASC.Common;
global using ASC.Common.DependencyInjection;

global using ASC.Common.Threading;

global using ASC.EventBus.Abstractions;
global using ASC.EventBus.Log;

global using ASC.Files.Core;
global using ASC.Files.Core.Core;
global using ASC.Files.Core.EF;
global using ASC.Files.Core.Vectorization;
global using ASC.Files.Core.Vectorization.Events;


global using Autofac;

global using Microsoft.Extensions.Hosting.WindowsServices;

global using NLog;

global using System.Text;

global using ASC.Web.Files.Utils;
