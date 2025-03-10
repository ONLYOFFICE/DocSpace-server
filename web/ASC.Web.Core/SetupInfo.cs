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

namespace ASC.Web.Studio.Core;

[Singleton]
public class SetupInfo
{
    private static string _webAutotestSecretEmail;
    private static string[] _hideSettings;

    public string ZendeskKey { get; private set; }
    public string TagManagerId { get; private set; }
    public long MaxImageUploadSize { get; private set; }

    /// <summary>
    /// Max possible file size for not chunked upload. Less or equal than 100 mb.
    /// </summary>
    public async Task<long> MaxUploadSize(TenantManager tenantManager, MaxTotalSizeStatistic maxTotalSizeStatistic)
    {
        return Math.Min(AvailableFileSize, await MaxChunkedUploadSize(tenantManager, maxTotalSizeStatistic));
    }

    public long AvailableFileSize { get; }
    public int MaxUploadThreadCount { get; set; }
    public long ChunkUploadSize { get; set; }
    public long ProviderMaxUploadSize { get; private set; }
    public bool ThirdPartyAuthEnabled { get; private set; }
    public string TipsAddress { get; private set; }
    public string WebApiBaseUrl { get { return VirtualPathUtility.ToAbsolute(GetAppSettings("api.url", "~/api/2.0/")); } }

    public static bool IsSecretEmail(string email)
    {
        email = Regex.Replace(email ?? "", "\\.*(?=\\S*(@gmail.com$))", "").ToLower();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(_webAutotestSecretEmail))
        {
            return false;
        }

        var regex = new Regex(_webAutotestSecretEmail, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        return regex.IsMatch(email);
    }

    public string SsoSamlLoginUrl { get; private set; }
    public string SsoSamlLogoutUrl { get; private set; }
    public bool SmsTrial { get; private set; }
    public string TfaRegistration { get; private set; }
    public int TfaAppBackupCodeLength { get; private set; }
    public int TfaAppBackupCodeCount { get; private set; }
    public string TfaAppSender { get; private set; }
    public string RecaptchaPublicKey { get; private set; }
    public string RecaptchaPrivateKey { get; private set; }
    public string RecaptchaVerifyUrl { get; private set; }
    public string HcaptchaPublicKey { get; private set; }
    public string HcaptchaPrivateKey { get; private set; }
    public string HcaptchaVerifyUrl { get; private set; }
    public string AmiMetaUrl { get; private set; }
    public string AmiTokenUrl { get; private set; }
    public int InvitationLimit { get; private set; }

    private readonly IConfiguration _configuration;

    public SetupInfo(IConfiguration configuration)
    {
        _configuration = configuration;

        ZendeskKey = GetAppSettings("web:zendesk-key", string.Empty);
        TagManagerId = GetAppSettings("web:tagmanager-id", string.Empty);

        MaxImageUploadSize = GetAppSettings("web:max-upload-size", 1024L * 1024L);
        AvailableFileSize = GetAppSettings("web:available-file-size", 100L * 1024L * 1024L);
        MaxUploadThreadCount = GetAppSettings("core:hosting:rateLimiterOptions:defaultConcurrencyWriteRequests", 15);
        ChunkUploadSize = GetAppSettings("files:uploader:chunk-size", 10 * 1024 * 1024);
        ProviderMaxUploadSize = GetAppSettings("files:provider:max-upload-size", 1024L * 1024L * 1024L);
        ThirdPartyAuthEnabled = string.Equals(GetAppSettings("web:thirdparty-auth", "true"), "true");

        TipsAddress = GetAppSettings("web.promo-tips-url", string.Empty);

        _webAutotestSecretEmail = GetAppSettings("web:autotest:secret-email", string.Empty);

        RecaptchaPublicKey = GetAppSettings("web:recaptcha:public-key", null);
        RecaptchaPrivateKey = GetAppSettings("web:recaptcha:private-key", null);
        RecaptchaVerifyUrl = GetAppSettings("web:recaptcha:verify-url", "https://www.recaptcha.net/recaptcha/api/siteverify");

        HcaptchaPublicKey = GetAppSettings("web:hcaptcha:public-key", null);
        HcaptchaPrivateKey = GetAppSettings("web:hcaptcha:private-key", null);
        HcaptchaVerifyUrl = GetAppSettings("web:hcaptcha:verify-url", "https://api.hcaptcha.com/siteverify");

        SsoSamlLoginUrl = GetAppSettings("web:sso:saml:login:url", "");
        SsoSamlLogoutUrl = GetAppSettings("web:sso:saml:logout:url", "");

        _hideSettings = configuration.GetSection("web:hide-settings").Get<string[]>() ?? [];

        SmsTrial = GetAppSettings("core.sms.trial", false);

        TfaRegistration = GetAppSettings("core.tfa.registration", "").ToLower();

        TfaAppBackupCodeLength = GetAppSettings("web.tfaapp.backup.length", 6);
        TfaAppBackupCodeCount = GetAppSettings("web.tfaapp.backup.count", 5);
        TfaAppSender = GetAppSettings("web.tfaapp.backup.title", "ONLYOFFICE");

        AmiMetaUrl = GetAppSettings("web:ami:meta", "");
        AmiTokenUrl = GetAppSettings("web:ami:token", "");

        InvitationLimit = GetAppSettings("web:invitation-limit", int.MaxValue);
    }


    //TODO
    public static bool IsVisibleSettings<TSettings>()
    {
        return IsVisibleSettings(typeof(TSettings).Name);
    }

    public static bool IsVisibleSettings(string settings)
    {
        return _hideSettings == null || !_hideSettings.Contains(settings, StringComparer.CurrentCultureIgnoreCase);
    }

    public async Task<long> MaxChunkedUploadSize(TenantManager tenantManager, MaxTotalSizeStatistic maxTotalSizeStatistic)
    {
        var diskQuota = await tenantManager.GetCurrentTenantQuotaAsync();
        if (diskQuota != null)
        {
            var usedSize = await maxTotalSizeStatistic.GetValueAsync();
            var freeSize = Math.Max(diskQuota.MaxTotalSize - usedSize, 0);
            return Math.Min(freeSize, diskQuota.MaxFileSize);
        }
        return ChunkUploadSize;
    }

    private string GetAppSettings(string key, string defaultValue)
    {
        var result = _configuration[key] ?? defaultValue;

        if (!string.IsNullOrEmpty(result))
        {
            result = result.Trim();
        }

        return result;

    }

    private T GetAppSettings<T>(string key, T defaultValue)
    {
        var configSetting = _configuration[key];
        if (!string.IsNullOrEmpty(configSetting))
        {
            configSetting = configSetting.Trim();
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter.CanConvertFrom(typeof(string)))
            {
                return (T)converter.ConvertFromString(configSetting);
            }
        }
        return defaultValue;
    }
}
