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

namespace ASC.AppHost.Configuration;

public static class Constants
{
    public const int AppHostPort = 8092;
    public const int AppHostHttpsPort = 443;
    public const string AppHostHttpsHost = "docspace.dev.localhost";

    public const int RestyPort = 8092;
    public const int RestyHttpsPort = 443;
    public const int SocketIoPort = 9899;
    public const int SsoAuthPort = 9834;
    public const int NewAiPort = 9837;
    public const int WebDavPort = 1900;
    public const int IdentityRegistrationPort = 9090;
    public const int IdentityAuthorizationPort = 8080;
    public const int PeoplePort = 5004;
    public const int FilesPort = 5007;
    public const int WebApiPort = 5000;
    public const int ApiSystemPort = 5010;
    public const int BackupPort = 5012;
    public const int WebstudioPort = 5003;
    public const int AiPort = 5157;
    public const int AiWorkerPort = 5154;
    public const int ClearEventsPort = 5027;
    public const int BackupWorkerPort = 5032;
    public const int FilesWorkerPort = 5009;
    public const int NotifyPort = 5005;
    public const int StudioNotifyPort = 5006;
    public const int OpensearchPort = 9200;
    public const int DocSpaceMcpPort = 8000;
    public const int PlaywrightUiPort = 3333;
    public const int E2ETestsUiPort = 3334;

    public const int TelegramPort = 5050;
    public const int MonolithPort = 5027;

    public const int OtelCollectorGrpcPort = 4317;
    public const int OtelCollectorHttpPort = 4318;

    public const string HostDockerInternal = "host.docker.internal";
    public const string OpenRestyContainer = "onlyoffice-openresty";
    public const string EditorsContainer = "onlyoffice-editors";
    public const string OpensearchContainer = "opensearch";
    public const string OpensearchVersion = "3.5.0";
    public const string SocketIoContainer = "onlyoffice-socketIO";
    public const string DocSpaceMcpContainer = "onlyoffice-docspace-mcp";
    public const string OtelCollectorContainer = "onlyoffice-otel-collector";

    public const string IdentityRegistrationContainer = "onlyoffice-identity-registration";
    public const string IdentityAuthorizationContainer = "onlyoffice-identity-authorization";
}
