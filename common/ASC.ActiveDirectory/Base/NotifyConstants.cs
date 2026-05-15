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

using ASC.Web.Core.Utility;

namespace ASC.ActiveDirectory.Base;

public class NotifyConstants
{
    public static readonly string TagUserName = "UserName";
    public static readonly string TagUserEmail = "UserEmail";
    public static readonly string TagMyStaffLink = "MyStaffLink";


    public static ITagValue TagOrangeButton(string btnText, string btnUrl)
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

        return new TagValue("OrangeButton", sb.ToString());
    }
}

public static class NotifyCommonTags
{
    public static string Footer = "Footer";

    public static string MasterTemplate = "MasterTemplate";

    public static readonly string WithoutUnsubscribe = "WithoutUnsubscribe";
}

[Scope]
public class LdapActivationNotifyAction(CommonLinkUtility commonLinkUtility, DisplayUserSettingsHelper displayUserSettingsHelper, IUrlShortener urlShortener, TenantManager manager) : NotifyAction(manager)
{
    public override string ID  => "user_ldap_activation";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_ldap_activation, () => WebstudioNotifyPatternResource.pattern_user_ldap_activation)
    ];

    public async Task Init(UserInfo ldapUserInfo, LdapLocalization resource)
    {
        var confirmLink = commonLinkUtility.GetConfirmationEmailUrl(ldapUserInfo.Email, ConfirmType.EmailActivation, null, ldapUserInfo.Id);
        Tags =
        [
            new TagValue(NotifyConstants.TagUserName, ldapUserInfo.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(NotifyConstants.TagUserEmail, ldapUserInfo.Email),
            new TagValue(NotifyConstants.TagMyStaffLink, commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff())),
            NotifyConstants.TagOrangeButton(resource.NotifyButtonJoin, await urlShortener.GetShortenLinkAsync(confirmLink)),
            new TagValue(NotifyCommonTags.WithoutUnsubscribe, true)
        ];
    }
}
