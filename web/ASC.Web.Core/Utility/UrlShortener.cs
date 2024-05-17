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

namespace ASC.Web.Core.Utility;

[Scope(typeof(BaseUrlShortener))]
public interface IUrlShortener
{
    Task<string> GetShortenLinkAsync(string shareLink);
}

[Scope]
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
        TenantManager tenantManager)
    : IUrlShortener
{
    public async Task<string> GetShortenLinkAsync(string shareLink)
    {
        if (Uri.IsWellFormedUriString(shareLink, UriKind.Absolute))
        {
            var tenantId = await tenantManager.GetCurrentTenantIdAsync();
            var context = await contextFactory.CreateDbContextAsync();
            var link = await context.ShortLinks.FirstOrDefaultAsync(q=> q.TenantId == tenantId && q.Link == shareLink);
            if (link != null)
            {
                return commonLinkUtility.GetFullAbsolutePath(UrlShortRewriter.BasePath + link.Short);
            }

            while (true)
            {
                var key = ShortUrl.GenerateRandomKey();
                var id = ShortUrl.Decode(key);
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

public static class ShortUrl
{
    private const string Alphabet = "5XzpDt6wZRdsTrJkSY_cgPyxN4j-fnb9WKBF8vh3GH72QqmLVCM";
    private static readonly int _base = Alphabet.Length;

    public static string GenerateRandomKey()
    {
        var rand = new Random();
        var length = 15;
        var result = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            var x = rand.Next(0, 51);
            result.Append(Alphabet.ElementAt(x));
        }
        return result.ToString();
    }

    public static ulong Decode(string str)
    {
        ulong num = 0;
        for (var i = 0; i < str.Length; i++)
        {
            num = num * (ulong)_base + (ulong)Alphabet.IndexOf(str.ElementAt(i));
        }
        return num;
    }
}