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

namespace ASC.Web.Core.Sms;

[Scope]
public class SmsSender(IConfiguration configuration,
    TenantManager tenantManager,
    ILogger<SmsSender> logger,
    SmsProviderManager smsProviderManager)
{
    public async Task<bool> SendSMSAsync(string number, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(number);
        ArgumentException.ThrowIfNullOrEmpty(message);

        if (!smsProviderManager.Enabled())
        {
            throw new MethodAccessException();
        }

        if ("log".Equals(configuration["core:notify:postman"], StringComparison.InvariantCultureIgnoreCase))
        {
            var tenant = tenantManager.GetCurrentTenant(false);
            var tenantId = tenant?.Id ?? Tenant.DefaultTenant;

            logger.InformationSendSmsToPhoneNumber(tenantId, number, message);
            return false;
        }

        number = new Regex("[^\\d+]").Replace(number, string.Empty);
        return await smsProviderManager.SendMessageAsync(number, message);
    }

    public static string GetPhoneValueDigits(string mobilePhone)
    {
        var reg = new Regex(@"[^\d]");
        mobilePhone = reg.Replace(mobilePhone ?? "", string.Empty).Trim();
        return mobilePhone[..Math.Min(64, mobilePhone.Length)];
    }

    public static string BuildPhoneNoise(string mobilePhone)
    {
        if (string.IsNullOrEmpty(mobilePhone))
        {
            return string.Empty;
        }

        mobilePhone = GetPhoneValueDigits(mobilePhone);

        const int startLen = 4;
        const int endLen = 4;
        if (mobilePhone.Length < startLen + endLen)
        {
            return mobilePhone;
        }

        var sb = new StringBuilder();
        sb.Append('+');
        sb.Append(mobilePhone, 0, startLen);
        for (var i = startLen; i < mobilePhone.Length - endLen; i++)
        {
            sb.Append('*');
        }
        sb.Append(mobilePhone, mobilePhone.Length - endLen, mobilePhone.Length - (endLen + 1));
        return sb.ToString();
    }
}