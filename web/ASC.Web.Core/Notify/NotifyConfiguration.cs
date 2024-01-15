// (c) Copyright Ascensio System SIA 2010-2023
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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Studio.Core.Notify;

[Singleton(Additional = typeof(WorkContextExtension))]
public class NotifyConfiguration(NotifyEngine notifyEngine, WorkContext workContext)
{
    private static bool _configured;
    private static readonly object _locker = new();
    private static readonly Regex _urlReplacer = new(@"(<a [^>]*href=(('(?<url>[^>']*)')|(""(?<url>[^>""]*)""))[^>]*>)|(<img [^>]*src=(('(?<url>(?![data:|cid:])[^>']*)')|(""(?<url>(?![data:|cid:])[^>""]*)""))[^/>]*/?>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _textileLinkReplacer = new(@"""(?<text>[\w\W]+?)"":""(?<link>[^""]+)""", RegexOptions.Singleline | RegexOptions.Compiled);

    public void Configure()
    {
        lock (_locker)
        {
            if (!_configured)
            {
                _configured = true;
                workContext.NotifyStartUp();
                workContext.NotifyClientRegistration += NotifyClientRegisterCallback;
                notifyEngine.AddAction<NotifyTransferRequest>();
            }
        }
    }

    private void NotifyClientRegisterCallback(Context context, INotifyClient client)
    {
        #region url correction

        var absoluteUrl = new SendInterceptorSkeleton(
            "Web.UrlAbsoluter",
            InterceptorPlace.MessageSend,
            InterceptorLifetime.Global,
            (r, _, scope) =>
            {
                if (r is { CurrentMessage.ContentType: Pattern.HtmlContentType })
                {
                    var commonLinkUtility = scope.ServiceProvider.GetService<CommonLinkUtility>();

                    var body = r.CurrentMessage.Body;

                    body = _urlReplacer.Replace(body, m =>
                    {
                        var url = m.Groups["url"].Value;
                        var ind = m.Groups["url"].Index - m.Index;
                        return string.IsNullOrEmpty(url) && ind > 0 ?
                            m.Value.Insert(ind, commonLinkUtility.GetFullAbsolutePath(string.Empty)) :
                            m.Value.Replace(url, commonLinkUtility.GetFullAbsolutePath(url));
                    });

                    body = _textileLinkReplacer.Replace(body, m =>
                    {
                        var url = m.Groups["link"].Value;
                        var ind = m.Groups["link"].Index - m.Index;
                        return string.IsNullOrEmpty(url) && ind > 0 ?
                            m.Value.Insert(ind, commonLinkUtility.GetFullAbsolutePath(string.Empty)) :
                            m.Value.Replace(url, commonLinkUtility.GetFullAbsolutePath(url));
                    });

                    r.CurrentMessage.Body = body;
                }
                return false;
            });
        client.AddInterceptor(absoluteUrl);

        #endregion

        #region security and culture

        var securityAndCulture = new SendInterceptorSkeleton(
            "ProductSecurityInterceptor",
             InterceptorPlace.DirectSend,
             InterceptorLifetime.Global,
             async (r, p, scope) =>
             {
                 var scopeClass = scope.ServiceProvider.GetRequiredService<ProductSecurityInterceptor>();
                 return await scopeClass.InterceptAsync(r, p);
             });
        client.AddInterceptor(securityAndCulture);

        #endregion

        #region white label correction

        var whiteLabel = new SendInterceptorSkeleton(
            "WhiteLabelInterceptor",
             InterceptorPlace.MessageSend,
             InterceptorLifetime.Global,
             (r, _, scope) =>
             {
                 try
                 {
                     var tags = r.Arguments;

                     var logoTextTag = tags.Find(a => a.Tag == CommonTags.LetterLogoText);
                     var logoText = logoTextTag != null ? (string)logoTextTag.Value : string.Empty;

                     if (!string.IsNullOrEmpty(logoText))
                     {
                         r.CurrentMessage.Body = r.CurrentMessage.Body
                             .Replace("${{" + CommonTags.LetterLogoText + "}}", logoText);
                     }
                 }
                 catch (Exception error)
                 {
                     scope.ServiceProvider.GetService<ILogger<ProductSecurityInterceptor>>().ErrorNotifyClientRegisterCallback(error);
                 }
                 return false;
             });
        client.AddInterceptor(whiteLabel);

        #endregion
    }
}

[Scope]
public class ProductSecurityInterceptor(TenantManager tenantManager,
    WebItemSecurity webItemSecurity,
    UserManager userManager,
    CoreBaseSettings coreBaseSettings,
    ILogger<ProductSecurityInterceptor> logger)
{
    public async Task<bool> InterceptAsync(NotifyRequest r, InterceptorPlace p)
    {
        try
        {
            await tenantManager.GetCurrentTenantAsync();

            var u = await userManager.SearchUserAsync(r.Recipient.ID);

            if (Constants.LostUser.Equals(u))
            {
                return false;
            }

            // security
            var tag = r.Arguments.Find(a => a.Tag == CommonTags.ProductID);
            var productId = tag != null ? (Guid)tag.Value : Guid.Empty;

            if (productId == Guid.Empty)
            {
                productId = (Guid)(CallContext.GetData("asc.web.product_id") ?? Guid.Empty);
            }

            if (productId != Guid.Empty && productId != WebItemManager.PeopleProductID /* ignore people product */)
            {
                return !await webItemSecurity.IsAvailableForUserAsync(productId, u.Id);
            }
        }
        catch (Exception error)
        {
            logger.ErrorProductSecurityInterceptor(error);
        }

        return false;
    }
}

public static class NotifyConfigurationExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<NotifyTransferRequest>();
        services.TryAdd<ProductSecurityInterceptor>();
        services.TryAdd<TextileStyler>();
        services.TryAdd<JabberStyler>();
        services.TryAdd<PushStyler>();
    }
}

[Scope]
public class NotifyTransferRequest(TenantManager tenantManager,
        AuthContext authContext,
        UserManager userManager,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        ILogger<ProductSecurityInterceptor> logger,
        TenantExtra tenantExtra,
        WebItemManager webItemManager,
        TenantLogoManager tenantLogoManager,
        AdditionalWhiteLabelSettingsHelperInit additionalWhiteLabelSettingsHelper,
        MailWhiteLabelSettingsHelper mailWhiteLabelSettingsHelper,
        TenantUtil tenantUtil,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        SettingsManager settingsManager,
        StudioNotifyHelper studioNotifyHelper)
    : INotifyEngineAction
{
    public async Task BeforeTransferRequestAsync(NotifyRequest request)
    {
        var aid = Guid.Empty;
        var aname = string.Empty;
        var tenant = await tenantManager.GetCurrentTenantAsync();

        if (authContext.IsAuthenticated)
        {
            aid = authContext.CurrentAccount.ID;
            var user = await userManager.GetUsersAsync(aid);
            if (userManager.UserExists(user))
            {
                aname = user.DisplayUserName(false, displayUserSettingsHelper)
                .Replace(">", "&#62")
                .Replace("<", "&#60");
            }
        }

        var productid = CallContext.GetData("asc.web.product_id");
        var product = productid != null ? webItemManager[(Guid)productid] as IProduct : null;

        var logoText = TenantWhiteLabelSettings.DefaultLogoText;
        if ((tenantExtra.Enterprise || coreBaseSettings.CustomMode) && !await MailWhiteLabelSettings.IsDefaultAsync(settingsManager))
        {
            logoText = await tenantLogoManager.GetLogoTextAsync();
        }

        request.Arguments.Add(new TagValue(CommonTags.AuthorID, aid));
        request.Arguments.Add(new TagValue(CommonTags.AuthorName, aname));
        request.Arguments.Add(new TagValue(CommonTags.AuthorUrl, commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(aid))));
        request.Arguments.Add(new TagValue(CommonTags.VirtualRootPath, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')));
        request.Arguments.Add(new TagValue(CommonTags.ProductID, product?.ID ?? Guid.Empty));
        request.Arguments.Add(new TagValue(CommonTags.DateTime, tenantUtil.DateTimeNow()));
        request.Arguments.Add(new TagValue(CommonTags.RecipientID, Context.SysRecipient));
        request.Arguments.Add(new TagValue(CommonTags.ProfileUrl, commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff())));
        request.Arguments.Add(new TagValue(CommonTags.RecipientSubscriptionConfigURL, commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetUnsubscribe())));
        request.Arguments.Add(new TagValue(CommonTags.HelpLink, await commonLinkUtility.GetHelpLinkAsync(settingsManager, additionalWhiteLabelSettingsHelper, false)));
        request.Arguments.Add(new TagValue(CommonTags.SalesEmail, commonLinkUtility.GetSalesEmail(additionalWhiteLabelSettingsHelper)));
        request.Arguments.Add(new TagValue(CommonTags.SiteLink, commonLinkUtility.GetSiteLink(mailWhiteLabelSettingsHelper)));
        request.Arguments.Add(new TagValue(CommonTags.SupportLink, await commonLinkUtility.GetSupportLinkAsync(settingsManager, additionalWhiteLabelSettingsHelper, false)));
        request.Arguments.Add(new TagValue(CommonTags.SupportEmail, commonLinkUtility.GetSupportEmail(mailWhiteLabelSettingsHelper)));
        request.Arguments.Add(new TagValue(CommonTags.LetterLogoText, logoText));
        request.Arguments.Add(new TagValue(CommonTags.MailWhiteLabelSettings, await MailWhiteLabelSettings.InstanceAsync(settingsManager)));
        request.Arguments.Add(new TagValue(CommonTags.SendFrom, tenant.Name == "" ? Resource.PortalName : tenant.Name));
        request.Arguments.Add(new TagValue(CommonTags.ImagePath, studioNotifyHelper.GetNotificationImageUrl("").TrimEnd('/')));

        var topGifTag = request.Arguments.Find(x => x.Tag == CommonTags.TopGif);
        if (topGifTag == null || string.IsNullOrEmpty((string)topGifTag.Value))
        {
            await AddLetterLogoAsync(request);
        }
    }
    public void AfterTransferRequest(NotifyRequest request)
    {

    }

    private async Task AddLetterLogoAsync(NotifyRequest request)
    {

        try
        {
            var attachment = await tenantLogoManager.GetMailLogoAsAttachmentAsync();

            if (attachment != null)
            {
                request.Arguments.Add(new TagValue(CommonTags.LetterLogo, "cid:" + attachment.ContentId));
                request.Arguments.Add(new TagValue(CommonTags.EmbeddedAttachments, new[] { attachment }));
            }
        }
        catch (Exception error)
        {
            logger.ErrorAddLetterLogo(error);
        }
    }
}
