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
global using System.Globalization;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Security.Cryptography;
global using System.ServiceModel;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Web;

global using Amazon;
global using Amazon.CloudFront;
global using Amazon.CloudFront.Model;
global using Amazon.Extensions.S3.Encryption;
global using Amazon.Extensions.S3.Encryption.Primitives;
global using Amazon.S3;
global using Amazon.S3.Internal;
global using Amazon.S3.Model;
global using Amazon.S3.Transfer;
global using Amazon.Util;

global using ASC.Common;
global using ASC.Common.Caching;
global using ASC.Common.Log;
global using ASC.Common.Threading;
global using ASC.Common.Threading.DistributedLock.Abstractions;
global using ASC.Common.Utils;
global using ASC.Core;
global using ASC.Core.ChunkedUploader;
global using ASC.Core.Common.Configuration;
global using ASC.Core.Common.Quota;
global using ASC.Core.Common.Quota.Custom;
global using ASC.Core.Common.Quota.Features;
global using ASC.Core.Common.Settings;
global using ASC.Core.Encryption;
global using ASC.Core.Notify;
global using ASC.Core.Tenants;
global using ASC.Core.Users;
global using ASC.Data.Storage;
global using ASC.Data.Storage.ChunkedUploader;
global using ASC.Data.Storage.Configuration;
global using ASC.Data.Storage.DataOperators;
global using ASC.Data.Storage.DiscStorage;
global using ASC.Data.Storage.Encryption;
global using ASC.Data.Storage.Log;
global using ASC.Data.Storage.RackspaceCloud;
global using ASC.Data.Storage.S3;
global using ASC.Data.Storage.Tar;
global using ASC.EventBus.Events;
global using ASC.Notify.Messages;
global using ASC.Protos.Migration;
global using ASC.Security.Cryptography;

global using Google.Apis.Auth.OAuth2;
global using Google.Cloud.Storage.V1;

global using ICSharpCode.SharpZipLib.GZip;
global using ICSharpCode.SharpZipLib.Tar;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.WebUtilities;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;

global using net.openstack.Core.Domain;
global using net.openstack.Providers.Rackspace;

global using ProtoBuf;

global using ZiggyCreatures.Caching.Fusion;

global using static Google.Cloud.Storage.V1.UrlSigner;

global using MimeMapping = ASC.Common.Web.MimeMapping;