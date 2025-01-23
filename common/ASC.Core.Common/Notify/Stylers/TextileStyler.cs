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

namespace ASC.Notify.Textile;

[Scope]
public class TextileStyler(CoreBaseSettings coreBaseSettings,
        IConfiguration configuration,
        InstanceCrypto instanceCrypto,
        ExternalResourceSettingsHelper externalResourceSettingsHelper,
        MailWhiteLabelSettingsHelper mailWhiteLabelSettingsHelper)
    : IPatternStyler
{
    private static readonly Regex _velocityArguments
        = new(NVelocityPatternFormatter.NoStylePreffix + "(?<arg>.*?)" + NVelocityPatternFormatter.NoStyleSuffix,
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    static TextileStyler()
    {
        const string file = "ASC.Core.Common.Notify.Stylers.Resources.style.css";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file);
        using var reader = new StreamReader(stream);
        BlockAttributesParser.Styler = new StyleReader(reader.ReadToEnd().Replace("\n", "").Replace("\r", ""));
    }

    public async Task ApplyFormatingAsync(NoticeMessage message)
    {
        var output = new StringBuilderTextileFormatter();
        var formatter = new TextileFormatter(output);

        if (!string.IsNullOrEmpty(message.Subject))
        {
            message.Subject = _velocityArguments.Replace(message.Subject, m => m.Result("${arg}"));
        }

        if (string.IsNullOrEmpty(message.Body))
        {
            return;
        }

        formatter.Format(message.Body);

        var template = GetTemplate(message);
        var imagePath = GetImagePath(message);
        var mailSettings = GetMailSettings(message);
        var unsubscribeText = await GetUnsubscribeTextAsync(message, mailSettings);

        InitTopImage(message, mailSettings, out var topImage);
        InitFooter(message, mailSettings, out var footerContent, out var footerSocialContent);

        var content = output.GetFormattedText().Trim();

        if (!content.StartsWith("<tr"))
        {
            content = $@"<tr border=""0"" cellspacing=""0"" cellpadding=""0"">
                <td style=""padding: 0 40px; margin: 0; text-align: center; vertical-align: top; width: 600px;"">{content}</td>
            </tr>";
        }

        message.Body = template.Replace("%CONTENT%", content)
                               .Replace("%TOPIMAGE%", topImage)
                               .Replace("%FOOTER%", footerContent)
                               .Replace("%FOOTERSOCIAL%", footerSocialContent)
                               .Replace("%TEXTFOOTER%", unsubscribeText)
                               .Replace("%IMAGEPATH%", imagePath);
    }

    private static string GetTemplate(NoticeMessage message)
    {
        var template = NotifyTemplateResource.HtmlMaster;

        var templateTag = message.GetArgument("MasterTemplate");
        if (templateTag != null)
        {
            var templateTagValue = templateTag.Value as string;
            if (!string.IsNullOrEmpty(templateTagValue))
            {
                var templateValue = NotifyTemplateResource.ResourceManager.GetString(templateTagValue);
                if (!string.IsNullOrEmpty(templateValue))
                {
                    template = templateValue;
                }
            }
        }

        return template;
    }

    private static string GetImagePath(NoticeMessage message)
    {
        var imagePathTag = message.GetArgument("ImagePath");

        return imagePathTag == null ? string.Empty : (string)imagePathTag.Value;
    }

    private string GetLogoImg(NoticeMessage message, string imagePath)
    {
        var logoImg = configuration["web:logo:mail"];
        if (string.IsNullOrEmpty(logoImg))
        {
            var logo = message.GetArgument("LetterLogo");
            if (logo != null && ((string)logo.Value).Length != 0)
            {
                logoImg = (string)logo.Value;
            }
            else
            {
                logoImg = imagePath + "/mail_logo.png";
            }
        }

        return logoImg;
    }

    private string GetLogoText(NoticeMessage message)
    {
        var logoText = configuration["web:logotext:mail"];

        if (string.IsNullOrEmpty(logoText))
        {
            var llt = message.GetArgument("LetterLogoText");
            if (llt != null && ((string)llt.Value).Length != 0)
            {
                logoText = (string)llt.Value;
            }
            else
            {
                logoText = BaseWhiteLabelSettings.DefaultLogoText;
            }
        }

        return logoText;
    }

    private static MailWhiteLabelSettings GetMailSettings(NoticeMessage message)
    {
        var mailWhiteLabelTag = message.GetArgument("MailWhiteLabelSettings");

        return mailWhiteLabelTag?.Value as MailWhiteLabelSettings;
    }

    private void InitFooter(NoticeMessage message, MailWhiteLabelSettings settings, out string footerContent, out string footerSocialContent)
    {
        footerContent = string.Empty;
        footerSocialContent = string.Empty;

        var footer = message.GetArgument("Footer");

        if (footer == null)
        {
            return;
        }

        var footerValue = (string)footer.Value;

        if (string.IsNullOrEmpty(footerValue))
        {
            return;
        }

        switch (footerValue)
        {
            case "common":
                InitCommonFooter(settings, out footerContent, out footerSocialContent);
                break;
            case "social":
                InitSocialFooter(settings, out footerSocialContent);
                break;
            case "opensource":
                InitOpensourceFooter(settings, out footerContent, out footerSocialContent);
                break;
        }

        footerSocialContent = footerSocialContent
            .Replace("%SOCIALNETWORKFACEBOOK%", externalResourceSettingsHelper.GetDefaultFullEntry("socialnetwork_facebook"))
            .Replace("%SOCIALNETWORKTWITTER%", externalResourceSettingsHelper.GetDefaultFullEntry("socialnetwork_twitter"))
            .Replace("%SOCIALNETWORKYOUTUBE%", externalResourceSettingsHelper.GetDefaultFullEntry("socialnetwork_youtube"))
            .Replace("%SOCIALNETWORKINSTAGRAM%", externalResourceSettingsHelper.GetDefaultFullEntry("socialnetwork_instagram"))
            .Replace("%SOCIALNETWORKTIKTOK%", externalResourceSettingsHelper.GetDefaultFullEntry("socialnetwork_tiktok"));
    }

    private void InitTopImage(NoticeMessage message, MailWhiteLabelSettings settings, out string footerTop)
    {
        var imagePath = GetImagePath(message);
        var logoImg = GetLogoImg(message, imagePath);
        var logoText = GetLogoText(message);
        var siteUrl = settings == null ? mailWhiteLabelSettingsHelper.DefaultMailSiteUrl : settings.SiteUrl;
        var topGif = message.GetArgument("TopGif");

        if (topGif != null && !string.IsNullOrEmpty((string)topGif.Value))
        {
            footerTop = NotifyTemplateResource.TopGif
                .Replace("%LOGO%", (string)topGif.Value)
                .Replace("%LOGOTEXT%", logoText)
                .Replace("%SITEURL%", siteUrl);
        }
        else
        {
            footerTop = NotifyTemplateResource.TopLogo
                .Replace("%LOGO%", logoImg)
                .Replace("%LOGOTEXT%", logoText)
                .Replace("%SITEURL%", siteUrl);
        }
    }

    private void InitCommonFooter(MailWhiteLabelSettings settings, out string footerContent, out string footerSocialContent)
    {
        footerContent = string.Empty;
        footerSocialContent = string.Empty;

        if (settings == null)
        {
            footerContent =
                NotifyTemplateResource.FooterCommonV10
                                      .Replace("%SUPPORTURL%", mailWhiteLabelSettingsHelper.DefaultMailSupportUrl)
                                      .Replace("%SALESEMAIL%", mailWhiteLabelSettingsHelper.DefaultMailSalesEmail)
                                      .Replace("%DEMOURL%", mailWhiteLabelSettingsHelper.DefaultMailDemoUrl);
            footerSocialContent = NotifyTemplateResource.SocialNetworksFooter;

        }
        else if (settings.FooterEnabled)
        {
            footerContent =
                NotifyTemplateResource.FooterCommonV10
                .Replace("%SUPPORTURL%", string.IsNullOrEmpty(settings.SupportUrl) ? "mailto:" + settings.SupportEmail : settings.SupportUrl)
                .Replace("%SALESEMAIL%", settings.SalesEmail)
                .Replace("%DEMOURL%", string.IsNullOrEmpty(settings.DemoUrl) ? "mailto:" + settings.SalesEmail : settings.DemoUrl);

            footerSocialContent = settings.FooterSocialEnabled ? NotifyTemplateResource.SocialNetworksFooter : string.Empty;
        }
    }

    private static void InitSocialFooter(MailWhiteLabelSettings settings, out string footerSocialContent)
    {
        footerSocialContent = string.Empty;

        if (settings == null || (settings.FooterEnabled && settings.FooterSocialEnabled))
        {
            footerSocialContent = NotifyTemplateResource.SocialNetworksFooter;
        }
    }

    private void InitOpensourceFooter(MailWhiteLabelSettings settings, out string footerContent, out string footerSocialContent)
    {
        footerContent = string.Empty;
        footerSocialContent = string.Empty;

        if (settings == null)
        {
            footerContent =
                NotifyTemplateResource.FooterOpensourceV10
                                      .Replace("%SUPPORTURL%", mailWhiteLabelSettingsHelper.DefaultMailForumUrl)
                                      .Replace("%SALESEMAIL%", mailWhiteLabelSettingsHelper.DefaultMailSalesEmail)
                                      .Replace("%DEMOURL%", mailWhiteLabelSettingsHelper.DefaultMailDemoUrl);
            footerSocialContent = NotifyTemplateResource.SocialNetworksFooter;

        }
        else if (settings.FooterEnabled)
        {
            footerContent =
                NotifyTemplateResource.FooterOpensourceV10
                .Replace("%SUPPORTURL%", mailWhiteLabelSettingsHelper.DefaultMailForumUrl)
                .Replace("%SALESEMAIL%", settings.SalesEmail)
                .Replace("%DEMOURL%", string.IsNullOrEmpty(settings.DemoUrl) ? "mailto:" + settings.SalesEmail : settings.DemoUrl);

            footerSocialContent = settings.FooterSocialEnabled ? NotifyTemplateResource.SocialNetworksFooter : string.Empty;
        }
    }

    private async Task<string> GetUnsubscribeTextAsync(NoticeMessage message, MailWhiteLabelSettings settings)
    {
        var withoutUnsubscribe = message.GetArgument("WithoutUnsubscribe");

        if (withoutUnsubscribe != null && (bool)withoutUnsubscribe.Value)
        {
            return string.Empty;
        }

        var unsubscribeLink = await GetPortalUnsubscribeLinkAsync(message, settings);

        if (string.IsNullOrEmpty(unsubscribeLink))
        {
            return string.Empty;
        }

        var rootPath = message.GetArgument("__VirtualRootPath").Value;

        return string.Format(NotifyTemplateResource.TextForFooterUnsubsribe, rootPath, unsubscribeLink)
            .Replace("%SUPPORTURL%", string.IsNullOrEmpty(settings.SupportUrl) ? "mailto:" + settings.SupportEmail : settings.SupportUrl)
            .Replace("%SALESEMAIL%", settings.SalesEmail);
    }

    private async Task<string> GetPortalUnsubscribeLinkAsync(NoticeMessage message, MailWhiteLabelSettings settings)
    {
        var subscriptionConfigArgument = message.GetArgument("RecipientSubscriptionConfigURL");

        var subscriptionConfigLink = (string)subscriptionConfigArgument?.Value;

        if (!string.IsNullOrEmpty(subscriptionConfigLink))
        {
            return subscriptionConfigLink;
        }

        var unsubscribeLinkArgument = message.GetArgument("ProfileUrl");

        var unsubscribeLink = (string)unsubscribeLinkArgument?.Value;

        if (!string.IsNullOrEmpty(unsubscribeLink))
        {
            return unsubscribeLink + "/notification";
        }

        return await GetSiteUnsubscribeLinkAsync(message, settings);
    }

    private async Task<string> GetSiteUnsubscribeLinkAsync(NoticeMessage message, MailWhiteLabelSettings settings)
    {
        var mail = message.Recipient.Addresses.FirstOrDefault(r => r.Contains('@'));

        if (string.IsNullOrEmpty(mail))
        {
            return string.Empty;
        }

        var format = coreBaseSettings.CustomMode
                         ? "{0}/unsubscribe/{1}"
                         : "{0}/Unsubscribe.aspx?id={1}";

        var site = settings == null
                       ? mailWhiteLabelSettingsHelper.DefaultMailSiteUrl
                       : settings.SiteUrl;

        var input = await instanceCrypto.EncryptAsync(Encoding.UTF8.GetBytes(mail.ToLowerInvariant()));
        return string.Format(format, site, WebEncoders.Base64UrlEncode(input));
    }
}
