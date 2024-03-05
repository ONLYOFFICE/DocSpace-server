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

namespace ASC.Api.Core.Security;

[Scope]
public class InvitationLinkHelper(IHttpContextAccessor httpContextAccessor,
    MessageTarget messageTarget,
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
        var validationResult = new LinkValidationResult { Result = EmailValidationKeyProvider.ValidationResult.Invalid };

        var (commonWithRoomLinkResult, linkId) = ValidateCommonWithRoomLink(key);

        if (commonWithRoomLinkResult != EmailValidationKeyProvider.ValidationResult.Invalid)
        {
            validationResult.Result = commonWithRoomLinkResult;
            validationResult.LinkType = InvitationLinkType.CommonWithRoom;
            validationResult.LinkId = linkId;

            return validationResult;
        }

        var commonLinkResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(ConfirmType.LinkInvite.ToStringFast() + (int)employeeType,
            key, emailValidationKeyProvider.ValidEmailKeyInterval);

        if (commonLinkResult != EmailValidationKeyProvider.ValidationResult.Invalid)
        {
            validationResult.Result = commonLinkResult;
            validationResult.LinkType = InvitationLinkType.Common;
            validationResult.ConfirmType = ConfirmType.LinkInvite;

            return validationResult;
        }

        commonLinkResult = await emailValidationKeyProvider.ValidateEmailKeyAsync(email + ConfirmType.EmpInvite.ToStringFast() + (int)employeeType,
            key, emailValidationKeyProvider.ValidEmailKeyInterval);

        if (commonLinkResult != EmailValidationKeyProvider.ValidationResult.Invalid)
        {
            validationResult.Result = commonLinkResult;
            validationResult.LinkType = InvitationLinkType.Common;
            validationResult.ConfirmType = ConfirmType.EmpInvite;

            return validationResult;
        }

        if (string.IsNullOrEmpty(email))
        {
            return validationResult;
        }

        var individualLinkResult = await ValidateIndividualLinkAsync(email, key, employeeType);

        validationResult.Result = individualLinkResult;
        validationResult.LinkType = InvitationLinkType.Individual;
        validationResult.ConfirmType = ConfirmType.LinkInvite;

        return validationResult;
    }

    private async Task<EmailValidationKeyProvider.ValidationResult> ValidateIndividualLinkAsync(string email, string key, EmployeeType employeeType)
    {
        var result = await emailValidationKeyProvider.ValidateEmailKeyAsync(email + ConfirmType.LinkInvite.ToStringFast() + employeeType.ToStringFast(),
            key, IndividualLinkExpirationInterval);

        if (result != EmailValidationKeyProvider.ValidationResult.Ok)
        {
            return result;
        }

        var user = await userManager.GetUserByEmailAsync(email);

        if (user.Equals(Constants.LostUser) || await authManager.GetUserPasswordStampAsync(user.Id) != DateTime.MinValue)
        {
            return EmailValidationKeyProvider.ValidationResult.Invalid;
        }

        var visitMessage = await GetLinkVisitMessageAsync(user.TenantId, email, key);

        if (visitMessage == null)
        {
            await SaveLinkVisitMessageAsync(email, key);
        }
        else if (visitMessage.Date + emailValidationKeyProvider.ValidVisitLinkInterval < DateTime.UtcNow)
        {
            return EmailValidationKeyProvider.ValidationResult.Expired;
        }

        return result;
    }

    private (EmailValidationKeyProvider.ValidationResult, Guid) ValidateCommonWithRoomLink(string key)
    {
        var linkId = signature.Read<Guid>(key);

        return linkId == default ? (EmailValidationKeyProvider.ValidationResult.Invalid, default) : (EmailValidationKeyProvider.ValidationResult.Ok, linkId);
    }

    private async Task<DbAuditEvent> GetLinkVisitMessageAsync(int tenantId, string email, string key)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var target = messageTarget.Create(email);
        var description = JsonSerializer.Serialize(new[] { key });

        var message = await Queries.AuditEventsAsync(context, tenantId, target.ToString(), description);

        return message;
    }

    private async Task SaveLinkVisitMessageAsync(string email, string key)
    {
        var headers = httpContextAccessor?.HttpContext?.Request.Headers;
        var target = messageTarget.Create(email);

        await messageService.SendHeadersMessageAsync(MessageAction.RoomInviteLinkUsed, target, headers, key);
    }
}

public enum InvitationLinkType
{
    Common,
    CommonWithRoom,
    Individual
}

public class LinkValidationResult
{
    public EmailValidationKeyProvider.ValidationResult Result { get; set; }
    public ConfirmType? ConfirmType { get; set; }
    public InvitationLinkType LinkType { get; set; }
    public Guid LinkId { get; set; }
}

static file class Queries
{
    public static readonly Func<MessagesContext, int, string, string, Task<DbAuditEvent>> AuditEventsAsync =
        EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, string target, string description) =>
                ctx.AuditEvents.FirstOrDefault(a => 
                    a.TenantId == tenantId && a.Action == (int)MessageAction.RoomInviteLinkUsed && a.Target == target && a.DescriptionRaw == description));
}