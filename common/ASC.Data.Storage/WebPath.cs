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
public class WebPathSettings
{
    private readonly IEnumerable<Appender> _appenders;

    public WebPathSettings(StorageConfigExtension storageConfigExtension)
    {
        var storage = storageConfigExtension.Storage;
        if (storage != null)
        {
            _appenders = storage.Appender;
        }
    }

    public string GetRelativePath(HttpContext httpContext, ILoggerFactory loggerFactory, string absolutePath)
    {
        if (!Uri.IsWellFormedUriString(absolutePath, UriKind.Absolute))
        {
            throw new ArgumentException($"bad path format {absolutePath} is not absolute");
        }

        var appender = _appenders.FirstOrDefault(x => absolutePath.Contains(x.Append) || (absolutePath.Contains(x.AppendSecure) && !string.IsNullOrEmpty(x.AppendSecure)));
        if (appender == null)
        {
            return absolutePath;
        }

        return SecureHelper.IsSecure(httpContext, loggerFactory) && !string.IsNullOrEmpty(appender.AppendSecure) ?
            absolutePath.Remove(0, appender.AppendSecure.Length) :
            absolutePath.Remove(0, appender.Append.Length);
    }

    public string GetPath(HttpContext httpContext, ILoggerFactory loggerFactory, string relativePath)
    {
        if (!string.IsNullOrEmpty(relativePath) && relativePath.StartsWith('~'))
        {
            throw new ArgumentException($"bad path format {relativePath} remove '~'", nameof(relativePath));
        }

        var result = relativePath;
        var ext = Path.GetExtension(relativePath).ToLowerInvariant();

        if (_appenders.Any())
        {
            var avaliableAppenders = _appenders.Where(x => x.Extensions != null && x.Extensions.Split('|').Contains(ext) || string.IsNullOrEmpty(ext)).ToList();
            var avaliableAppendersCount = avaliableAppenders.Count;

            Appender appender;
            if (avaliableAppendersCount > 1)
            {
                appender = avaliableAppenders[relativePath.Length % avaliableAppendersCount];
            }
            else if (avaliableAppendersCount == 1)
            {
                appender = avaliableAppenders.First();
            }
            else
            {
                appender = _appenders.First();
            }

            if (appender.Append.StartsWith('~'))
            {
                var query = string.Empty;
                //Rel path
                if (relativePath.IndexOfAny(['?', '=', '&']) != -1)
                {
                    //Cut it
                    query = relativePath[relativePath.IndexOf('?')..];
                    relativePath = relativePath[..relativePath.IndexOf('?')];
                }
                //if (HostingEnvironment.IsHosted)
                //{
                //    result = VirtualPathUtility.ToAbsolute(string.Format("{0}/{1}{2}", appender.Append.TrimEnd('/'), relativePath.TrimStart('/'), query));
                //}
                //else
                //{
                result = $"{appender.Append.TrimEnd('/').TrimStart('~')}/{relativePath.TrimStart('/')}{query}";
                //}
            }
            else
            {
                //TODO HostingEnvironment.IsHosted
                if (SecureHelper.IsSecure(httpContext, loggerFactory) && !string.IsNullOrEmpty(appender.AppendSecure))
                {
                    result = $"{appender.AppendSecure.TrimEnd('/')}/{relativePath.TrimStart('/')}";
                }
                else
                {
                    //Append directly
                    result = $"{appender.Append.TrimEnd('/')}/{relativePath.TrimStart('/')}";
                }
            }
        }

        return result;
    }
}

[Scope]
public class WebPath(
    WebPathSettings webPathSettings,
    IServiceProvider serviceProvider,
    SettingsManager settingsManager,
    StorageSettingsHelper storageSettingsHelper,
    IHttpContextAccessor httpContextAccessor,
    CoreBaseSettings coreBaseSettings,
    ILoggerFactory loggerFactory)
{
    public async Task<string> GetPathAsync(string relativePath)
    {
        if (!string.IsNullOrEmpty(relativePath) && relativePath.StartsWith('~'))
        {
            throw new ArgumentException($"bad path format {relativePath} remove '~'", nameof(relativePath));
        }

        if (coreBaseSettings.Standalone && await serviceProvider.GetService<StaticUploader>().CanUploadAsync()) //hack for skip resolve DistributedTaskQueueOptionsManager
        {
            try
            {
                var uri = await (await storageSettingsHelper.DataStoreAsync(await settingsManager.LoadAsync<CdnStorageSettings>())).GetInternalUriAsync("", relativePath, TimeSpan.Zero, null);
                var result = uri.AbsoluteUri.ToLower();
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
            catch (Exception)
            {

            }
        }

        return webPathSettings.GetPath(httpContextAccessor?.HttpContext, loggerFactory, relativePath);
    }
}