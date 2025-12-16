// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.AppHost.Configuration;

public static class Constants
{
    public const int RestyPort = 8092;
    public const int SocketIoPort = 9899;
    public const int SsoAuthPort = 9834;
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
    public const int AiServicePort = 5154;
    public const int ClearEventsPort = 5027;
    public const int BackupBackgroundTasksPort = 5032;
    public const int FilesServicePort = 5009;
    public const int StudioNotifyPort = 5006;
    
    public const string HostDockerInternal = "host.docker.internal";
    public const string OpenRestyContainer = "asc-openresty";
    public const string EditorsContainer = "asc-editors";
    public const string SocketIoContainer = "asc-socketIO";
    public const string IdentityRegistrationContainer = "asc-identity-registration";
    public const string IdentityAuthorizationContainer = "asc-identity-authorization";
}
