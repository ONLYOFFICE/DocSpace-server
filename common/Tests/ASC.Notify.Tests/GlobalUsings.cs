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
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;

global using ASC.Api.Core.Extensions;
global using ASC.AuditTrail.Models;
global using ASC.Common.DependencyInjection;
global using ASC.Core;
global using ASC.Core.Billing;
global using ASC.Core.Common.WhiteLabel;
global using ASC.Core.Tenants;
global using ASC.Core.Users;
global using ASC.Data.Backup;
global using ASC.Notify.Engine;
global using ASC.Notify.Extension;
global using ASC.Notify.Messages;
global using ASC.Notify.Model;
global using ASC.Notify.Patterns;
global using ASC.Notify.Recipients;
global using ASC.Notify.Tests.Infrastructure;
global using ASC.Notify.Textile;
global using ASC.Security.Cryptography;
global using ASC.Web.Core.PublicResources;
global using ASC.Web.Core.Users;
global using ASC.Web.Studio.Core.Notify;
global using ASC.Web.Studio.Utility;

global using Aspire.Hosting;
global using Aspire.Hosting.ApplicationModel;
global using Aspire.Hosting.Testing;

global using Autofac;

global using FluentAssertions;

global using MailKit.Net.Smtp;
global using MailKit.Security;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;

global using MimeKit;
