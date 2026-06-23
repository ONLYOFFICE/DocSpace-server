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

namespace ASC.Api.Settings;

/// <summary>
/// The build version parameters.
/// </summary>
[Scope]
public class BuildVersion
{
    /// <summary>
    /// The ONLYOFFICE DocSpace version.
    /// </summary>
    public string DocSpace { get; set; }

    /// <summary>
    /// The ONLYOFFICE Docs version.
    /// </summary>
    public string DocumentServer { get; set; }

    [JsonIgnore]
    private readonly IConfiguration _configuration;

    [JsonIgnore]
    private readonly FilesLinkUtility _filesLinkUtility;

    [JsonIgnore]
    private readonly DocumentServiceConnector _documentServiceConnector;

    [JsonIgnore]
    private readonly ICache _cache;

    public BuildVersion(IConfiguration configuration, FilesLinkUtility filesLinkUtility, DocumentServiceConnector documentServiceConnector, ICache cache)
    {
        _configuration = configuration;
        _filesLinkUtility = filesLinkUtility;
        _documentServiceConnector = documentServiceConnector;
        _cache = cache;
    }

    public async Task<BuildVersion> GetCurrentBuildVersionAsync()
    {
        DocSpace = GetDocSpaceVersion();
        DocumentServer = await GetDocumentVersionAsync();

        return this;
    }

    private string GetDocSpaceVersion()
    {
        return _configuration["version:number"] ?? "3.1.0";
    }

    private async Task<string> GetDocumentVersionAsync()
    {
        if (string.IsNullOrEmpty(_filesLinkUtility.DocServiceApiUrl))
        {
            return null;
        }

        var cacheKey = "DocumentServiceVersion";

        var version = _cache.Get<string>(cacheKey);

        if (string.IsNullOrEmpty(version))
        {
            version = await _documentServiceConnector.GetVersionAsync();

            _cache.Insert(cacheKey, version, DateTime.UtcNow.Add(TimeSpan.FromMinutes(15)));
        }

        return version;
    }
}