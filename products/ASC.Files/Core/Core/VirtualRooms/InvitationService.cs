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

using ASC.Core.Billing;
using ASC.MessagingSystem.EF.Context;

namespace ASC.Files.Core.VirtualRooms;

[Scope]
public class InvitationService(
    CommonLinkUtility commonLinkUtility, 
    IDaoFactory daoFactory, 
    InvitationValidator invitationValidator, 
    ITariffService tariffService, 
    TenantManager tenantManager, 
    CountPaidUserChecker countPaidUserChecker, 
    FileSecurity fileSecurity, 
    UserManager userManager,
    IPSecurity.IPSecurity iPSecurity,
    AuthContext authContext,
    IDbContextFactory<MessagesContext> dbContextFactory,
    FilesMessageService filesMessageService,
    DisplayUserSettingsHelper displayUserSettingsHelper)
{
    public string GetInvitationLink(Guid linkId, Guid createdBy)
    {
        var key = invitationValidator.MakeIndividualLinkKey(linkId, createdBy);
        return commonLinkUtility.GetConfirmationUrl(key, ConfirmType.LinkInvite, createdBy);
    }

    public async Task<string> GetInvitationLinkAsync(string email, FileShare share, Guid createdBy, string roomId, string culture = null)
    {
        var type = FileSecurity.GetTypeByShare(share);
        var link = await commonLinkUtility.GetInvitationLinkAsync(email, type, createdBy, culture) + $"&roomId={roomId}";
        return link;
    }
    
    public async Task<Validation> ConfirmAsync(string key, string email, EmployeeType employeeType, string roomId = null, Guid? userId = default)
    {
        if (!await iPSecurity.VerifyAsync())
        {
            throw new SecurityException();
        }

        var data = await GetLinkDataAsync(key, email, employeeType, userId);
        var validation = new Validation { Result = data.Result };
        
        if (data.Result is EmailValidationKeyProvider.ValidationResult.Invalid or EmailValidationKeyProvider.ValidationResult.Expired)
        {
            return validation;
        }
        
        var isAuth = authContext.IsAuthenticated;
        
        (validation.RoomId, validation.Title) = data.LinkType switch
        {
            InvitationLinkType.Individual when !string.IsNullOrEmpty(roomId) => await GetRoomDataAsync(roomId, async entry =>
            {
                return entry switch
                {
                    FileEntry<int> entryInt => await fileSecurity.CanReadAsync(entryInt, data.User.Id),
                    FileEntry<string> entryString => await fileSecurity.CanReadAsync(entryString, data.User.Id),
                    _ => false
                };
            }),
            InvitationLinkType.CommonToRoom when !isAuth => await GetRoomDataAsync(data.RoomId),
            InvitationLinkType.CommonToRoom => await GetRoomDataAsync(data.RoomId, async entry =>
            {
                return entry switch
                {
                    Folder<int> entryInt => await ResolveAccessAsync(entryInt),
                    Folder<string> entryString => await ResolveAccessAsync(entryString),
                    _ => false
                };

                async Task<bool> ResolveAccessAsync<T>(Folder<T> folder)
                {
                    if (await fileSecurity.CanReadAsync(folder))
                    {
                        return true;
                    }
                    
                    var tenantId = await tenantManager.GetCurrentTenantIdAsync();
                    await using var context = await dbContextFactory.CreateDbContextAsync();

                    var query = context.AuditEvents.Where(x => x.TenantId == tenantId && x.Action == (int)MessageAction.RoomRemoveUser);

                    if (entry.ProviderEntry)
                    {
                        var match = Selectors.Pattern.Match(data.RoomId);
                        if (!match.Success)
                        {
                            return false;
                        }
                        
                        query = query.Where(x => x.Target.StartsWith($"{match.Groups[2]}-{entry.ProviderId}-"));
                    }
                    else
                    {
                        query = query.Where(x => x.Target == data.RoomId);
                    }

                    var userId = authContext.CurrentAccount.ID;
                
                    await foreach(var auditEvent in query.ToAsyncEnumerable())
                    {
                        var description = JsonSerializer.Deserialize<List<string>>(auditEvent.DescriptionRaw);
                        var info = JsonSerializer.Deserialize<EventDescription<JsonElement>>(description.Last());

                        if (!info.UserIds.Contains(userId) || auditEvent.UserId == userId)
                        {
                            continue;
                        }

                        validation.Result = EmailValidationKeyProvider.ValidationResult.UserExcluded;
                        return false;
                    }
                    
                    if (FileSecurity.PaidShares.Contains(data.Share) && await userManager.GetUserTypeAsync(userId) is EmployeeType.User)
                    {
                        data.Share = FileSecurity.GetHighFreeRole(folder.FolderType);

                        if (data.Share == FileShare.None || 
                            !FileSecurity.AvailableRoomAccesses.TryGetValue(folder.FolderType, out var availableRoles) || 
                            !availableRoles.TryGetValue(SubjectType.InvitationLink, out var availableRolesBySubject) || 
                            !availableRolesBySubject.Contains(data.Share))
                        {
                            validation.Result = EmailValidationKeyProvider.ValidationResult.QuotaFailed;
                            return false;
                        }
                    }

                    var user = await userManager.GetUsersAsync(userId);
                    
                    await fileSecurity.ShareAsync(folder.Id, FileEntryType.Folder, userId, data.Share);

                    switch (entry)
                    {
                        case FileEntry<int> entryInt:
                            await filesMessageService.SendAsync(MessageAction.RoomCreateUser, entryInt, userId, data.Share, null, true, 
                                user.DisplayUserName(false, displayUserSettingsHelper));
                            break;
                        case FileEntry<string> entryString:
                            await filesMessageService.SendAsync(MessageAction.RoomCreateUser, entryString, userId, data.Share, null, true, 
                                user.DisplayUserName(false, displayUserSettingsHelper));
                            break;
                    }

                    return true;
                }
            }),
            _ => (null, null)
        };

        if (isAuth || data.Result is EmailValidationKeyProvider.ValidationResult.UserExisted)
        {
            if (validation.Result is not (EmailValidationKeyProvider.ValidationResult.UserExcluded or EmailValidationKeyProvider.ValidationResult.QuotaFailed))
            {
                validation.Result = EmailValidationKeyProvider.ValidationResult.UserExisted;
            }

            return validation;
        }
        
        validation.Result = await GetQuotaBasedResultAsync(data);

        if (validation.Result is EmailValidationKeyProvider.ValidationResult.Ok)
        {
            return validation;
        }

        validation.RoomId = null;
        validation.Title = null;

        return validation;
    }
    
    public async Task<InvitationLinkData> GetInvitationDataAsync(string key, string email, EmployeeType employeeType = EmployeeType.All)
    {
        var data = await GetLinkDataAsync(key, email, employeeType);

        data.Result = await GetQuotaBasedResultAsync(data);

        return data;
    }

    private async Task<InvitationLinkData> GetLinkDataAsync(string key, string email, EmployeeType employeeType = EmployeeType.All, Guid? userId = default)
    {
        var result = await invitationValidator.ValidateAsync(key, email, employeeType, userId);
        var data = new InvitationLinkData
        {
            Result = result.Status, 
            LinkType = result.LinkType, 
            ConfirmType = result.ConfirmType, 
            User = result.User,
            EmployeeType = employeeType,
        };

        if (result.LinkType is not InvitationLinkType.CommonToRoom)
        {
            return data;
        }

        var securityDao = daoFactory.GetSecurityDao<string>();
        var record = await securityDao.GetSharesAsync(new[] { result.LinkId })
            .FirstOrDefaultAsync(s => s.SubjectType == SubjectType.InvitationLink);
        
        if (record is not { SubjectType: SubjectType.InvitationLink })
        {
            data.Result = EmailValidationKeyProvider.ValidationResult.Invalid;
            return data;
        }

        data.Result = record.Options.ExpirationDate > DateTime.UtcNow 
            ? EmailValidationKeyProvider.ValidationResult.Ok 
            : EmailValidationKeyProvider.ValidationResult.Expired;

        data.Share = record.Share;
        data.RoomId = record.EntryId;
        data.EmployeeType = FileSecurity.GetTypeByShare(record.Share);

        return data;
    }

    private async Task<EmailValidationKeyProvider.ValidationResult> GetQuotaBasedResultAsync(InvitationLinkData data)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        
        // preferential rate does not allow invite users
        if ((await tariffService.GetTariffAsync(tenant.Id)).State > TariffState.Paid)
        {
            return EmailValidationKeyProvider.ValidationResult.Invalid;
        }

        if (data.LinkType is InvitationLinkType.Individual || data.EmployeeType is EmployeeType.User)
        {
            return data.Result;
        }
        
        try
        {
            await countPaidUserChecker.CheckAppend();
        }
        catch (TenantQuotaException)
        {
            return EmailValidationKeyProvider.ValidationResult.TariffLimit;
        }

        return data.Result;
    }

    private async Task<(string, string)> GetRoomDataAsync(string roomId, Func<FileEntry, Task<bool>> accessResolver = null)
    {
        if (int.TryParse(roomId, out var intId))
        {
            var internalRoom = await daoFactory.GetFolderDao<int>().GetFolderAsync(intId);
            if (!await CheckRoomAsync(internalRoom))
            {
                return (null, null);
            }
    
            return (internalRoom.Id.ToString(), internalRoom.Title);
        }

        var provider = await daoFactory.ProviderDao.GetProviderInfoByEntryIdAsync(roomId);
        if (provider == null || string.IsNullOrEmpty(provider.FolderId))
        {
            return (null, null);
        }

        var thirdPartyRoom = await daoFactory.GetFolderDao<string>().GetFolderAsync(provider.FolderId);
        if (!await CheckRoomAsync(thirdPartyRoom))
        {
            return (null, null);
        }
    
        return (thirdPartyRoom.Id, thirdPartyRoom.Title);

        async Task<bool> CheckRoomAsync<T>(Folder<T> room)
        {
            if (room == null || !DocSpaceHelper.IsRoom(room.FolderType))
            {
                return false;
            }

            if (accessResolver != null)
            {
                return await accessResolver(room);
            }

            return true;
        }
    }
}

public class Validation
{
    public EmailValidationKeyProvider.ValidationResult Result { get; set; }
    public string RoomId { get; set; }
    public string Title { get; set; }
}

public class InvitationLinkData
{
    public string RoomId { get; set; }
    public FileShare Share { get; set; }
    public InvitationLinkType LinkType { get; set; }
    public ConfirmType? ConfirmType { get; set; }
    public EmployeeType EmployeeType { get; set; }
    public EmailValidationKeyProvider.ValidationResult Result { get; set; }
    public UserInfo User { get; set; }
    public bool IsCorrect => Result == EmailValidationKeyProvider.ValidationResult.Ok;
}