// (c) Copyright Ascensio System SIA 2010-2023
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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Studio.Core.TFA;

public class BackupCode
{
    public bool IsUsed { get; set; }

    public string Code { get; set; }

    public string GetEncryptedCode(InstanceCrypto InstanceCrypto, Signature Signature)
    {
        try
        {
            return InstanceCrypto.Decrypt(Code);
        }
        catch
        {
            //support old scheme stored in the DB
            return Signature.Read<string>(Code);
        }
    }

    public void SetEncryptedCode(InstanceCrypto InstanceCrypto, string code)
    {
        Code = InstanceCrypto.Encrypt(code);
    }
}

[Scope]
public class TfaManager(SettingsManager settingsManager,
    SecurityContext securityContext,
    CookiesManager cookiesManager,
    SetupInfo setupInfo,
    Signature signature,
    InstanceCrypto instanceCrypto,
    MachinePseudoKeys machinePseudoKeys,
    ICache cache,
    TfaAppAuthSettingsHelper tfaAppAuthSettingsHelper)
{
    private static readonly TwoFactorAuthenticator _tfa = new();
    private ICache Cache { get; set; } = cache;

    public async Task<SetupCode> GenerateSetupCodeAsync(UserInfo user)
    {
        return _tfa.GenerateSetupCode(setupInfo.TfaAppSender, user.Email, await GenerateAccessTokenAsync(user), false, 4);
    }

    public async Task<bool> ValidateAuthCodeAsync(UserInfo user, string code, bool checkBackup = true, bool isEntryPoint = false)
    {
        if (!tfaAppAuthSettingsHelper.IsVisibleSettings
            || !(await settingsManager.LoadAsync<TfaAppAuthSettings>()).EnableSetting)
        {
            return false;
        }

        if (user == null || Equals(user, Constants.LostUser))
        {
            throw new Exception(Resource.ErrorUserNotFound);
        }

        code = (code ?? "").Trim();

        if (string.IsNullOrEmpty(code))
        {
            throw new Exception(Resource.ActivateTfaAppEmptyCode);
        }

        int.TryParse(Cache.Get<string>("tfa/" + user.Id), out var counter);

        var loginSettings = await settingsManager.LoadAsync<LoginSettings>();
        var attemptsCount = loginSettings.AttemptCount;

        if (++counter > attemptsCount)
        {
            throw new BruteForceCredentialException(Resource.TfaTooMuchError);
        }
        Cache.Insert("tfa/" + user.Id, counter.ToString(CultureInfo.InvariantCulture), DateTime.UtcNow.Add(TimeSpan.FromMinutes(1)));

        if (!_tfa.ValidateTwoFactorPIN(await GenerateAccessTokenAsync(user), code))
        {
            if (checkBackup && (await TfaAppUserSettings.BackupCodesForUserAsync(settingsManager, user.Id)).Any(x => x.GetEncryptedCode(instanceCrypto, signature) == code && !x.IsUsed))
            {
                await TfaAppUserSettings.DisableCodeForUserAsync(settingsManager, instanceCrypto, signature, user.Id, code);
            }
            else
            {
                throw new ArgumentException(Resource.TfaAppAuthMessageError);
            }
        }

        Cache.Insert("tfa/" + user.Id, (counter - 1).ToString(CultureInfo.InvariantCulture), DateTime.UtcNow.Add(TimeSpan.FromMinutes(1)));

        if (!securityContext.IsAuthenticated)
        {
            var action = isEntryPoint ? MessageAction.LoginSuccessViaApiTfa : MessageAction.LoginSuccesViaTfaApp;
            await cookiesManager.AuthenticateMeAndSetCookiesAsync(user.Id, action);
        }

        if (!await TfaAppUserSettings.EnableForUserAsync(settingsManager, user.Id))
        {
            await GenerateBackupCodesAsync();
            return true;
        }

        return false;
    }

    public async Task<IEnumerable<BackupCode>> GenerateBackupCodesAsync()
    {
        var count = setupInfo.TfaAppBackupCodeCount;
        var length = setupInfo.TfaAppBackupCodeLength;

        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_";

        var list = new List<BackupCode>();

        for (var i = 0; i < count; i++)
        {
            var data = RandomNumberGenerator.GetBytes(length);

            var result = new StringBuilder(length);
            foreach (var b in data)
            {
                result.Append(alphabet[b % alphabet.Length]);
            }

            var code = new BackupCode();
            code.SetEncryptedCode(instanceCrypto, result.ToString());
            list.Add(code);
        }
        var settings = await settingsManager.LoadForCurrentUserAsync<TfaAppUserSettings>();
        settings.CodesSetting = list;
        await settingsManager.SaveForCurrentUserAsync(settings);

        return list;
    }

    private async Task<string> GenerateAccessTokenAsync(UserInfo user)
    {
        var userSalt = await TfaAppUserSettings.GetSaltAsync(settingsManager, user.Id);

        //from Signature.Create
        var machineSalt = Encoding.UTF8.GetString(machinePseudoKeys.GetMachineConstant());
        var token = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(userSalt + machineSalt)));
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        return encodedToken[..10];
    }
}
