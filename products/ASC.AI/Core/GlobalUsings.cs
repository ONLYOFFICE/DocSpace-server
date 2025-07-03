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

global using ASC.AI.Core.Chat.Database;
global using ASC.AI.Core.Chat.Extensions;
global using ASC.AI.Core.Chat.Models;
global using ASC.AI.Core.Common;
global using ASC.AI.Core.Common.Clients;
global using ASC.AI.Core.Common.Clients.Providers;
global using ASC.AI.Core.Common.Models;
global using ASC.AI.Core.Common.Database;
global using ASC.AI.Core.Common.Database.Models;
global using ASC.AI.Core.Common.Services;
global using ASC.AI.Core.Settings;

global using ASC.Common;
global using ASC.Common.Log;
global using ASC.Common.Mapping;
global using ASC.Common.Web;

global using ASC.Core;
global using ASC.Core.Common.EF;
global using ASC.Core.Common.EF.Model;
global using ASC.Core.Common.EF.Model.Chat;
global using ASC.Core.Users;

global using ASC.Files.Core;
global using ASC.Files.Core.Resources;
global using ASC.Files.Core.Security;

global using ASC.Security.Cryptography;

global using AutoMapper;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.AI;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;

global using OpenAI;

global using System.ClientModel;
global using System.ClientModel.Primitives;
global using System.Collections.Frozen;
global using System.ComponentModel.DataAnnotations;
global using System.Net.Http.Headers;
global using System.Net.Http.Json;
global using System.Runtime.CompilerServices;
global using System.Security;
global using System.Text.Encodings.Web;
global using System.Text.Json;
global using System.Text.Json.Serialization;