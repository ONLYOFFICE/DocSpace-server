﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Api.Core.Security;

[Scope]
public class InvitationValidator(
    IHttpContextAccessor httpContextAccessor,
    MessageService messageService,
    Signature signature,
    IDbContextFactory<MessagesContext> dbContextFactory,
    EmailValidationKeyProvider emailValidationKeyProvider,
    UserManager userManager,
    AuthManager authManager)
{
    public TimeSpan IndividualLinkExpirationInterval => emailValidationKeyProvider.ValidEmailKeyInterval;

    public string MakeIndividualLinkKey(Guid linkId)
    {
        return signature.Create(linkId);
    }

    public async Task<LinkValidationResult> ValidateAsync(string key, string email, EmployeeType employeeType)
    {
        var result = new LinkValidationResult { Status = EmailValidationKeyProvider.ValidationResult.Invalid };

        var (commonWithRoomLinkResult, linkId) = ValidateCommonWithRoomLink(key);

        if (commonWithRoomLinkResult != EmailValidationKeyProvider.ValidationResult.Invalid)
        {
            result.Status = commonWithRoomLinkResult;
            result.LinkType = InvitationLinkType.CommonToRoom;
            result.LinkId = linkId;

            return result;
        }

        var commonLinkResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(ConfirmType.LinkInvite.ToStringFast() + (int)employeeType,
            key, emailValidationKeyProvider.ValidEmailKeyInterval);

        if (commonLinkResult != EmailValidationKeyProvider.ValidationResult.Invalid)
        {
            result.Status = commonLinkResult;
            result.LinkType = InvitationLinkType.Common;
            result.ConfirmType = ConfirmType.LinkInvite;

            return result;
        }

        commonLinkResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(email + ConfirmType.EmpInvite.ToStringFast() + (int)employeeType,
            key, emailValidationKeyProvider.ValidEmailKeyInterval);

        if (commonLinkResult != EmailValidationKeyProvider.ValidationResult.Invalid)
        {
            result.Status = commonLinkResult;
            result.LinkType = InvitationLinkType.Common;
            result.ConfirmType = ConfirmType.EmpInvite;

            return result;
        }

        if (string.IsNullOrEmpty(email))
        {
            return result;
        }

        var (status, user) = await ValidateIndividualLinkAsync(email, key, employeeType);

        result.Status = status;
        result.LinkType = InvitationLinkType.Individual;
        result.ConfirmType = ConfirmType.LinkInvite;
        result.User = user;

        return result;
    }

    private async Task<(EmailValidationKeyProvider.ValidationResult, UserInfo)> ValidateIndividualLinkAsync(string email, string key, EmployeeType employeeType)
    {
        var result = await emailValidationKeyProvider.ValidateEmailKeyAsync(email + ConfirmType.LinkInvite.ToStringFast() + employeeType.ToStringFast(),
            key, IndividualLinkExpirationInterval);

        if (result != EmailValidationKeyProvider.ValidationResult.Ok)
        {
            return (result, null);
        }

        var user = await userManager.GetUserByEmailAsync(email);
        if (user.Equals(Constants.LostUser) || user.Status == EmployeeStatus.Terminated)
        {
            return (EmailValidationKeyProvider.ValidationResult.Invalid, null);
        }

        if (await authManager.GetUserPasswordStampAsync(user.Id) != DateTime.MinValue)
        {
            return (EmailValidationKeyProvider.ValidationResult.UserExisted, user);
        }

        var visitMessage = await GetLinkVisitMessageAsync(user.TenantId, email, key);
        if (visitMessage == null)
        {
            await SaveLinkVisitMessageAsync(email, key);
        }
        else if (visitMessage.Date + emailValidationKeyProvider.ValidVisitLinkInterval < DateTime.UtcNow)
        {
            return (EmailValidationKeyProvider.ValidationResult.Expired, null);
        }

        return (result, user);
    }

    private (EmailValidationKeyProvider.ValidationResult, Guid) ValidateCommonWithRoomLink(string key)
    {
        var linkId = signature.Read<Guid>(key);

        return linkId == default ? (EmailValidationKeyProvider.ValidationResult.Invalid, default) : (EmailValidationKeyProvider.ValidationResult.Ok, linkId);
    }

    private async Task<DbAuditEvent> GetLinkVisitMessageAsync(int tenantId, string email, string key)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var target = MessageTarget.Create(email);
        var description = JsonSerializer.Serialize(new[] { key });

        var message = await Queries.AuditEventsAsync(context, tenantId, target.ToString(), description);

        return message;
    }

    private async Task SaveLinkVisitMessageAsync(string email, string key)
    {
        var headers = httpContextAccessor?.HttpContext?.Request.Headers;
        var target = MessageTarget.Create(email);

        await messageService.SendHeadersMessageAsync(MessageAction.RoomInviteLinkUsed, target, headers, key);
    }
}

public enum InvitationLinkType
{
    Common,
    CommonToRoom,
    Individual
}

public class LinkValidationResult
{
    public EmailValidationKeyProvider.ValidationResult Status { get; set; }
    public ConfirmType? ConfirmType { get; set; }
    public InvitationLinkType LinkType { get; set; }
    public Guid LinkId { get; set; }
    public UserInfo User { get; set; }
}

static file class Queries
{
    public static readonly Func<MessagesContext, int, string, string, Task<DbAuditEvent>> AuditEventsAsync =
        EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, string target, string description) =>
                ctx.AuditEvents.FirstOrDefault(a => 
                    a.TenantId == tenantId && a.Action == (int)MessageAction.RoomInviteLinkUsed && a.Target == target && a.DescriptionRaw == description));
}