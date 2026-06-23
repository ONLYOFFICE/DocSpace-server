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

namespace ASC.Data.Storage;

[Singleton]
public class PathUtils(IConfiguration configuration, IHostEnvironment hostEnvironment)
{
    private IHostEnvironment HostEnvironment { get; } = hostEnvironment;

    private readonly string _storageRoot = configuration[Constants.StorageRootParam];
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PathUtils(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IWebHostEnvironment webHostEnvironment) : this(configuration, hostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public static string Normalize(string path, bool addTailingSeparator = false)
    {
        path = path
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace("\\\\", Path.DirectorySeparatorChar.ToString())
            .Replace("//", Path.DirectorySeparatorChar.ToString())
            .TrimEnd(Path.DirectorySeparatorChar);

        return addTailingSeparator && 0 < path.Length ? path + Path.DirectorySeparatorChar : path;
    }

    public string ResolveVirtualPath(string module, string domain)
    {
        var url = $"~/storage/{module}/{(string.IsNullOrEmpty(domain) ? "root" : domain)}/";

        return ResolveVirtualPath(url);
    }

    public string ResolveVirtualPath(string virtPath, bool addTrailingSlash = true)
    {
        virtPath ??= "";

        if (virtPath.StartsWith('~') && !Uri.IsWellFormedUriString(virtPath, UriKind.Absolute))
        {
            var rootPath = "/";
            if (!string.IsNullOrEmpty(_webHostEnvironment?.WebRootPath) && _webHostEnvironment?.WebRootPath.Length > 1)
            {
                rootPath = _webHostEnvironment?.WebRootPath.Trim('/');
            }

            virtPath = virtPath.Replace("~", rootPath);
        }
        if (addTrailingSlash)
        {
            virtPath += "/";
        }
        else
        {
            virtPath = virtPath.TrimEnd('/');
        }

        return virtPath.Replace("//", "/");
    }

    public string ResolvePhysicalPath(string physPath, IDictionary<string, string> storageConfig)
    {
        physPath = Normalize(physPath).TrimStart('~');

        if (physPath.Contains(Constants.StorageRootParam))
        {
            physPath = physPath.Replace(Constants.StorageRootParam, _storageRoot ?? storageConfig[Constants.StorageRootParam]);
        }

        if (!Path.IsPathRooted(physPath))
        {
            physPath = Path.GetFullPath(CrossPlatform.PathCombine(HostEnvironment.ContentRootPath, physPath.Trim(Path.DirectorySeparatorChar)));
        }

        return physPath;
    }
}