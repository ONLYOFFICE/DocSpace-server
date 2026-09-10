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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The credentials and the title of the third-party storage account the portal writes its backups to.
/// </summary>
public class ThirdPartyBackupRequestDto
{
    /// <summary>
    /// The address of the storage server to connect to. It is needed by the WebDAV presets whose server is not known
    /// in advance (`WebDav`, `Nextcloud`, `ownCloud`), where it points at the WebDAV endpoint of that server, and by
    /// `SharePoint`; the presets with a fixed address and the OAuth services ignore it.
    /// </summary>
    /// <example>https://cloud.example.com/remote.php/dav/files/admin/</example>
    public string Url { get; set; }

    /// <summary>
    /// The account name at the storage service, used by the services that authenticate by login and password. A login
    /// sent without a password is rejected as an invalid request.
    /// </summary>
    /// <example>admin</example>
    public string Login { get; set; }

    /// <summary>
    /// The password, or the application password, for `login` at the storage service. Either this or `token` has to
    /// be sent, and the credentials are verified against the service before the account is saved.
    /// </summary>
    /// <example>p@ssw0rd!</example>
    public string Password { get; set; }

    /// <summary>
    /// The OAuth 2.0 authorization code from the consent screen of `Box`, `DropboxV2`, `GoogleDrive` or `OneDrive` -
    /// not an access token: the portal exchanges the code for its own token and keeps that. The client ID and
    /// redirect URL the consent screen URL is built from come from `GET api/2.0/files/thirdparty/capabilities`.
    /// </summary>
    /// <example>4/0AY0e-g5Tn8vQrM2kZs7xB1pLd9</example>
    public string Token { get; set; }

    /// <summary>
    /// The name the backup account is shown under in the portal. Characters that a folder title cannot hold are
    /// replaced and the value is truncated; on the first connection a title that comes out of that empty is refused.
    /// </summary>
    /// <example>Backup storage</example>
    public string CustomerTitle { get; set; }

    /// <summary>
    /// The storage service to connect, as the `key` of `GET api/2.0/files/thirdparty/providers`; the value is matched
    /// case-insensitively. `Nextcloud` and `ownCloud` are presets over WebDAV and are stored and reported back as
    /// `WebDav`.
    /// </summary>
    /// <example>Nextcloud</example>
    public string ProviderKey { get; set; }
}
