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
global using System.ComponentModel;
global using System.Configuration;
global using System.Diagnostics;
global using System.Globalization;
global using System.Net;
global using System.Net.Mail;
global using System.Net.Mime;
global using System.Reflection;
global using System.Security.Authentication;
global using System.Security.Cryptography;
global using System.Security.Principal;
global using System.ServiceModel;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Threading.Channels;
global using System.Web;

global using ASC.Api.Core.Extensions;
global using ASC.Common;
global using ASC.Common.Caching;
global using ASC.Common.Log;
global using ASC.Common.Security;
global using ASC.Common.Security.Authorizing;
global using ASC.Common.Threading;
global using ASC.Common.Threading.DistributedLock.Abstractions;
global using ASC.Common.Threading.DistributedLock.Common;
global using ASC.Common.Threading.DistributedLock.RedisLock.Configuration;
global using ASC.Common.Utils;
global using ASC.Security.Cryptography;

global using Autofac;
global using Autofac.Configuration;

global using Confluent.Kafka;

global using JWT;
global using JWT.Algorithms;

global using Medallion.Threading;

global using Microsoft.AspNetCore.Cryptography.KeyDerivation;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.Extensions;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.Extensions.Caching.Memory;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Primitives;
global using Microsoft.Net.Http.Headers;
global using Microsoft.OpenApi;

global using NetEscapades.EnumGenerators;

global using NVelocity;
global using NVelocity.App;
global using NVelocity.Runtime.Resource.Loader;

global using ProtoBuf;

global using RabbitMQ.Client;
global using RabbitMQ.Client.Events;

global using StackExchange.Redis;
global using StackExchange.Redis.Extensions.Core;
global using StackExchange.Redis.Extensions.Core.Abstractions;

global using Swashbuckle.AspNetCore.SwaggerGen;

global using ZiggyCreatures.Caching.Fusion;

global using ILogger = Microsoft.Extensions.Logging.ILogger;