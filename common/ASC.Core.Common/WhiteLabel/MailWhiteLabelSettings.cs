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

namespace ASC.Web.Core.WhiteLabel;

/// <summary>
/// Mail white label settings
/// </summary>
public class MailWhiteLabelSettings : ISettings<MailWhiteLabelSettings>
{
    private readonly MailWhiteLabelSettingsHelper _mailWhiteLabelSettingsHelper;
    private readonly IConfiguration _configuration;

    /// <summary>Specifies if the mail footer is enabled or not</summary>
    /// <type>System.Boolean, System</type>
    public bool FooterEnabled { get; set; }

    /// <summary>Specifies if the footer with social media contacts is enabled or not</summary>
    /// <type>System.Boolean, System</type>
    public bool FooterSocialEnabled { get; init; }

    /// <summary>Support URL</summary>
    /// <type>System.String, System</type>
    public string SupportUrl { get; init; }

    /// <summary>Support email</summary>
    /// <type>System.String, System</type>
    public string SupportEmail { get; init; }

    /// <summary>Sales email</summary>
    /// <type>System.String, System</type>
    public string SalesEmail { get; init; }

    /// <summary>Demo URL</summary>
    /// <type>System.String, System</type>
    public string DemoUrl { get; init; }

    /// <summary>Site URL</summary>
    /// <type>System.String, System</type>
    public string SiteUrl { get; init; }

    [JsonIgnore]
    public Guid ID => new("{C3602052-5BA2-452A-BD2A-ADD0FAF8EB88}");

    public MailWhiteLabelSettings(IConfiguration configuration)
    {
        _mailWhiteLabelSettingsHelper = new MailWhiteLabelSettingsHelper(configuration);
        _configuration = configuration;
    }

    public MailWhiteLabelSettings()
    {

    }

    public MailWhiteLabelSettings GetDefault()
    {
        return new MailWhiteLabelSettings(_configuration)
        {
            FooterEnabled = true,
            FooterSocialEnabled = true,
            SupportUrl = _mailWhiteLabelSettingsHelper?.DefaultMailSupportUrl,
            SupportEmail = _mailWhiteLabelSettingsHelper?.DefaultMailSupportEmail,
            SalesEmail = _mailWhiteLabelSettingsHelper?.DefaultMailSalesEmail,
            DemoUrl = _mailWhiteLabelSettingsHelper?.DefaultMailDemoUrl,
            SiteUrl = _mailWhiteLabelSettingsHelper?.DefaultMailSiteUrl
        };
    }

    public bool IsDefault()
    {
        var defaultSettings = GetDefault();
        return FooterEnabled == defaultSettings.FooterEnabled &&
                FooterSocialEnabled == defaultSettings.FooterSocialEnabled &&
                SupportUrl == defaultSettings.SupportUrl &&
                SupportEmail == defaultSettings.SupportEmail &&
                SalesEmail == defaultSettings.SalesEmail &&
                DemoUrl == defaultSettings.DemoUrl &&
                SiteUrl == defaultSettings.SiteUrl;
    }

    public static async Task<MailWhiteLabelSettings> InstanceAsync(SettingsManager settingsManager)
    {
        return await settingsManager.LoadForDefaultTenantAsync<MailWhiteLabelSettings>();
    }

    public static async Task<bool> IsDefaultAsync(SettingsManager settingsManager)
    {
        return (await InstanceAsync(settingsManager)).IsDefault();
    }
}

[Singleton]
public class MailWhiteLabelSettingsHelper(IConfiguration configuration)
{
    public string DefaultMailSupportUrl
    {
        get
        {
            var url = BaseCommonLinkUtility.GetRegionalUrl(configuration["externalresources:support"] ?? string.Empty, null);

            return !string.IsNullOrEmpty(url) ? url : "http://helpdesk.onlyoffice.com";
        }
    }

    public string DefaultMailSupportEmail
    {
        get
        {
            var email = configuration["externalresources:supportemail"];

            return !string.IsNullOrEmpty(email) ? email : "support@onlyoffice.com";
        }
    }

    public string DefaultMailSalesEmail
    {
        get
        {
            var email = configuration["externalresources:paymentemail"];

            return !string.IsNullOrEmpty(email) ? email : "sales@onlyoffice.com";
        }
    }

    public string DefaultMailDemoUrl
    {
        get
        {
            var url = BaseCommonLinkUtility.GetRegionalUrl(configuration["externalresources:orderdemo"] ?? string.Empty, null);

            return !string.IsNullOrEmpty(url) ? url : "https://www.onlyoffice.com/demo-order.aspx";
        }
    }

    public string DefaultMailSiteUrl
    {
        get
        {
            var url = configuration["externalresources:site"];

            return !string.IsNullOrEmpty(url) ? url : "https://www.onlyoffice.com";
        }
    }

    public string DefaultMailForumUrl
    {
        get
        {
            var url = configuration["externalresources:forum"];

            return !string.IsNullOrEmpty(url) ? url : "https://forum.onlyoffice.com";
        }
    }
}
