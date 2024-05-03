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

namespace ASC.Data.Storage;

[Singleton]
public class WebPathSettings
{
    private readonly IEnumerable<Appender> _appenders;

    public WebPathSettings(Configuration.Storage storage)
    {
        if (storage != null)
        {
            _appenders = storage.Appender;
        }
    }

    public string GetRelativePath(HttpContext httpContext, ILoggerProvider options, string absolutePath)
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

        return SecureHelper.IsSecure(httpContext, options) && !string.IsNullOrEmpty(appender.AppendSecure) ?
            absolutePath.Remove(0, appender.AppendSecure.Length) :
            absolutePath.Remove(0, appender.Append.Length);
    }

    public string GetPath(HttpContext httpContext, ILoggerProvider options, string relativePath)
    {
        if (!string.IsNullOrEmpty(relativePath) && relativePath.IndexOf('~') == 0)
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

            if (appender.Append.IndexOf('~') == 0)
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
                if (SecureHelper.IsSecure(httpContext, options) && !string.IsNullOrEmpty(appender.AppendSecure))
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

[Scope(Additional = typeof(StaticUploaderExtension))]
public class WebPath(
    WebPathSettings webPathSettings,
    IServiceProvider serviceProvider,
    SettingsManager settingsManager,
    StorageSettingsHelper storageSettingsHelper,
    IHttpContextAccessor httpContextAccessor,
    CoreBaseSettings coreBaseSettings,
    ILoggerProvider options)
{
    public async Task<string> GetPathAsync(string relativePath)
    {
        if (!string.IsNullOrEmpty(relativePath) && relativePath.IndexOf('~') == 0)
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

        return webPathSettings.GetPath(httpContextAccessor?.HttpContext, options, relativePath);
    }
}

public class StaticUploaderExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<StaticUploader>();
    }
}
