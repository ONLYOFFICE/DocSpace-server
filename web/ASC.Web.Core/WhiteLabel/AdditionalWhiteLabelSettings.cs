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

public class AdditionalWhiteLabelSettingsWrapper
{
    [SwaggerSchemaCustom("Additional white label settings")]
    public AdditionalWhiteLabelSettings Settings { get; set; }
}

public class AdditionalWhiteLabelSettings : ISettings<AdditionalWhiteLabelSettings>
{
    public AdditionalWhiteLabelSettingsHelperInit AdditionalWhiteLabelSettingsHelper;

    [SwaggerSchemaCustom("Specifies if the start document is enabled or not")]
    public bool StartDocsEnabled { get; init; }

    [SwaggerSchemaCustom("Specifies if the help center is enabled or not")]
    public bool HelpCenterEnabled { get; init; }

    [SwaggerSchemaCustom("Specifies if feedback and support are available or not")]
    public bool FeedbackAndSupportEnabled { get; init; }

    [SwaggerSchemaCustom("Feedback and support URL")]
    public string FeedbackAndSupportUrl { get; init; }

    [SwaggerSchemaCustom("Specifies if the user forum is enabled or not")]
    public bool UserForumEnabled { get; init; }

    [SwaggerSchemaCustom("User forum URL")]
    public string UserForumUrl { get; init; }

    [SwaggerSchemaCustom("Specifies if the video guides are enabled or not")]
    public bool VideoGuidesEnabled { get; init; }

    [SwaggerSchemaCustom("Video guides URL")]
    public string VideoGuidesUrl { get; init; }

    [SwaggerSchemaCustom("Sales email")]
    public string SalesEmail { get; init; }

    [SwaggerSchemaCustom("URL to pay for the portal")]
    public string BuyUrl { get; init; }

    [SwaggerSchemaCustom("Specifies if the license agreements are enabled or not")]
    public bool LicenseAgreementsEnabled { get; init; }

    [SwaggerSchemaCustom("License agreements URL")]
    public string LicenseAgreementsUrl { get; init; }

    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{0108422F-C05D-488E-B271-30C4032494DA}"); }
    }

    public AdditionalWhiteLabelSettings(AdditionalWhiteLabelSettingsHelperInit additionalWhiteLabelSettingsHelper)
    {
        this.AdditionalWhiteLabelSettingsHelper = additionalWhiteLabelSettingsHelper;
    }

    public AdditionalWhiteLabelSettings() { }

    public AdditionalWhiteLabelSettings GetDefault()
    {
        return new AdditionalWhiteLabelSettings(AdditionalWhiteLabelSettingsHelper)
        {
            StartDocsEnabled = true,
            HelpCenterEnabled = AdditionalWhiteLabelSettingsHelper?.DefaultHelpCenterUrl != null,
            FeedbackAndSupportEnabled = AdditionalWhiteLabelSettingsHelper?.DefaultFeedbackAndSupportUrl != null,
            FeedbackAndSupportUrl = AdditionalWhiteLabelSettingsHelper?.DefaultFeedbackAndSupportUrl,
            UserForumEnabled = AdditionalWhiteLabelSettingsHelper?.DefaultUserForumUrl != null,
            UserForumUrl = AdditionalWhiteLabelSettingsHelper?.DefaultUserForumUrl,
            VideoGuidesEnabled = AdditionalWhiteLabelSettingsHelper?.DefaultVideoGuidesUrl != null,
            VideoGuidesUrl = AdditionalWhiteLabelSettingsHelper?.DefaultVideoGuidesUrl,
            SalesEmail = AdditionalWhiteLabelSettingsHelper?.DefaultMailSalesEmail,
            BuyUrl = AdditionalWhiteLabelSettingsHelper?.DefaultBuyUrl,
            LicenseAgreementsEnabled = true,
            LicenseAgreementsUrl = DefaultLicenseAgreements
        };
    }

    public static string DefaultLicenseAgreements
    {
        get
        {
            return "https://help.onlyoffice.com/Products/Files/doceditor.aspx?fileid=6795868&doc=RG5GaVN6azdUQW5kLzZQNzBXbHZ4Rm9QWVZuNjZKUmgya0prWnpCd2dGcz0_IjY3OTU4Njgi0";
        }
    }
}

[Scope]
public class AdditionalWhiteLabelSettingsHelper(AdditionalWhiteLabelSettingsHelperInit additionalWhiteLabelSettingsHelperInit)
{
    public bool IsDefault(AdditionalWhiteLabelSettings settings)
    {
        settings.AdditionalWhiteLabelSettingsHelper ??= additionalWhiteLabelSettingsHelperInit;
        
        var defaultSettings = settings.GetDefault();

        return settings.StartDocsEnabled == defaultSettings.StartDocsEnabled &&
                settings.HelpCenterEnabled == defaultSettings.HelpCenterEnabled &&
                settings.FeedbackAndSupportEnabled == defaultSettings.FeedbackAndSupportEnabled &&
                settings.FeedbackAndSupportUrl == defaultSettings.FeedbackAndSupportUrl &&
                settings.UserForumEnabled == defaultSettings.UserForumEnabled &&
                settings.UserForumUrl == defaultSettings.UserForumUrl &&
                settings.VideoGuidesEnabled == defaultSettings.VideoGuidesEnabled &&
                settings.VideoGuidesUrl == defaultSettings.VideoGuidesUrl &&
                settings.SalesEmail == defaultSettings.SalesEmail &&
                settings.BuyUrl == defaultSettings.BuyUrl &&
                settings.LicenseAgreementsEnabled == defaultSettings.LicenseAgreementsEnabled &&
                settings.LicenseAgreementsUrl == defaultSettings.LicenseAgreementsUrl;
    }
}

[Singleton]
public class AdditionalWhiteLabelSettingsHelperInit(IConfiguration configuration)
{
    [SwaggerSchemaCustom("Default help center URL")]
    public string DefaultHelpCenterUrl
    {
        get
        {
            var url = configuration["web:help-center"];
            return string.IsNullOrEmpty(url) ? null : url;
        }
    }

    [SwaggerSchemaCustom("Default feedback and support URL")]
    public string DefaultFeedbackAndSupportUrl
    {
        get
        {
            var url = configuration["web:support-feedback"];
            return string.IsNullOrEmpty(url) ? null : url;
        }
    }

    [SwaggerSchemaCustom("Default user forum URL")]
    public string DefaultUserForumUrl
    {
        get
        {
            var url = configuration["web:user-forum"];
            return string.IsNullOrEmpty(url) ? null : url;
        }
    }

    [SwaggerSchemaCustom("Default video guides URL")]
    public string DefaultVideoGuidesUrl
    {
        get
        {
            var url = DefaultHelpCenterUrl;
            return string.IsNullOrEmpty(url) ? null : url + "/video.aspx";
        }
    }

    [SwaggerSchemaCustom("Default sales email")]
    public string DefaultMailSalesEmail
    {
        get
        {
            var email = configuration["core:payment:email"];
            return !string.IsNullOrEmpty(email) ? email : "sales@onlyoffice.com";
        }
    }

    [SwaggerSchemaCustom("Default URL to pay for the portal")]
    public string DefaultBuyUrl
    {
        get
        {
            var site = configuration["web:teamlab-site"];
            return !string.IsNullOrEmpty(site) ? site + "/post.ashx?type=buydocspaceenterprise" : "";
        }
    }
}
