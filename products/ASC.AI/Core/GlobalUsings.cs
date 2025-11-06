// (c) Copyright Ascensio System SIA 2009-2025
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

global using ASC.AI.Core.Database;
global using ASC.AI.Core.Database.Models;
global using ASC.AI.Core.Chat.Data;
global using ASC.AI.Core.Chat.History;
global using ASC.AI.Core.MCP;
global using ASC.AI.Core.MCP.Auth;
global using ASC.AI.Core.MCP.Data;
global using ASC.AI.Core.MCP.Transport;
global using ASC.AI.Core.Tools;
global using ASC.AI.Core.Provider;
global using ASC.AI.Core.Provider.Data;
global using ASC.AI.Core.Provider.Model;
global using ASC.AI.Core.Chat.Tool;
global using ASC.AI.Core.Retrieval.Knowledge;
global using ASC.AI.Core.Retrieval.Web;
global using ASC.AI.Core.Retrieval.Web.Engine;
global using ASC.AI.Core.Utils;

global using ASC.Common;
global using ASC.Common.Log;
global using ASC.Common.Threading.DistributedLock.Abstractions;
global using ASC.Common.Web;

global using ASC.Core;
global using ASC.Core.Billing;
global using ASC.Core.Common.AI;
global using ASC.Core.Common.Configuration;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.EF.Model;
global using ASC.Core.Common.EF.Model.Ai;
global using ASC.Core.Common.Settings;
global using ASC.Core.Notify.Socket;
global using ASC.Core.Tenants;
global using ASC.Core.Users;
global using ASC.Files.Core;
global using ASC.Files.Core.EF;
global using ASC.Files.Core.Helpers;
global using ASC.Files.Core.Resources;
global using ASC.Files.Core.Security;
global using ASC.Files.Core.Text;
global using ASC.Files.Core.Utils;
global using ASC.Files.Core.Vectorization.Data;
global using ASC.Files.Core.Vectorization.Embedding;
global using ASC.Files.Core.Vectorization.Settings;

global using ASC.FederatedLogin;
global using ASC.FederatedLogin.Helpers;
global using ASC.FederatedLogin.LoginProviders;

global using ASC.Security.Cryptography;

global using ASC.Web.Core;
global using ASC.Web.Files.Services.WCFService.FileOperations;
global using ASC.Web.Files.Utils;
global using ASC.Web.Studio.Utility;

global using ASC.ElasticSearch.VectorData;

global using Anthropic.SDK;

global using ModelContextProtocol.Client;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.AI;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;

global using OpenAI;

global using System.ClientModel;
global using System.ClientModel.Primitives;
global using System.ComponentModel;
global using System.Collections.Frozen;
global using System.ComponentModel.DataAnnotations;
global using System.Net.Http.Headers;
global using System.Net.Http.Json;
global using System.Runtime.CompilerServices;
global using System.Security;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading.Channels;

global using Riok.Mapperly.Abstractions;

global using ZiggyCreatures.Caching.Fusion;