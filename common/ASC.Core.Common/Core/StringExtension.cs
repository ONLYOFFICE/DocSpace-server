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

namespace System;

public static class StringExtension
{
    private static readonly Regex _reStrict = new(@"^(([^<>()[\]\\.,;:\s@\""]+"
                                                  + @"(\.[^<>()[\]\\.,;:\s@\""]+)*)|(\"".+\""))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}"
                                                  + @"\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$");


    /// <param name="str"></param>
    extension(string str)
    {
        public string HtmlEncode()
        {
            return !string.IsNullOrEmpty(str) ? HttpUtility.HtmlEncode(str) : str;
        }

        /// <summary>
        /// Replace ' on ′
        /// </summary>
        /// <returns></returns>
        public string ReplaceSingleQuote()
        {
            return str?.Replace('\'', '′');
        }

        public bool TestEmailRegex()
        {
            str = (str ?? "").Trim();
            return !string.IsNullOrEmpty(str) && _reStrict.IsMatch(str);
        }

        public bool TestEmailPunyCode()
        {
            var toTest = _reStrict.Match(str);
            return toTest.Groups.Values.Distinct().Select(r => r.Value).Where(r => !string.IsNullOrEmpty(r)).Any(TestPunnyCode);
        }

        public bool TestUrlPunyCode()
        {
            return Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out var uri) && TestPunnyCode(uri.DnsSafeHost);
        }

        public bool TestPunnyCode()
        {
            var idn = new IdnMapping();

            try
            {
                var punyCode = idn.GetAscii(str.TrimStart('.'));
                var domain2 = idn.GetUnicode(punyCode);

                if (!string.Equals(punyCode, domain2))
                {
                    return true;
                }
            }
            catch (ArgumentException ex) when (ex.ParamName == "unicode")
            {
                return true;
            }

            return false;
        }

        public int EnumerableComparer(string y)
        {
            var xIndex = 0;
            var yIndex = 0;

            while (xIndex < str.Length)
            {
                if (yIndex >= y.Length)
                {
                    return 1;
                }

                if (char.IsDigit(str[xIndex]) && char.IsDigit(y[yIndex]))
                {
                    var xBuilder = new StringBuilder();
                    while (xIndex < str.Length && char.IsDigit(str[xIndex]))
                    {
                        xBuilder.Append(str[xIndex++]);
                    }

                    var yBuilder = new StringBuilder();
                    while (yIndex < y.Length && char.IsDigit(y[yIndex]))
                    {
                        yBuilder.Append(y[yIndex++]);
                    }

                    long xValue;
                    try
                    {
                        xValue = Convert.ToInt64(xBuilder.ToString());
                    }
                    catch (OverflowException)
                    {
                        xValue = long.MaxValue;
                    }

                    long yValue;
                    try
                    {
                        yValue = Convert.ToInt64(yBuilder.ToString());
                    }
                    catch (OverflowException)
                    {
                        yValue = long.MaxValue;
                    }

                    int difference;
                    if ((difference = xValue.CompareTo(yValue)) != 0)
                    {
                        return difference;
                    }
                }
                else
                {
                    int difference;
                    if ((difference = string.Compare(str[xIndex].ToString(CultureInfo.InvariantCulture), y[yIndex].ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase)) != 0)
                    {
                        return difference;
                    }

                    xIndex++;
                    yIndex++;
                }
            }

            if (yIndex < y.Length)
            {
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Convert standard Base64 to URL-safe Base64
        /// </summary>
        public string Base64ToUrlSafe()
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            return str.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        /// <summary>
        /// Convert URL-safe Base64 to standard Base64
        /// </summary>
        public string Base64FromUrlSafe()
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            str = str.Replace('-', '+').Replace('_', '/');

            switch (str.Length % 4)
            {
                case 2: str += "=="; break;
                case 3: str += "="; break;
            }

            return str;
        }
    }
}