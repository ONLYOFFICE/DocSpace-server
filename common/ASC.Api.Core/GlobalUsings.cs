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

global using System.ComponentModel.DataAnnotations;
global using System.Globalization;
global using System.IdentityModel.Tokens.Jwt;
global using System.Net;
global using System.Reflection;
global using System.Runtime.InteropServices;
global using System.Runtime.Serialization;
global using System.Security;
global using System.Security.Authentication;
global using System.Security.Claims;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Encodings.Web;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Threading.Channels;
global using System.Threading.RateLimiting;
global using System.Web;

global using Apache.NMS;

global using ASC.Api.Core;
global using ASC.Api.Core.Auth;
global using ASC.Api.Core.Convention;
global using ASC.Api.Core.Core;
global using ASC.Api.Core.Extensions;
global using ASC.Api.Core.Log;
global using ASC.Api.Core.Middleware;
global using ASC.Api.Core.Routing;
global using ASC.Api.Core.Security;
global using ASC.Api.Core.Socket;
global using ASC.AuditTrail.Repositories;
global using ASC.AuditTrail.Types;
global using ASC.Common;
global using ASC.Common.Caching;
global using ASC.Common.Caching.Settings;
global using ASC.Common.Data;
global using ASC.Common.DependencyInjection;
global using ASC.Common.Log;
global using ASC.Common.Logging;
global using ASC.Common.Security.Authentication;
global using ASC.Common.Security.Authorizing;
global using ASC.Common.Threading;
global using ASC.Common.Threading.DistributedLock.Abstractions;
global using ASC.Common.Threading.DistributedLock.RedisLock;
global using ASC.Common.Threading.DistributedLock.ZooKeeperLock;
global using ASC.Common.Threading.DistributedLock.ZooKeeperLock.Configuration;
global using ASC.Common.Utils;
global using ASC.Common.Web;
global using ASC.Core;
global using ASC.Core.Billing;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.EF.Context;
global using ASC.Core.Common.Hosting;
global using ASC.Core.Common.Hosting.Interfaces;
global using ASC.Core.Common.Notify.Engine;
global using ASC.Core.Common.Quota;
global using ASC.Core.Common.Quota.Features;
global using ASC.Core.Common.Security;
global using ASC.Core.Common.Settings;
global using ASC.Core.Notify.Socket;
global using ASC.Core.Tenants;
global using ASC.Core.Users;
global using ASC.EventBus;
global using ASC.EventBus.Abstractions;
global using ASC.EventBus.ActiveMQ;
global using ASC.EventBus.Extensions.Logger;
global using ASC.EventBus.InMemory;
global using ASC.EventBus.RabbitMQ;
global using ASC.EventBus.Serializers;
global using ASC.MessagingSystem.Core;
global using ASC.MessagingSystem.EF.Context;
global using ASC.MessagingSystem.EF.Model;
global using ASC.People.ApiModels.ResponseDto;
global using ASC.Security.Cryptography;
global using ASC.Web.Api.Models;
global using ASC.Web.Api.Routing;
global using ASC.Web.Core;
global using ASC.Web.Core.Helpers;
global using ASC.Web.Core.HttpHandlers;
global using ASC.Web.Core.PublicResources;
global using ASC.Web.Core.Quota;
global using ASC.Web.Core.Users;
global using ASC.Web.Studio.Core;
global using ASC.Web.Studio.UserControls.Management.SingleSignOnSettings;
global using ASC.Web.Studio.Utility;
global using ASC.Webhooks.Core;
global using ASC.Webhooks.Core.EF.Context;

global using Autofac;
global using Autofac.Extensions.DependencyInjection;

global using Confluent.Kafka;

global using HealthChecks.UI.Client;

global using Medallion.Threading.Redis;
global using Medallion.Threading.ZooKeeper;

global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.Cookies;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.HttpOverrides;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.ApplicationModels;
global using Microsoft.AspNetCore.Mvc.Authorization;
global using Microsoft.AspNetCore.Mvc.Controllers;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.AspNetCore.Mvc.Routing;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.Routing.Constraints;
global using Microsoft.AspNetCore.Routing.Patterns;
global using Microsoft.AspNetCore.Server.Kestrel.Core;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Primitives;
global using Microsoft.IdentityModel.Tokens;
global using Microsoft.Net.Http.Headers;

global using NLog;
global using NLog.AWS.Logger;
global using NLog.Config;
global using NLog.Web;

global using RedisRateLimiting;
global using RedisRateLimiting.AspNetCore;

global using StackExchange.Redis;
global using StackExchange.Redis.Extensions.Core.Abstractions;
global using StackExchange.Redis.Extensions.Core.Configuration;

global using LogLevel = Microsoft.Extensions.Logging.LogLevel;