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

namespace ASC.Files.Core.VirtualRooms;

[Scope]
public class InvitationLinkService(
    CommonLinkUtility commonLinkUtility, 
    IDaoFactory daoFactory, 
    InvitationLinkHelper invitationLinkHelper, 
    ITariffService tariffService, 
    TenantManager tenantManager, 
    CountPaidUserChecker countPaidUserChecker, 
    FileSecurity fileSecurity, 
    UserManager userManager,
    IPSecurity.IPSecurity iPSecurity)
{
    public string GetInvitationLink(Guid linkId, Guid createdBy)
    {
        var key = invitationLinkHelper.MakeIndividualLinkKey(linkId);

        return commonLinkUtility.GetConfirmationUrl(key, ConfirmType.LinkInvite, createdBy);
    }

    public async Task<string> GetInvitationLinkAsync(string email, FileShare share, Guid createdBy, string roomId, string culture = null)
    {
        var type = FileSecurity.GetTypeByShare(share);
        var link = await commonLinkUtility.GetInvitationLinkAsync(email, type, createdBy, culture) + $"&roomId={roomId}";
        return link;
    }

    public async Task<Validation> ValidateAsync(string key, string email, EmployeeType employeeType, string roomId = default)
    {
        if (!await iPSecurity.VerifyAsync())
        {
            throw new SecurityException();
        }

        var linkData = await GetProcessedLinkDataAsync(key, email, employeeType);
        var result = new Validation { Result = linkData.Result };

        if (!linkData.IsCorrect)
        {
            return result;
        }

        (string id, string title) data = linkData.LinkType switch
        {
            InvitationLinkType.Individual when !string.IsNullOrEmpty(roomId) => await GetRoomDataAsync(roomId, email),
            InvitationLinkType.CommonWithRoom => await GetRoomDataAsync(linkData.RoomId, null),
            _ => (null, null)
        };

        result.RoomId = data.id;
        result.Title = data.title;

        return result;
    }

    public async Task<InvitationLinkData> GetProcessedLinkDataAsync(string key, string email)
    {
        return await GetProcessedLinkDataAsync(key, email, EmployeeType.All);
    }

    public async Task<InvitationLinkData> GetProcessedLinkDataAsync(string key, string email, EmployeeType employeeType)
    {
        Tenant tenant;
        var linkData = new InvitationLinkData { Result = EmailValidationKeyProvider.ValidationResult.Invalid };

        try
        {
            tenant = await tenantManager.GetCurrentTenantAsync();
        }
        catch (Exception)
        {
            return linkData;
        }

        if ((await tariffService.GetTariffAsync(tenant.Id)).State > TariffState.Paid)
        {
            return linkData;
        }

        var validationResult = await invitationLinkHelper.ValidateAsync(key, email, employeeType);
        linkData.Result = validationResult.Result;
        linkData.LinkType = validationResult.LinkType;
        linkData.ConfirmType = validationResult.ConfirmType;
        linkData.EmployeeType = employeeType;

        if (validationResult.LinkId == default)
        {
            if (!await CheckQuota(linkData.LinkType, employeeType))
            {
                linkData.Result = EmailValidationKeyProvider.ValidationResult.TariffLimit;
            }
            return linkData;
        }

        var record = await GetLinkRecordAsync(validationResult.LinkId);

        if (record is not { SubjectType: SubjectType.InvitationLink })
        {
            linkData.Result = EmailValidationKeyProvider.ValidationResult.Invalid;
            return linkData;
        }

        linkData.Result = record.Options.ExpirationDate > DateTime.UtcNow 
            ? EmailValidationKeyProvider.ValidationResult.Ok 
            : EmailValidationKeyProvider.ValidationResult.Expired;

        linkData.Share = record.Share;
        linkData.RoomId = record.EntryId;
        linkData.EmployeeType = FileSecurity.GetTypeByShare(record.Share);

        if (!await CheckQuota(linkData.LinkType, linkData.EmployeeType))
        {
            linkData.Result = EmailValidationKeyProvider.ValidationResult.TariffLimit;
        }

        return linkData;
    }

    private async Task<FileShareRecord<string>> GetLinkRecordAsync(Guid linkId)
    {
        var securityDao = daoFactory.GetSecurityDao<string>();
        var share = await securityDao.GetSharesAsync(new[] { linkId }).FirstOrDefaultAsync(s => s.SubjectType == SubjectType.InvitationLink);
        return share;
    }

    private async Task<bool> CheckQuota(InvitationLinkType linkType, EmployeeType employeeType)
    {
        if (linkType == InvitationLinkType.Individual ||
            employeeType is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin or EmployeeType.Collaborator))
        {
            return true;
        }

        try
        {
            await countPaidUserChecker.CheckAppend();
        }
        catch (TenantQuotaException)
        {
            return false;
        }

        return true;
    }

    private async Task<(string, string)> GetRoomDataAsync(string roomId, string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            FileEntry entry = int.TryParse(roomId, out var id)
                ? await daoFactory.GetFolderDao<int>().GetFolderAsync(id)
                : await daoFactory.GetFolderDao<string>().GetFolderAsync(roomId);

            return (roomId, entry.Title);
        }
        
        var user = await userManager.GetUserByEmailAsync(email);

        if (user.Equals(Constants.LostUser))
        {
            return (null, null);
        }
        
        if (int.TryParse(roomId, out var intId))
        {
            var internalRoom = await daoFactory.GetFolderDao<int>().GetFolderAsync(intId);

            if (internalRoom == null || !DocSpaceHelper.IsRoom(internalRoom.FolderType) || !await fileSecurity.CanReadAsync(internalRoom, user.Id))
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

        if (thirdPartyRoom == null || !DocSpaceHelper.IsRoom(thirdPartyRoom.FolderType) || !await fileSecurity.CanReadAsync(thirdPartyRoom, user.Id))
        {
            return (null, null);
        }
    
        return (thirdPartyRoom.Id, thirdPartyRoom.Title);
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
    public bool IsCorrect => Result == EmailValidationKeyProvider.ValidationResult.Ok;
}