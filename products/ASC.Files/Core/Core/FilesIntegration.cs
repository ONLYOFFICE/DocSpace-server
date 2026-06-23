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

namespace ASC.Web.Files.Api;

[Scope]
public class FilesIntegration(IDaoFactory daoFactory)
{
    private static readonly Dictionary<string, IFileSecurityProvider> _providers = new();

    public async Task<T> RegisterBunchAsync<T>(string module, string bunch, string data)
    {
        var folderDao = daoFactory.GetFolderDao<T>();

        return await folderDao.GetFolderIDAsync(module, bunch, data, true);
    }

    public IAsyncEnumerable<T> RegisterBunchFoldersAsync<T>(string module, string bunch, IEnumerable<string> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        data = data.ToList();
        if (!data.Any())
        {
            return AsyncEnumerable.Empty<T>();
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        return folderDao.GetFolderIDsAsync(module, bunch, data, true);
    }

    public bool IsRegisteredFileSecurityProvider(string module, string bunch)
    {
        lock (_providers)
        {
            return _providers.ContainsKey(module + bunch);
        }

    }

    public void RegisterFileSecurityProvider(string module, string bunch, IFileSecurityProvider securityProvider)
    {
        lock (_providers)
        {
            _providers[module + bunch] = securityProvider;
        }
    }

    internal static IFileSecurity GetFileSecurity(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var parts = path.Split('/');
        if (parts.Length < 3)
        {
            return null;
        }

        IFileSecurityProvider provider;
        lock (_providers)
        {
            _providers.TryGetValue(parts[0] + parts[1], out provider);
        }

        return provider?.GetFileSecurity(parts[2]);
    }

    internal static Dictionary<object, IFileSecurity> GetFileSecurity(Dictionary<string, string> paths)
    {
        var result = new Dictionary<object, IFileSecurity>();
        var gropped = paths.GroupBy(r =>
        {
            var parts = r.Value.Split('/');
            if (parts.Length < 3)
            {
                return string.Empty;
            }

            return parts[0] + parts[1];
        }, v =>
        {
            var parts = v.Value.Split('/');
            if (parts.Length < 3)
            {
                return new KeyValuePair<string, string>(v.Key, "");
            }

            return new KeyValuePair<string, string>(v.Key, parts[2]);
        });

        foreach (var grouping in gropped)
        {
            IFileSecurityProvider provider;
            lock (_providers)
            {
                _providers.TryGetValue(grouping.Key, out provider);
            }

            if (provider == null)
            {
                continue;
            }

            var data = provider.GetFileSecurity(grouping.ToDictionary(r => r.Key, r => r.Value));

            foreach (var d in data)
            {
                result.Add(d.Key, d.Value);
            }
        }

        return result;
    }
}