// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.ApiSystem.Classes;

[Singleton]
public class TimeZonesProvider(CommonConstants commonConstants)
{
    #region Private

    private static readonly Dictionary<string, string> _timeZoneMap = new()
    {
        { "", "Europe/London" },
        { "fr", "Europe/Paris" },
        { "es", "Europe/Madrid" },
        { "de", "Europe/Berlin" },
        { "ru", "Europe/Moscow" },
        { "lv", "Europe/Riga" },
        { "pt", "America/Cuiaba" },
        { "it", "Europe/Rome" },
        { "tr", "Europe/Istanbul" },
        { "id", "Europe/London" },
        { "zh", "Asia/Shanghai" },
        { "ja", "Asia/Tokyo" },
        { "ko", "Asia/Seoul" },
        { "az", "Asia/Baku" },
        { "cs", "Europe/Warsaw" },
        { "el", "Europe/Warsaw" },
        { "fi", "Europe/Warsaw" },
        { "pl", "Europe/Warsaw" },
        { "uk", "Europe/Kiev" },
        { "vi", "Asia/Shanghai" }
    };

    private static readonly Dictionary<string, CultureInfo> _cultureUiMap = new()
    {
        { "", CultureInfo.GetCultureInfo("en-US") },
        { "en", CultureInfo.GetCultureInfo("en-GB") },
        { "az", CultureInfo.GetCultureInfo("az") },
        { "cs", CultureInfo.GetCultureInfo("cs") },
        { "de", CultureInfo.GetCultureInfo("de") },
        { "es", CultureInfo.GetCultureInfo("es") },
        { "fr", CultureInfo.GetCultureInfo("fr") },
        { "it", CultureInfo.GetCultureInfo("it") },
        { "lv", CultureInfo.GetCultureInfo("lv") },
        { "nl", CultureInfo.GetCultureInfo("nl") },
        { "pl", CultureInfo.GetCultureInfo("pl") },
        { "pt", CultureInfo.GetCultureInfo("pt") },
        { "ro", CultureInfo.GetCultureInfo("ro") },
        { "sk", CultureInfo.GetCultureInfo("sk") },
        { "sl", CultureInfo.GetCultureInfo("sl") },
        { "fi", CultureInfo.GetCultureInfo("fi") },
        { "vi", CultureInfo.GetCultureInfo("vi") },
        { "tr", CultureInfo.GetCultureInfo("tr") },
        { "el", CultureInfo.GetCultureInfo("el-GR") },
        { "bg", CultureInfo.GetCultureInfo("bg") },
        { "ru", CultureInfo.GetCultureInfo("ru") },
        { "sr", CultureInfo.GetCultureInfo("sr-Latn-RS") },
        { "uk", CultureInfo.GetCultureInfo("uk-UA") },
        { "hy", CultureInfo.GetCultureInfo("hy-AM") },
        { "ar", CultureInfo.GetCultureInfo("ar-SA") },
        { "si", CultureInfo.GetCultureInfo("si") },
        { "lo", CultureInfo.GetCultureInfo("lo-LA") },
        { "zh", CultureInfo.GetCultureInfo("zh-CN") },
        { "ja", CultureInfo.GetCultureInfo("ja-JP") },
        { "ko", CultureInfo.GetCultureInfo("ko-KR") }
    };

    #endregion


    #region Public

    public TimeZoneInfo GetCurrentTimeZoneInfo(string languageKey)
    {
        var timeZoneId = _timeZoneMap.TryGetValue(languageKey, out var id) ? id : _timeZoneMap[""];

        return TimeZoneConverter.GetTimeZone(timeZoneId);
    }

    public CultureInfo GetCurrentCulture(string languageKey)
    {
        if (string.IsNullOrEmpty(languageKey))
        {
            return commonConstants.DefaultCulture;
        }

        var culture = _cultureUiMap.GetValueOrDefault(languageKey);

        return culture ?? commonConstants.DefaultCulture;
    }

    #endregion
}