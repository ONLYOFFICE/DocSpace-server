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

global using System.Collections;
global using System.Collections.Concurrent;
global using System.Collections.Frozen;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Configuration;
global using System.Data.Common;
global using System.Diagnostics;
global using System.Globalization;
global using System.Linq.Expressions;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Net.Sockets;
global using System.Net.Http.Json;
global using System.Reflection;
global using System.Runtime.Serialization;
global using System.Security;
global using System.Security.Authentication;
global using System.Security.Claims;
global using System.Security.Cryptography;
global using System.Security.Principal;
global using System.ServiceModel;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Web;

global using Amazon;
global using Amazon.Runtime;
global using Amazon.SimpleEmail;
global using Amazon.SimpleEmail.Model;

global using ASC.Api.Core.Extensions;
global using ASC.AuditTrail.Models;
global using ASC.Collections;
global using ASC.Common;
global using ASC.Common.Caching;
global using ASC.Common.Log;
global using ASC.Common.Logging;
global using ASC.Common.Module;
global using ASC.Common.Notify.Engine;
global using ASC.Common.Notify.Patterns;
global using ASC.Common.Radicale;
global using ASC.Common.Radicale.Core;
global using ASC.Common.Security;
global using ASC.Common.Security.Authentication;
global using ASC.Common.Security.Authorizing;
global using ASC.Common.Threading.DistributedLock.Abstractions;
global using ASC.Common.Utils;
global using ASC.Common.Web;
global using ASC.Core;
global using ASC.Core.Billing;
global using ASC.Core.Caching;
global using ASC.Core.Common;
global using ASC.Core.Common.Configuration;
global using ASC.Core.Common.Core;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.EF.Context;
global using ASC.Core.Common.EF.Model;
global using ASC.Core.Common.Hosting.Extensions;
global using ASC.Core.Common.Hosting.Interfaces;
global using ASC.Core.Common.Log;
global using ASC.Core.Common.Messaging;
global using ASC.Core.Common.Notify;
global using ASC.Core.Common.Notify.IntegrationEvents.Events;
global using ASC.Core.Common.Notify.Jabber;
global using ASC.Core.Common.Notify.Push;
global using ASC.Core.Common.Notify.Stylers.Resources;
global using ASC.Core.Common.Notify.Telegram;
global using ASC.Core.Common.Quota;
global using ASC.Core.Common.Quota.Features;
global using ASC.Core.Common.Security;
global using ASC.Core.Common.Settings;
global using ASC.Core.Common.Users;
global using ASC.Core.Common.WhiteLabel;
global using ASC.Core.Configuration;
global using ASC.Core.Data;
global using ASC.Core.Notify;
global using ASC.Core.Notify.Jabber;
global using ASC.Core.Notify.Senders;
global using ASC.Core.Notify.Socket;
global using ASC.Core.Security.Authentication;
global using ASC.Core.Tenants;
global using ASC.Core.Users;
global using ASC.EventBus.Abstractions;
global using ASC.EventBus.Events;
global using ASC.Geolocation;
global using ASC.MessagingSystem.Core;
global using ASC.MessagingSystem.EF.Context;
global using ASC.MessagingSystem.EF.Model;
global using ASC.MessagingSystem.Mapping;
global using ASC.Notify;
global using ASC.Notify.Channels;
global using ASC.Notify.Cron;
global using ASC.Notify.Engine;
global using ASC.Notify.Messages;
global using ASC.Notify.Model;
global using ASC.Notify.Patterns;
global using ASC.Notify.Recipients;
global using ASC.Notify.Sinks;
global using ASC.Security.Cryptography;
global using ASC.Web.Core.WhiteLabel;
global using ASC.Web.Core.Files;
global using ASC.Web.Studio.Utility;

global using Autofac;

global using Google.Apis.Auth.OAuth2;

global using MailKit.Security;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Infrastructure;
global using Microsoft.EntityFrameworkCore.Query;
global using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Net.Http.Headers;

global using MimeKit;

global using NetEscapades.EnumGenerators;

global using NVelocity;
global using NVelocity.App.Events;

global using Polly;
global using Polly.Registry;
global using Polly.Retry;

global using ProtoBuf;

global using Refit;

global using Riok.Mapperly.Abstractions;

global using Telegram.Bot;

global using Textile;
global using Textile.Blocks;
global using Textile.States;

global using ZiggyCreatures.Caching.Fusion;

global using AppOptions = FirebaseAdmin.AppOptions;
global using FirebaseAdminMessaging = FirebaseAdmin.Messaging;
global using FirebaseApp = FirebaseAdmin.FirebaseApp;
global using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;
global using JsonSerializer = System.Text.Json.JsonSerializer;