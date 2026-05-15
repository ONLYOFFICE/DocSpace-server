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

namespace ASC.Core.Users;

[Singleton]
public class UserFormatter : IComparer<UserInfo>
{
    private static readonly Dictionary<string, Dictionary<DisplayUserNameFormat, string>> _displayFormats = new()
    {
        { "ru", new Dictionary<DisplayUserNameFormat, string>{ { DisplayUserNameFormat.Default, "{1} {0}" }, { DisplayUserNameFormat.FirstLast, "{0} {1}" }, { DisplayUserNameFormat.LastFirst, "{1} {0}" } } },
        { "default", new Dictionary<DisplayUserNameFormat, string>{ {DisplayUserNameFormat.Default, "{0} {1}" }, { DisplayUserNameFormat.FirstLast, "{0} {1}" }, { DisplayUserNameFormat.LastFirst, "{1}, {0}" } } }
    };

    private readonly IConfiguration _configuration;
    private readonly DisplayUserNameFormat _format;
    private bool _forceFormatChecked;
    private string _forceFormat;
    public Regex UserNameRegex { get; }

    public UserFormatter(IConfiguration configuration)
    {
        _format = DisplayUserNameFormat.Default;
        _configuration = configuration;
        UserNameRegex = new Regex(_configuration["core:username:regex"] ?? "");
    }

    public string GetUserName(UserInfo userInfo, DisplayUserNameFormat format = DisplayUserNameFormat.Default)
    {
        ArgumentNullException.ThrowIfNull(userInfo);

        return GetUserDisplayFormat(format, userInfo.FirstName, userInfo.LastName);
    }

    public string GetUserName(string firstName, string lastName)
    {
        if (string.IsNullOrEmpty(firstName))
        {
            throw new ArgumentException(firstName);
        }

        if (string.IsNullOrEmpty(lastName))
        {
            throw new ArgumentException(lastName);
        }

        return GetUserDisplayFormat(DisplayUserNameFormat.Default, firstName, lastName);
    }

    int IComparer<UserInfo>.Compare(UserInfo x, UserInfo y)
    {
        return Compare(x, y, _format);
    }

    public static int Compare(UserInfo x, UserInfo y, DisplayUserNameFormat format)
    {
        if (x == null)
        {
            if (y == null)
            {
                return 0;
            }

            return -1;
        }

        if (y == null)
        {
            return +1;
        }

        if (format == DisplayUserNameFormat.Default)
        {
            format = GetUserDisplayDefaultOrder();
        }

        int result;
        if (format == DisplayUserNameFormat.FirstLast)
        {
            result = string.Compare(x.FirstName, y.FirstName, StringComparison.OrdinalIgnoreCase);
            if (result == 0)
            {
                result = string.Compare(x.LastName, y.LastName, StringComparison.OrdinalIgnoreCase);
            }
        }
        else
        {
            result = string.Compare(x.LastName, y.LastName, StringComparison.OrdinalIgnoreCase);
            if (result == 0)
            {
                result = string.Compare(x.FirstName, y.FirstName, StringComparison.OrdinalIgnoreCase);
            }
        }

        return result;
    }

    private string GetUserDisplayFormat(DisplayUserNameFormat format, string firstName, string lastName)
    {
        string formatString;
        if (!_forceFormatChecked)
        {
            _forceFormat = _configuration["core:user-display-format"];
            if (string.IsNullOrEmpty(_forceFormat))
            {
                _forceFormat = null;
            }

            _forceFormatChecked = true;
        }

        if (_forceFormat != null)
        {
            formatString = _forceFormat;
        }
        else
        {
            var culture = CultureInfo.CurrentCulture.Name;
            if (!_displayFormats.TryGetValue(culture, out var formats))
            {
                var twoLetterIsoLanguageName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                if (!_displayFormats.TryGetValue(twoLetterIsoLanguageName, out formats))
                {
                    formats = _displayFormats["default"];
                }
            }
            formatString = formats[format];
        }

        if (IsChineseText(firstName) || IsChineseText(lastName))
        {
            formatString = "{1}{0}";
        }

        return string.Format(formatString, firstName, lastName).Trim();
    }

    private static bool IsChineseText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var pattern = @"[\u4e00-\u9fff]";
        return Regex.IsMatch(text, pattern);
    }

    public static DisplayUserNameFormat GetUserDisplayDefaultOrder()
    {
        var culture = CultureInfo.CurrentCulture.Name;
        if (!_displayFormats.TryGetValue(culture, out var formats))
        {
            var twoletter = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            if (!_displayFormats.TryGetValue(twoletter, out formats))
            {
                formats = _displayFormats["default"];
            }
        }
        var format = formats[DisplayUserNameFormat.Default];

        return format.IndexOf("{0}") < format.IndexOf("{1}") ? DisplayUserNameFormat.FirstLast : DisplayUserNameFormat.LastFirst;
    }

    public bool IsValidUserName(string firstName, string lastName)
    {
        if (!UserNameRegex.IsMatch(firstName))
        {
            return false;
        }

        return string.IsNullOrEmpty(lastName) || UserNameRegex.IsMatch(lastName);
    }
}