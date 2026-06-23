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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Studio.Core.SMS;

[Scope]
public class SmsManager(UserManager userManager,
    SecurityContext securityContext,
    TenantManager tenantManager,
    SmsKeyStorage smsKeyStorage,
    SmsSender smsSender,
    StudioSmsNotificationSettingsHelper studioSmsNotificationSettingsHelper,
    CookiesManager cookieManager)
{
    public async ValueTask<string> SaveMobilePhoneAsync(UserInfo user, string mobilePhone)
    {
        mobilePhone = SmsSender.GetPhoneValueDigits(mobilePhone);

        if (user == null || Equals(user, Constants.LostUser))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        if (string.IsNullOrEmpty(mobilePhone))
        {
            throw new Exception(Resource.ActivateMobilePhoneEmptyPhoneNumber);
        }

        if (!string.IsNullOrEmpty(user.MobilePhone) && user.MobilePhoneActivationStatus == MobilePhoneActivationStatus.Activated)
        {
            throw new Exception(Resource.MobilePhoneMustErase);
        }

        user.MobilePhone = mobilePhone;
        user.MobilePhoneActivationStatus = MobilePhoneActivationStatus.NotActivated;
        if (securityContext.IsAuthenticated)
        {
            await userManager.UpdateUserInfoWithSyncCardDavAsync(user);
        }
        else
        {
            try
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);
                await userManager.UpdateUserInfoWithSyncCardDavAsync(user);
            }
            finally
            {
                securityContext.Logout();
            }
        }

        if (await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            await PutAuthCodeAsync(user, false);
        }

        return mobilePhone;
    }

    public async ValueTask PutAuthCodeAsync(UserInfo user, bool again)
    {
        if (user == null || Equals(user, Constants.LostUser))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        if (!await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync() || !await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            throw new MethodAccessException();
        }

        var mobilePhone = SmsSender.GetPhoneValueDigits(user.MobilePhone);

        if (await smsKeyStorage.ExistsKeyAsync(mobilePhone) && !again)
        {
            return;
        }
        var (succ, key) = await smsKeyStorage.GenerateKeyAsync(mobilePhone);
        if (!succ)
        {
            throw new Exception(Resource.SmsTooMuchError);
        }

        if (await smsSender.SendSMSAsync(mobilePhone, string.Format(Resource.SmsAuthenticationMessageToUser, key)))
        {
            await tenantManager.SetTenantQuotaRowAsync(new TenantQuotaRow { TenantId = tenantManager.GetCurrentTenantId(), Path = "/sms", Counter = 1, LastModified = DateTime.UtcNow }, true);
        }
    }

    public async Task<(bool, string)> ValidateSmsCodeAsync(UserInfo user, string code, bool isEntryPoint = false, bool session = false)
    {
        if (!await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync() || 
            !await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            return (false, null);
        }

        if (user == null || Equals(user, Constants.LostUser))
        {
            throw new ItemNotFoundException(Resource.ErrorUserNotFound);
        }

        var valid = await smsKeyStorage.ValidateKeyAsync(user.MobilePhone, code);
        switch (valid)
        {
            case SmsKeyStorage.Result.Empty:
                throw new Exception(Resource.ActivateMobilePhoneEmptyCode);
            case SmsKeyStorage.Result.TooMuch:
                throw new BruteForceCredentialException(Resource.SmsTooMuchError);
            case SmsKeyStorage.Result.Timeout:
                throw new TimeoutException(Resource.SmsAuthenticationTimeout);
            case SmsKeyStorage.Result.Invalide:
                throw new ArgumentException(Resource.SmsAuthenticationMessageError);
        }
        if (valid != SmsKeyStorage.Result.Ok)
        {
            throw new Exception("Error: " + valid);
        }

        string token = null;
        if (!securityContext.IsAuthenticated)
        {
            var action = isEntryPoint ? MessageAction.LoginSuccessViaApiSms : MessageAction.LoginSuccessViaSms;
            token = await cookieManager.AuthenticateMeAndSetCookiesAsync(user.Id, action, session);
        }

        if (user.MobilePhoneActivationStatus == MobilePhoneActivationStatus.NotActivated)
        {
            user.MobilePhoneActivationStatus = MobilePhoneActivationStatus.Activated;
            await userManager.UpdateUserInfoAsync(user);
        }

        return (true, token);
    }
}