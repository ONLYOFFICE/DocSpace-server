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

namespace ASC.Web.Core.Utility;

public interface IUrlShortener
{
    Task<string> GetShortenLinkAsync(string shareLink);
}

[Scope(typeof(IUrlShortener))]
public class BaseUrlShortener(ConsumerFactory consumerFactory, IServiceProvider serviceProvider) : IUrlShortener
{
    public Task<string> GetShortenLinkAsync(string shareLink)
    {
        IUrlShortener shortener;
        if (consumerFactory.Get<BitlyLoginProvider>().Enabled)
        {
            shortener = serviceProvider.GetRequiredService<BitLyShortener>();
        }
        else
        {
            shortener = serviceProvider.GetRequiredService<OnlyoShortener>();
        }

        return shortener.GetShortenLinkAsync(shareLink);
    }
}

[Scope]
public class BitLyShortener(ConsumerFactory consumerFactory) : IUrlShortener
{
    private ConsumerFactory ConsumerFactory { get; } = consumerFactory;

    public Task<string> GetShortenLinkAsync(string shareLink)
    {
        return Task.FromResult(ConsumerFactory.Get<BitlyLoginProvider>().GetShortenLink(shareLink));
    }
}

[Scope]
public class OnlyoShortener(IDbContextFactory<UrlShortenerDbContext> contextFactory,
        CommonLinkUtility commonLinkUtility,
        TenantManager tenantManager,
        ShortUrl shortUrl)
    : IUrlShortener
{
    public async Task<string> GetShortenLinkAsync(string shareLink)
    {
        if (!shareLink.StartsWith(commonLinkUtility.ServerRootPath) || new Uri(shareLink).Host != new Uri(commonLinkUtility.ServerRootPath).Host)
        {
            throw new ArgumentException("the link should be to this portal");
        }
        if (Uri.IsWellFormedUriString(shareLink, UriKind.Absolute))
        {
            var tenantId = tenantManager.GetCurrentTenantId();
            await using var context = await contextFactory.CreateDbContextAsync();
            var link = await context.ShortLinks.FirstOrDefaultAsync(q => q.TenantId == tenantId && q.Link == shareLink);
            if (link != null)
            {
                return commonLinkUtility.GetFullAbsolutePath(UrlShortRewriter.BasePath + link.Short);
            }

            while (true)
            {
                var key = shortUrl.GenerateRandomKey();
                var id = shortUrl.Decode(key);
                var existId = await context.ShortLinks.AnyAsync(q => q.Id == id);
                if (!existId)
                {
                    var newShortLink = new ShortLink
                    {
                        Id = id,
                        Link = shareLink,
                        Short = key,
                        TenantId = tenantId
                    };
                    await context.ShortLinks.AddAsync(newShortLink);
                    await context.SaveChangesAsync();
                    return commonLinkUtility.GetFullAbsolutePath(UrlShortRewriter.BasePath + key);
                }
            }
        }

        return shareLink;
    }
}

[Singleton]
public class ShortUrl
{
    private readonly string _alphabet;
    private readonly int _base;
    private readonly int _length;

    public ShortUrl(IConfiguration configuration)
    {
        _alphabet = configuration["urlShortener:alphabet"] ?? "5XzpDt6wZRdsTrJkSY_cgPyxN4j-fnb9WKBF8vh3GH72QqmLVCM";
        _base = _alphabet.Length;
        if (!int.TryParse(configuration["urlShortener:length"], out _length))
        {
            _length = 15;
        }
    }
    public string GenerateRandomKey(int customLength = 0, string alphabet = null)
    {
        var rand = new Random();
        var result = new StringBuilder();
        var length = customLength == 0 ? _length : customLength;
        alphabet ??= _alphabet;
        var @base = alphabet.Length;

        for (var i = 0; i < length; i++)
        {
            var x = rand.Next(0, @base);
            result.Append(alphabet.ElementAt(x));
        }
        return result.ToString();
    }

    public ulong Decode(string str)
    {
        ulong num = 0;
        for (var i = 0; i < str.Length; i++)
        {
            num = num * (ulong)_base + (ulong)_alphabet.IndexOf(str.ElementAt(i));
        }
        return num;
    }
}