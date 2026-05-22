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

namespace ASC.Web.Studio.Core.Notify;

public static class TagValues
{
    public static ITagValue WithoutUnsubscribe()
    {
        return new TagValue(CommonTags.WithoutUnsubscribe, true);
    }

    public static ITagValue OrangeButton(string btnText, string btnUrl, string tag = null)
    {
        var sb = new StringBuilder();

        sb.Append(@"<table cellspacing=""0"" cellpadding=""0"" style=""border: 0 none; border-collapse: collapse; border-spacing: 0; empty-cells: show; margin: 0 auto; max-width: 600px; padding: 0; vertical-align: top; width: 100%; text-align: left;"">");
        sb.Append("<tbody>");
        sb.Append(@"<tr border=""0"" cellspacing=""0"" cellpadding=""0"">");
        sb.Append(@"<td style=""width: 180px;""></td>");
        sb.Append("<!--[if mso]>");
        sb.Append(@"<td class=""body-text"" border=""0"" style=""margin: 0; padding: 0; text-align: center; width: 230px; white-space: nowrap;"">");
        sb.Append($@"<v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href=""{btnUrl}"" style=""v-text-anchor: middle; box-sizing: border-box; font-weight: 600; font-size: 12px; height: 56px; text-decoration: none; width: 230px;"" arcsize=""5%"" strokecolor=""#FF6F3D"" fillcolor=""#FF6F3D"" alt=""{btnText}"" target=""_blank"">");
        sb.Append("<w:anchorlock/>");
        sb.Append($@"<center class=""fol"" style=""color:#ffffff; font-family: 'Open Sans', Helvetica, Arial, Tahoma, sans-serif; font-weight: 600; font-size: 12px; letter-spacing: 0.04em; text-align: center; text-decoration: none; text-transform: uppercase; white-space: nowrap;"">{btnText}</center>");
        sb.Append("</v:roundrect>");
        sb.Append("</td>");
        sb.Append("<![endif]-->");
        sb.Append("<td style=\"text-align: center; white-space: nowrap;\">");
        sb.Append($@"<a class=""fol"" href=""{btnUrl}"" style=""background-color:#FF6F3D; border:1px solid #FF6F3D; border-radius: 3px; color:#ffffff; display: inline-block; font-family: 'Open Sans', Helvetica, Arial, Tahoma, sans-serif; font-size: 12px; font-weight: 600; padding-top: 15px; padding-right: 65px; padding-bottom: 15px; padding-left: 65px; text-align: center; text-decoration: none; text-transform: uppercase; -webkit-text-size-adjust: none; mso-hide: all; white-space: nowrap; letter-spacing: 0.04em;"" alt=""{btnText}"" target=""_blank"">{btnText}</a>");
        sb.Append("</td>");
        sb.Append(@"<td style=""width: 180px;""></td>");
        sb.Append("</tr>");
        sb.Append("</tbody>");
        sb.Append("</table>");

        return string.IsNullOrEmpty(tag)
            ? new TagValue("OrangeButton", sb.ToString())
            : new TagValue(tag, sb.ToString());
    }

    public static ITagValue TrulyYours(StudioNotifyHelper studioNotifyHelper, string text, bool asTableRow = false)
    {
        var sb = new StringBuilder();
        var url = studioNotifyHelper.SiteLink;
        var urlText = new Uri(url).Host;

        if (asTableRow)
        {
            sb.Append(@"<tr border=""0"" cellspacing=""0"" cellpadding=""0"">");
            sb.Append(@"<td class=""fol"" style=""color: #333333; font-family: 'Open Sans', Helvetica, Arial, Tahoma, sans-serif; font-size: 13px; line-height: 1.6em; Margin: 0; padding: 0px 40px 32px; vertical-align: top; text-align: center;"">");
        }
        else
        {
            sb.Append(@"<p style=""font-size: 14px;line-height: 21px;margin: 20px 0 32px;word-wrap: break-word !important;"">");
        }

        sb.Append($@"{text} <br />");
        sb.Append($@"<a style=""color: #FF6F3D; text-decoration: none;"" target=""_blank"" href=""{url}"">{urlText}</a>");

        if (asTableRow)
        {
            sb.Append("</td>");
            sb.Append("</tr>");
        }
        else
        {
            sb.Append("</p>");
        }

        return new TagValue("TrulyYours", sb.ToString());
    }

    public static ITagValue TableTop()
    {
        return new TagValue("TableItemsTop",
                            "<table cellpadding=\"0\" cellspacing=\"0\" style=\"margin: 20px 0 0; border-spacing: 0; empty-cells: show; width: 520px; font-size: 18px;\">");
    }

    public static ITagValue TableBottom()
    {
        return new TagValue("TableItemsBtm", "</table>");
    }

    public static ITagValue Image(StudioNotifyHelper studioNotifyHelper, int id, string imageFileName)
    {
        var imgSrc = studioNotifyHelper.GetNotificationImageUrl(imageFileName);

        var imgHtml = $"<img style=\"border: 0; padding: 0; width: auto; height: auto;\" alt=\"\" src=\"{imgSrc}\"/>";

        var tagName = "Image" + (id > 0 ? id.ToString() : string.Empty);

        return new TagValue(tagName, imgHtml);
    }
}
