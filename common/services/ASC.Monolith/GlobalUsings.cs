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

global using System.Collections.Concurrent;
global using System.Text;
global using System.Text.Encodings.Web;
global using System.Threading.Channels;

global using ASC.ActiveDirectory.Base;
global using ASC.ActiveDirectory.ComplexOperations;
global using ASC.AI.Core.Chat.Deletion;
global using ASC.AI.Core.Chat.Tool;
global using ASC.AI.Core.Export;
global using ASC.AI.Extensions;
global using ASC.AI.Worker.BackgroundServices;
global using ASC.AI.Worker.Extensions;
global using ASC.Api.Core;
global using ASC.Api.Core.Auth;
global using ASC.Api.Core.Core;
global using ASC.Api.Core.Extensions;
global using ASC.ApiSystem.Classes;
global using ASC.Api.Settings.Smtp;
global using ASC.ClearEvents.Extensions;
global using ASC.ClearEvents.Services;
global using ASC.Common.IntegrationEvents.Events;
global using ASC.Common.Threading;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.EF.Context;
global using ASC.Core.Common.Notify.Engine;
global using ASC.Core.Common.Notify.IntegrationEvents.Events;
global using ASC.Data.Backup.Core.IntegrationEvents.Events;
global using ASC.Data.Backup.Core.Quota;
global using ASC.Data.Backup.EF.Context;
global using ASC.Data.Backup.Extension;
global using ASC.Data.Backup.Services;
global using ASC.Data.Backup.Worker;
global using ASC.Data.Backup.Worker.Extension;
global using ASC.Data.Reassigns;
global using ASC.Data.Storage;
global using ASC.Data.Storage.Encryption;
global using ASC.Data.Storage.Encryption.IntegrationEvents.Events;
global using ASC.ElasticSearch;
global using ASC.ElasticSearch.Service;
global using ASC.EventBus.Abstractions;
global using ASC.EventBus.Events;
global using ASC.FederatedLogin;
global using ASC.Files.Core.Core;
global using ASC.Files.Core.EF;
global using ASC.Files.Core.Helpers;
global using ASC.Files.Core.IntegrationEvents.Events;
global using ASC.Files.Core.RoomTemplates.Events;
global using ASC.Files.Core.RoomTemplates.Operations;
global using ASC.Files.Core.Services.DocumentBuilderService;
global using ASC.Files.Core.Services.NotifyService;
global using ASC.Files.Core.Vectorization;
global using ASC.Files.Core.Vectorization.Events;
global using ASC.Files.Extension;
global using ASC.Files.Worker.Extension;
global using ASC.Files.Worker.Services;
global using ASC.Files.Worker.Services.Thumbnail;
global using ASC.MessagingSystem;
global using ASC.MessagingSystem.Data;
global using ASC.Migration.Core;
global using ASC.Migration.Core.Core;
global using ASC.Monolith;
global using ASC.Notify;
global using ASC.Notify.Extension;
global using ASC.Notify.Services;
global using ASC.TelegramService.Extension;
global using ASC.TelegramService.Services;
global using ASC.Web.Api.Core;
global using ASC.Web.Core;
global using ASC.Web.Core.HttpHandlers;
global using ASC.Web.Core.RemovePortal;
global using ASC.Web.Files;
global using ASC.Web.Files.Configuration;
global using ASC.Web.Files.HttpHandlers;
global using ASC.Web.Files.Services.DocumentService;
global using ASC.Web.Files.Services.WCFService.FileOperations;
global using ASC.Web.Files.Utils;
global using ASC.Web.Studio.Core.Backup;
global using ASC.Web.Studio.Wallet;
global using ASC.Webhooks;
global using ASC.Webhooks.Core.IntegrationEvents.Events;
global using ASC.Webhooks.Extension;
global using ASC.Webhooks.Service.Services;

global using Autofac;
global using Autofac.Extensions.DependencyInjection;

global using Microsoft.AspNetCore.Http.Features;
global using Microsoft.AspNetCore.Server.Kestrel.Core;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Hosting.WindowsServices;

global using NLog;

global using Microsoft.AspNetCore.Authentication;
global using AiDbContext = ASC.AI.Core.Database.AiDbContext;
global using NotifySenderService = ASC.Core.Common.Notify.Engine.NotifySenderService;