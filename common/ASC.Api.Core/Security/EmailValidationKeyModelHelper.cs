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

using static ASC.Security.Cryptography.EmailValidationKeyProvider;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Api.Core.Security;

[Transient]
public class EmailValidationKeyModelHelper(IHttpContextAccessor httpContextAccessor,
    EmailValidationKeyProvider provider,
    AuthContext authContext,
    UserManager userManager,
    AuthManager authentication,
    InvitationValidator invitationValidator,
    AuditEventsRepository auditEventsRepository,
    TenantUtil tenantUtil,
    CookiesManager cookiesManager,
    SecurityContext securityContext)
{
    public EmailValidationKeyModel GetModel()
    {
        var request = QueryHelpers.ParseQuery(httpContextAccessor.HttpContext.Request.Headers["confirm"]);

        var type = request.TryGetValue("type", out var value) ? value.FirstOrDefault() : null;

        ConfirmType? cType = null;
        if (ConfirmTypeExtensions.TryParse(type, out var confirmType))
        {
            cType = confirmType;
        }
        
        if (!request.TryGetValue("key", out var key))
        {
            key = httpContextAccessor.HttpContext.Request.Cookies[cookiesManager.GetConfirmCookiesName() + $"_{type}"];
        }

        request.TryGetValue("emplType", out var emplType);
        EmployeeTypeExtensions.TryParse(emplType, out var employeeType);

        request.TryGetValue("email", out var _email);

        request.TryGetValue("uid", out var userIdKey);
        Guid.TryParse(userIdKey, out var userId);

        request.TryGetValue("first", out var first);

        return new EmailValidationKeyModel
        {
            Email = _email,
            EmplType = employeeType,
            Key = key,
            Type = cType,
            UiD = userId,
            First = first
        };
    }

    public async Task<ValidationResult> ValidateAsync(EmailValidationKeyModel inDto)
    {
        var (key, emplType, email, uiD, type, first) = inDto;

        ValidationResult checkKeyResult;

        switch (type)
        {
            case ConfirmType.EmpInvite:
                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type + (int)emplType, key, provider.ValidEmailKeyInterval);
                break;

            case ConfirmType.LinkInvite:
                checkKeyResult = (await invitationValidator.ValidateAsync(key, email, emplType ?? default)).Status;
                break;

            case ConfirmType.PortalOwnerChange:                
                var newOwner = await userManager.GetUsersAsync(uiD.GetValueOrDefault());
                if(Equals(newOwner, Constants.LostUser) || newOwner.Status == EmployeeStatus.Terminated)
                {
                    checkKeyResult = ValidationResult.Invalid;
                    break;
                }
                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type + uiD.GetValueOrDefault(), key, provider.ValidEmailKeyInterval);
                break;

            case ConfirmType.EmailChange:
                var userId = uiD.GetValueOrDefault();
                if (authContext.CurrentAccount.ID != userId)
                {
                    checkKeyResult = ValidationResult.Invalid;
                    break;
                }
                var emailChangeEvent = (await auditEventsRepository.GetByFilterAsync(action: MessageAction.UserSentEmailChangeInstructions, entry: EntryType.User, target: MessageTarget.Create(userId).ToString(), limit: 1)).FirstOrDefault();
                var postfix = emailChangeEvent == null ? userId.ToString() : tenantUtil.DateTimeToUtc(emailChangeEvent.Date).ToString("s", CultureInfo.InvariantCulture);

                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type + postfix, key, provider.ValidEmailKeyInterval);
                break;
            case ConfirmType.PasswordChange:
                var userInfo = await userManager.GetUserByEmailAsync(email);
                if(Equals(userInfo, Constants.LostUser) || userInfo.Id != uiD || userInfo.Status == EmployeeStatus.Terminated)
                {
                    checkKeyResult = ValidationResult.Invalid;
                    break;
                }
                var auditEvent = (await auditEventsRepository.GetByFilterAsync(action: MessageAction.UserSentPasswordChangeInstructions, entry: EntryType.User, target: MessageTarget.Create(userInfo.Id).ToString(), limit: 1)).FirstOrDefault();
                var passwordStamp = await authentication.GetUserPasswordStampAsync(userInfo.Id);

                string hash;

                if (auditEvent != null)
                {
                    var auditEventDate = tenantUtil.DateTimeToUtc(auditEvent.Date);

                    hash = (auditEventDate.CompareTo(passwordStamp) > 0 ? auditEventDate : passwordStamp).ToString("s", CultureInfo.InvariantCulture);
                }
                else
                {
                    hash = passwordStamp.ToString("s", CultureInfo.InvariantCulture);
                }

                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type + hash, key, provider.ValidEmailKeyInterval);
                
                if (checkKeyResult is ValidationResult.Ok && userInfo.ActivationStatus is not EmployeeActivationStatus.Activated)
                {
                    await securityContext.AuthenticateMeWithoutCookieAsync(userInfo.Id);
                    
                    userInfo.ActivationStatus = EmployeeActivationStatus.Activated;
                    await userManager.UpdateUserInfoAsync(userInfo);
                }
                break;

            case ConfirmType.Activation:
                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type + uiD, key, provider.ValidEmailKeyInterval);
                break;

            case ConfirmType.ProfileRemove:
                // validate UiD
                var user = await userManager.GetUsersAsync(uiD.GetValueOrDefault());
                if (user == null || Equals(user, Constants.LostUser) || user.Status == EmployeeStatus.Terminated || authContext.IsAuthenticated && authContext.CurrentAccount.ID != uiD)
                {
                    return ValidationResult.Invalid;
                }

                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type + uiD, key, provider.ValidEmailKeyInterval);
                break;

            case ConfirmType.Wizard:
                checkKeyResult = await provider.ValidateEmailKeyAsync("" + type, key, provider.ValidEmailKeyInterval);
                break;

            case ConfirmType.PhoneActivation:
            case ConfirmType.PhoneAuth:
            case ConfirmType.TfaActivation:
            case ConfirmType.TfaAuth:
            case ConfirmType.Auth:
                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type + first, key, provider.ValidAuthKeyInterval);
                break;

            case ConfirmType.PortalContinue:
                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type, key);
                break;

            default:
                checkKeyResult = await provider.ValidateEmailKeyAsync(email + type, key, provider.ValidEmailKeyInterval);
                break;
        }

        return checkKeyResult;
    }
}