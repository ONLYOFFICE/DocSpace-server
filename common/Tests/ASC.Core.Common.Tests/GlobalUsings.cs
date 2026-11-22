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

global using System.Collections.Concurrent;
global using System.ComponentModel.DataAnnotations;
global using System.Diagnostics;
global using System.Globalization;
global using System.Net;
global using System.Net.NetworkInformation;
global using System.Net.Security;
global using System.Net.Sockets;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Security.Cryptography;
global using System.Security.Cryptography.X509Certificates;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Xml.Linq;

global using ASC.AI.Worker.Extensions;
global using ASC.Api.Core.Extensions;
global using ASC.Api.Core.Middleware;
global using ASC.Api.Core.Webhook.Payloads;
global using ASC.Common.Caching;
global using ASC.Common.Notify.Patterns;
global using ASC.Common.Threading.DistributedLock.Abstractions;
global using ASC.Common.Threading.DistributedLock.Common;
global using ASC.Common.Threading.DistributedLock.InMemoryLock;
global using ASC.Common.Utils;
global using ASC.Common.Web;
global using ASC.Core.Billing;
global using ASC.Core.Common.Quota;
global using ASC.Core.Common.Security;
global using ASC.Core.Data;
global using ASC.Core.Tenants;
global using ASC.Core.Users;
global using ASC.FederatedLogin.Helpers;
global using ASC.Files.Core;
global using ASC.Files.Core.ApiModels.WebhookDto;
global using ASC.Files.Core.Helpers;
global using ASC.Files.Core.Core.Thirdparty;
global using ASC.Notify;
global using ASC.Notify.Channels;
global using ASC.Notify.Cron;
global using ASC.Notify.Engine;
global using ASC.Notify.Messages;
global using ASC.Notify.Model;
global using ASC.Notify.Patterns;
global using ASC.Notify.Recipients;
global using ASC.Notify.Sinks;
global using ASC.Notify.Textile;
global using ASC.People.Extensions;
global using ASC.Webhooks.Contract;
global using ASC.Webhooks.Core;

global using FluentAssertions;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Caching.Distributed;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Options;

global using Polly;
global using Polly.Registry;
global using Polly.Retry;

global using WireMock;
global using WireMock.Matchers;
global using WireMock.RequestBuilders;
global using WireMock.ResponseBuilders;
global using WireMock.Server;
global using WireMock.Settings;
global using WireMock.Types;
global using WireMock.Util;

global using ZiggyCreatures.Caching.Fusion;
global using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

global using static ASC.Core.Common.Tests.PaymentStubs;

global using AiAgentMigrationTitle = ASC.Files.Core.Core.AI.AiAgentMigrationTitle;
global using AiAgentsMigrationPlan = ASC.Core.Common.AI.AiAgentsMigrationPlan;
global using AiAgentsMigrationService = ASC.AI.Worker.BackgroundServices.AiAgentsMigrationService;
global using BoxAPIException = Box.V2.Exceptions.BoxAPIException;
global using BoxError = Box.V2.BoxError;
global using CreateFolderError = Dropbox.Api.Files.CreateFolderError;
global using DbQuota = ASC.Core.Common.EF.DbQuota;
global using DocsTrialCache = ASC.Files.Core.Helpers.DocsTrialCache;
global using DocsTrialCacheOptions = ASC.Files.Core.Helpers.DocsTrialCacheOptions;
global using DocsTrialCheck = ASC.Files.Core.Helpers.DocsTrialCheck;
global using DocsTrialDecision = ASC.Files.Core.Helpers.DocsTrialDecision;
global using DocsTrialState = ASC.Files.Core.Helpers.DocsTrialState;
global using DocumentService = ASC.Files.Core.Helpers.DocumentService;
global using DocumentServiceLicense = ASC.Files.Core.Helpers.DocumentServiceLicense;
global using GoogleApiException = Google.GoogleApiException;
global using GoogleRequestError = Google.Apis.Requests.RequestError;
global using GoogleSingleError = Google.Apis.Requests.SingleError;
global using GraphError = Microsoft.Graph.Error;
global using GraphServiceException = Microsoft.Graph.ServiceException;
global using JsonNodeExtension = Microsoft.OpenApi.JsonNodeExtension;
global using LegacyAiProvidersMigrationEntry = ASC.AI.Migration.LegacyAiProvidersMigrationEntry;
global using LegacyAiProvidersMigrationGate = ASC.AI.Migration.LegacyAiProvidersMigrationGate;
global using MachinePseudoKeys = ASC.Security.Cryptography.MachinePseudoKeys;
global using MySqlServerVersionCache = ASC.Core.Common.EF.MySqlServerVersionCache;
global using OpenApiSchema = Microsoft.OpenApi.OpenApiSchema;
global using QuotaSocketManager = ASC.Core.Common.Quota.QuotaSocketManager;
global using RelocationError = Dropbox.Api.Files.RelocationError;
global using RoomLogoManager = ASC.Files.Core.VirtualRooms.RoomLogoManager;
global using SchemaFilterContext = Swashbuckle.AspNetCore.SwaggerGen.SchemaFilterContext;
global using SchemaRepository = Swashbuckle.AspNetCore.SwaggerGen.SchemaRepository;
global using SocketData = ASC.Core.Notify.Socket.SocketData;
global using UploadError = Dropbox.Api.Files.UploadError;
global using UploadSessionFinishError = Dropbox.Api.Files.UploadSessionFinishError;
global using UploadWriteFailed = Dropbox.Api.Files.UploadWriteFailed;
global using Wildcard = ASC.Data.Storage.Wildcard;
global using WriteError = Dropbox.Api.Files.WriteError;
