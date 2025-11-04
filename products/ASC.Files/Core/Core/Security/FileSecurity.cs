// (c) Copyright Ascensio System SIA 2009-2025
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

using System.ComponentModel;

using Actions = ASC.Web.Studio.Core.Notify.Actions;

namespace ASC.Files.Core.Security;

[Scope]
public class FileSecurityCommon(UserManager userManager, WebItemSecurity webItemSecurity)
{
    public async Task<bool> IsDocSpaceAdministratorAsync(Guid userId)
    {
        return await userManager.IsUserInGroupAsync(userId, Constants.GroupAdmin.ID) ||
               await webItemSecurity.IsProductAdministratorAsync(ProductEntryPoint.ID, userId);
    }
}

[Scope(typeof(IFileSecurity))]
public class FileSecurity(
    IHttpContextAccessor httpContextAccessor,
    IDaoFactory daoFactory,
    UserManager userManager,
    TenantManager tenantManager,
    AuthContext authContext,
    GlobalFolder globalFolder,
    FileSecurityCommon fileSecurityCommon,
    FileUtility fileUtility,
    StudioNotifyHelper studioNotifyHelper,
    BadgesSettingsHelper badgesSettingsHelper,
    ExternalShare externalShare,
    AuthManager authManager,
    VectorizationSettings vectorizationSettings,
    VectorizationHelper vectorizationHelper)
    : IFileSecurity
{
    public readonly FileShare DefaultMyShare = FileShare.Restrict;
    public readonly FileShare DefaultCommonShare = FileShare.Read;
    public readonly FileShare DefaultPrivacyShare = FileShare.Restrict;
    public readonly FileShare DefaultArchiveShare = FileShare.Restrict;
    public readonly FileShare DefaultVirtualRoomsShare = FileShare.Restrict;
    public readonly FileShare DefaultRoomTemplatesShare = FileShare.Restrict;

    public static readonly HashSet<FileShare> PaidShares = [FileShare.RoomManager];
    private static HashSet<FileShare> DefaultFileAccess => [FileShare.Editing, FileShare.FillForms, FileShare.Review, FileShare.Comment, FileShare.Read, FileShare.None];
    private static readonly FrozenDictionary<SubjectType, HashSet<FileShare>> _defaultFileShareDictionary = new Dictionary<SubjectType, HashSet<FileShare>>
    {
        { SubjectType.ExternalLink, DefaultFileAccess },
        { SubjectType.PrimaryExternalLink, DefaultFileAccess }
    }.ToFrozenDictionary();


    private static readonly FrozenDictionary<FolderType, FrozenDictionary<SubjectType, HashSet<FileShare>>> _availableRoomFileAccesses =
        new Dictionary<FolderType, FrozenDictionary<SubjectType, HashSet<FileShare>>>
    {
        { FolderType.USER,
            new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { SubjectType.User, [FileShare.ReadWrite, FileShare.Editing, FileShare.FillForms, FileShare.Review, FileShare.Comment, FileShare.Read, FileShare.Restrict, FileShare.None] },
                { SubjectType.Group, [FileShare.ReadWrite, FileShare.Editing, FileShare.FillForms, FileShare.Review, FileShare.Comment, FileShare.Read, FileShare.Restrict, FileShare.None] },
                { SubjectType.ExternalLink, DefaultFileAccess },
                { SubjectType.PrimaryExternalLink, DefaultFileAccess }
            }.ToFrozenDictionary()
        },
        { FolderType.CustomRoom, _defaultFileShareDictionary },
        { FolderType.PublicRoom, _defaultFileShareDictionary },
        { FolderType.EditingRoom, _defaultFileShareDictionary },
        { FolderType.VirtualDataRoom, _defaultFileShareDictionary },
        { FolderType.FillingFormsRoom,
            new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { SubjectType.ExternalLink, [FileShare.FillForms, FileShare.None] },
                { SubjectType.PrimaryExternalLink, [FileShare.FillForms, FileShare.None] }
            }.ToFrozenDictionary()
        },
        { FolderType.AiRoom, _defaultFileShareDictionary }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<FolderType, FrozenDictionary<SubjectType, int>> _linkCountRoomSettingsAccesses =
        new Dictionary<FolderType, FrozenDictionary<SubjectType, int>>
    {
        {
            FolderType.CustomRoom,
            new Dictionary<SubjectType, int>
            {
                { SubjectType.PrimaryExternalLink, 1 },
                { SubjectType.ExternalLink, 5 }
            }.ToFrozenDictionary()
        },
        {
            FolderType.PublicRoom,
            new Dictionary<SubjectType, int>
            {
                { SubjectType.PrimaryExternalLink, 1 },
                { SubjectType.ExternalLink, 5 }
            }.ToFrozenDictionary()
        },
        {
            FolderType.FillingFormsRoom,
            new Dictionary<SubjectType, int>
            {
                { SubjectType.PrimaryExternalLink, 1 }
            }.ToFrozenDictionary()
        }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<FolderType, FrozenDictionary<SubjectType, int>> _linkCountRoomFileSettingsAccesses =
        new Dictionary<FolderType, FrozenDictionary<SubjectType, int>>
    {
        { FolderType.USER, new Dictionary<SubjectType, int> { { SubjectType.ExternalLink, 6 } }.ToFrozenDictionary() },
        { FolderType.CustomRoom, new Dictionary<SubjectType, int> { { SubjectType.ExternalLink, 6 } }.ToFrozenDictionary() },
        { FolderType.VirtualDataRoom, new Dictionary<SubjectType, int> { { SubjectType.ExternalLink, 6 } }.ToFrozenDictionary() },
        { FolderType.EditingRoom, new Dictionary<SubjectType, int> { { SubjectType.ExternalLink, 6 } }.ToFrozenDictionary() },
        {
            FolderType.PublicRoom,
            new Dictionary<SubjectType, int>
            {
                { SubjectType.PrimaryExternalLink, 1 },
                { SubjectType.ExternalLink, 5 }
            }.ToFrozenDictionary()
        },
        { FolderType.FillingFormsRoom, new Dictionary<SubjectType, int> { { SubjectType.PrimaryExternalLink, 1 } }.ToFrozenDictionary() },
        { FolderType.AiRoom, new Dictionary<SubjectType, int> { { SubjectType.ExternalLink, 6 } }.ToFrozenDictionary() },
    }.ToFrozenDictionary();

    public static readonly FrozenDictionary<FolderType, FrozenDictionary<SubjectType, HashSet<FileShare>>> AvailableRoomAccesses =
        new Dictionary<FolderType, FrozenDictionary<SubjectType, HashSet<FileShare>>>
        {
            { FolderType.USER, _defaultFileShareDictionary },
            {
                FolderType.CustomRoom, new Dictionary<SubjectType, HashSet<FileShare>>
                {
                    {
                        SubjectType.User, [
                            FileShare.RoomManager, FileShare.ContentCreator, FileShare.Editing, FileShare.Review,
                            FileShare.Comment, FileShare.Read, FileShare.None
                        ]
                    },
                    {
                        SubjectType.Group, [
                            FileShare.ContentCreator, FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.Read, FileShare.None
                        ]
                    },
                    {
                        SubjectType.InvitationLink, [
                            FileShare.ContentCreator, FileShare.Editing, FileShare.Review,
                            FileShare.Comment, FileShare.Read, FileShare.None
                        ]
                    },
                    { SubjectType.ExternalLink, DefaultFileAccess },
                    { SubjectType.PrimaryExternalLink, DefaultFileAccess }
                }.ToFrozenDictionary()
            },
            {
                FolderType.PublicRoom,
                new Dictionary<SubjectType, HashSet<FileShare>>
                {
                    { SubjectType.User, [FileShare.RoomManager, FileShare.ContentCreator, FileShare.None] },
                    { SubjectType.Group, [FileShare.ContentCreator, FileShare.None] },
                    { SubjectType.InvitationLink, [FileShare.ContentCreator, FileShare.Read, FileShare.None] },
                    { SubjectType.ExternalLink, DefaultFileAccess },
                    { SubjectType.PrimaryExternalLink, DefaultFileAccess }
                }.ToFrozenDictionary()
            },
            {
                FolderType.FillingFormsRoom,
                new Dictionary<SubjectType, HashSet<FileShare>>
                {
                    { SubjectType.User, [FileShare.RoomManager, FileShare.ContentCreator, FileShare.FillForms, FileShare.None] },
                    { SubjectType.Group, [FileShare.ContentCreator, FileShare.FillForms, FileShare.None] },
                    { SubjectType.InvitationLink, [FileShare.ContentCreator, FileShare.FillForms, FileShare.None] },
                    { SubjectType.ExternalLink, [FileShare.FillForms, FileShare.Read, FileShare.None] },
                    { SubjectType.PrimaryExternalLink, [FileShare.FillForms, FileShare.Read, FileShare.None] }
                }.ToFrozenDictionary()
            },
            {
                FolderType.EditingRoom,
                new Dictionary<SubjectType, HashSet<FileShare>>
                {
                    { SubjectType.User, [FileShare.RoomManager, FileShare.ContentCreator, FileShare.Editing, FileShare.Read, FileShare.None] },
                    { SubjectType.Group, [FileShare.ContentCreator, FileShare.Editing, FileShare.Read, FileShare.None] },
                    { SubjectType.InvitationLink, [FileShare.ContentCreator, FileShare.Editing, FileShare.Read, FileShare.None] }
                }.ToFrozenDictionary()
            },
            {
                FolderType.VirtualDataRoom,
                new Dictionary<SubjectType, HashSet<FileShare>>
                {
                    { SubjectType.User, [FileShare.RoomManager, FileShare.ContentCreator, FileShare.Editing, FileShare.Read, FileShare.FillForms, FileShare.None] },
                    { SubjectType.Group, [FileShare.ContentCreator, FileShare.Editing, FileShare.Read, FileShare.FillForms, FileShare.None] },
                    { SubjectType.InvitationLink, [FileShare.ContentCreator, FileShare.Editing, FileShare.Read, FileShare.FillForms, FileShare.None] }
                }.ToFrozenDictionary()
            },
            {
                FolderType.AiRoom,
                new Dictionary<SubjectType, HashSet<FileShare>>
                {
                    { SubjectType.User, [FileShare.RoomManager, FileShare.ContentCreator, FileShare.Read, FileShare.None] },
                    { SubjectType.Group, [FileShare.ContentCreator, FileShare.Read, FileShare.None] },
                    { SubjectType.InvitationLink, [FileShare.ContentCreator, FileShare.Read, FileShare.None] }
                }.ToFrozenDictionary()
            }
        }.ToFrozenDictionary();

    public static readonly FrozenDictionary<EmployeeType, HashSet<FileShare>> AvailableUserAccesses = new Dictionary<EmployeeType, HashSet<FileShare>>
    {
        {
            EmployeeType.DocSpaceAdmin, [
                FileShare.RoomManager, FileShare.ContentCreator, FileShare.Editing, FileShare.FillForms, FileShare.Review,
                FileShare.Comment, FileShare.Read, FileShare.None
            ]
        },
        {
            EmployeeType.RoomAdmin, [
                FileShare.RoomManager, FileShare.ContentCreator, FileShare.Editing, FileShare.Review, FileShare.Comment,
                FileShare.FillForms, FileShare.Read, FileShare.None
            ]
        },
        {
            EmployeeType.User, [
                FileShare.ContentCreator, FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.FillForms,
                FileShare.Read, FileShare.None
            ]
        },
        {
            EmployeeType.Guest, [
                FileShare.ContentCreator, FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.FillForms,
                FileShare.Read, FileShare.None
            ]
        }
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<FileEntryType, IEnumerable<FilesSecurityActions>> _securityEntries =
    new Dictionary<FileEntryType, IEnumerable<FilesSecurityActions>>
    {
            {
                FileEntryType.File, new List<FilesSecurityActions>
                {
                    FilesSecurityActions.Read,
                    FilesSecurityActions.Comment,
                    FilesSecurityActions.FillForms,
                    FilesSecurityActions.Review,
                    FilesSecurityActions.Edit,
                    FilesSecurityActions.Delete,
                    FilesSecurityActions.CustomFilter,
                    FilesSecurityActions.Rename,
                    FilesSecurityActions.ReadHistory,
                    FilesSecurityActions.Lock,
                    FilesSecurityActions.EditHistory,
                    FilesSecurityActions.Copy,
                    FilesSecurityActions.Move,
                    FilesSecurityActions.Duplicate,
                    FilesSecurityActions.SubmitToFormGallery,
                    FilesSecurityActions.Download,
                    FilesSecurityActions.Convert,
                    FilesSecurityActions.CreateRoomFrom,
                    FilesSecurityActions.CopyLink,
                    FilesSecurityActions.Embed,
                    FilesSecurityActions.StartFilling,
                    FilesSecurityActions.FillingStatus,
                    FilesSecurityActions.ResetFilling,
                    FilesSecurityActions.StopFilling,
                    FilesSecurityActions.OpenForm,
                    FilesSecurityActions.Vectorization,
                    FilesSecurityActions.AscAi
                }
            },
            {
                FileEntryType.Folder, new List<FilesSecurityActions>
                {
                    FilesSecurityActions.Read,
                    FilesSecurityActions.Create,
                    FilesSecurityActions.Delete,
                    FilesSecurityActions.EditRoom,
                    FilesSecurityActions.Rename,
                    FilesSecurityActions.CopyTo,
                    FilesSecurityActions.MoveTo,
                    FilesSecurityActions.Copy,
                    FilesSecurityActions.Move,
                    FilesSecurityActions.Pin,
                    FilesSecurityActions.Mute,
                    FilesSecurityActions.EditAccess,
                    FilesSecurityActions.Duplicate,
                    FilesSecurityActions.Download,
                    FilesSecurityActions.CopySharedLink,
                    FilesSecurityActions.Reconnect,
                    FilesSecurityActions.CreateRoomFrom,
                    FilesSecurityActions.CopyLink,
                    FilesSecurityActions.Embed,
                    FilesSecurityActions.ChangeOwner,
                    FilesSecurityActions.IndexExport,
                    FilesSecurityActions.UseChat
                }
            }
    }.ToFrozenDictionary();

    private ConcurrentDictionary<string, FileShareRecord<int>> _cachedRecordsInternal;
    private ConcurrentDictionary<string, FileShareRecord<string>> _cachedRecordsThirdParty;
    private readonly ConcurrentDictionary<string, Guid> _cachedRoomOwner = new();

    private ConcurrentDictionary<string, FileShareRecord<T>> GetCachedRecords<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return (ConcurrentDictionary<string, FileShareRecord<T>>)Convert.ChangeType(_cachedRecordsInternal ??= new ConcurrentDictionary<string, FileShareRecord<int>>(), typeof(ConcurrentDictionary<string, FileShareRecord<T>>));
        }

        if (typeof(T) == typeof(string))
        {
            _cachedRecordsThirdParty ??= new ConcurrentDictionary<string, FileShareRecord<string>>();
            return (ConcurrentDictionary<string, FileShareRecord<T>>)Convert.ChangeType(_cachedRecordsThirdParty, typeof(ConcurrentDictionary<string, FileShareRecord<T>>));
        }

        return null;
    }

    public IAsyncEnumerable<Tuple<FileEntry<T>, bool>> CanReadAsync<T>(IAsyncEnumerable<FileEntry<T>> entries, Guid userId)
    {
        return CanAsync(entries, userId, FilesSecurityActions.Read);
    }

    public IAsyncEnumerable<Tuple<FileEntry<T>, bool>> CanReadAsync<T>(IAsyncEnumerable<FileEntry<T>> entries)
    {
        return CanReadAsync(entries, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanReadAsync<T>(FileEntry<T> entry)
    {
        return await CanReadAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanReadAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Read);
    }

    public async Task<bool> CanReadHistoryAsync<T>(FileEntry<T> entry)
    {
        return await CanReadHistoryAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanReadHistoryAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.ReadHistory);
    }

    public async Task<bool> CanCommentAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Comment);
    }

    public async Task<bool> CanCommentAsync<T>(FileEntry<T> entry)
    {
        return await CanCommentAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanFillFormsAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.FillForms);
    }
    public async Task<bool> CanStartFillingAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.StartFilling);
    }
    public async Task<bool> CanStopFillingAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.StopFilling);
    }
    public async Task<bool> CanResetFillingAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.ResetFilling);
    }

    public async Task<bool> CanFillFormsAsync<T>(FileEntry<T> entry)
    {
        return await CanFillFormsAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanReviewAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Review);
    }

    public async Task<bool> CanReviewAsync<T>(FileEntry<T> entry)
    {
        return await CanReviewAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanCustomFilterEditAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.CustomFilter);
    }

    public async Task<bool> CanCustomFilterEditAsync<T>(FileEntry<T> entry)
    {
        return await CanCustomFilterEditAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanCreateAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Create);
    }

    public async Task<bool> CanCreateAsync<T>(FileEntry<T> entry)
    {
        return await CanCreateAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanCreateFromAsync<T>(FileEntry<T> entry)
    {
        return await CanCreateFromAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanCreateFromAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.CreateFrom);
    }

    public async Task<bool> CanEditAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Edit);
    }

    public async Task<bool> CanEditAsync<T>(FileEntry<T> entry)
    {
        return await CanEditAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanDeleteAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Delete);
    }

    public async Task<bool> CanDeleteAsync<T>(FileEntry<T> entry)
    {
        return await CanDeleteAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanEditRoomAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.EditRoom);
    }

    public async Task<bool> CanEditRoomAsync<T>(FileEntry<T> entry)
    {
        return await CanEditRoomAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanRenameAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Rename);
    }

    public async Task<bool> CanRenameAsync<T>(FileEntry<T> entry)
    {
        return await CanRenameAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanDownloadAsync<T>(FileEntry<T> entry)
    {
        return await CanDownloadAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanDownloadAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Download);
    }

    public async Task<bool> CanShareAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanEditAsync(entry, userId);
    }

    public async Task<bool> CanShareAsync<T>(FileEntry<T> entry)
    {
        return await CanShareAsync(entry, authContext.CurrentAccount.ID);
    }

    public Task<bool> CanConvertAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return CanAsync(entry, userId, FilesSecurityActions.Convert);
    }

    public Task<bool> CanConvertAsync<T>(FileEntry<T> entry)
    {
        return CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.Convert);
    }

    public async Task<bool> CanLockAsync<T>(FileEntry<T> entry)
    {
        return await CanLockAsync(entry, authContext.CurrentAccount.ID);
    }

    public async Task<bool> CanLockAsync<T>(FileEntry<T> entry, Guid userId)
    {
        return await CanAsync(entry, userId, FilesSecurityActions.Lock);
    }

    public async Task<bool> CanCopyToAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.CopyTo);
    }

    public async Task<bool> CanCopyAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.Copy);
    }

    public async Task<bool> CanMoveToAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.MoveTo);
    }

    public async Task<bool> CanMoveAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.Move);
    }

    public async Task<bool> CanPinAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.Pin);
    }

    public async Task<bool> CanEditAccessAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.EditAccess);
    }

    public async Task<bool> CanEditInternalAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.EditInternal);
    }

    public async Task<bool> CanEditExpirationAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.EditExpiration);
    }

    public async Task<bool> CanEditHistoryAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.EditHistory);
    }

    public async Task<bool> CanReadLinksAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.ReadLinks);
    }

    public async Task<bool> CanChangeOwnerAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.ChangeOwner);
    }

    public async Task<bool> CanIndexExportAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.IndexExport);
    }

    public async Task<bool> CanVectorizationAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.Vectorization);
    }

    public async Task<bool> CanUseChatAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.UseChat);
    }
    
    public async Task<IEnumerable<Guid>> WhoCanReadAsync<T>(FileEntry<T> entry, bool includeLinks = false)
    {
        var (directAccess, sharedAccess) = await WhoCanAsync(entry, FilesSecurityActions.Read, includeLinks);

        return directAccess.Concat(sharedAccess).Distinct();
    }

    public async Task<(IEnumerable<Guid> directAccess, IEnumerable<Guid> sharedAccess)> WhoCanReadSeparatelyAsync<T>(FileEntry<T> entry)
    {
        return await WhoCanAsync(entry, FilesSecurityActions.Read, true);
    }

    private async Task<(IEnumerable<Guid> directAccess, IEnumerable<Guid> sharedAccess)> WhoCanAsync<T>(FileEntry<T> entry, FilesSecurityActions action, bool includeLinks = false)
    {
        List<Guid> directAccess = [];
        List<Guid> sharedAccess = [];

        var sharedAsDirect = true;

        var sharesFromDb = await GetSharesAsync(entry);

        if (!includeLinks)
        {
            sharesFromDb = sharesFromDb.Where(r => !r.IsLink);
        }

        var shares = sharesFromDb.ToList();

        var linksUsersTask = includeLinks ?
            GetLinksUsersAsync(shares.Where(r => r.SubjectType is SubjectType.PrimaryExternalLink or SubjectType.ExternalLink))
            : Task.FromResult(new List<Guid>(0));

        var tenantId = tenantManager.GetCurrentTenantId();
        var copyShares = shares.GroupBy(k => k.Subject).ToDictionary(k => k.Key);

        FileShareRecord<T>[] defaultRecords;

        switch (entry.RootFolderType)
        {
            case FolderType.COMMON:
                defaultRecords =
                [
                    new FileShareRecord<T>
                    {
                        Level = int.MaxValue,
                        EntryId = entry.Id,
                        EntryType = entry.FileEntryType,
                        Share = DefaultCommonShare,
                        Subject = Constants.GroupEveryone.ID,
                        TenantId = tenantId,
                        Owner = authContext.CurrentAccount.ID
                    }
                ];

                if (!shares.Any())
                {
                    var defaultShareRecord = defaultRecords.FirstOrDefault();

                    if (defaultShareRecord != null && ((defaultShareRecord.Share == FileShare.Read && action == FilesSecurityActions.Read) ||
                        (defaultShareRecord.Share == FileShare.ReadWrite)))
                    {
                        return ((await userManager.GetUsersByGroupAsync(defaultShareRecord.Subject))
                                    .Where(x => x.Status == EmployeeStatus.Active).Select(y => y.Id).Distinct(), sharedAccess);
                    }

                    return (directAccess, sharedAccess);
                }

                break;

            case FolderType.USER:
                sharedAsDirect = false;

                defaultRecords =
                [
                    new FileShareRecord<T>
                    {
                        Level = int.MaxValue,
                        EntryId = entry.Id,
                        EntryType = entry.FileEntryType,
                        Share = DefaultMyShare,
                        Subject = entry.RootCreateBy,
                        TenantId = tenantId,
                        Owner = entry.RootCreateBy
                    }
                ];

                if (shares.Count == 0)
                {
                    return ([entry.RootCreateBy], sharedAccess);
                }
                break;

            case FolderType.Privacy:
                defaultRecords =
                [
                    new FileShareRecord<T>
                    {
                        Level = int.MaxValue,
                        EntryId = entry.Id,
                        EntryType = entry.FileEntryType,
                        Share = DefaultPrivacyShare,
                        Subject = entry.RootCreateBy,
                        TenantId = tenantId,
                        Owner = entry.RootCreateBy
                    }
                ];

                if (shares.Count == 0)
                {
                    return ([entry.RootCreateBy], sharedAccess);
                }
                break;

            case FolderType.BUNCH:
                if (action == FilesSecurityActions.Read)
                {
                    var folderDao = daoFactory.GetFolderDao<T>();
                    var root = await folderDao.GetFolderAsync(entry.RootId);
                    if (root != null)
                    {
                        var path = await folderDao.GetBunchObjectIDAsync(root.Id);

                        var adapter = FilesIntegration.GetFileSecurity(path);

                        if (adapter != null)
                        {
                            return (await adapter.WhoCanReadAsync(entry), sharedAccess);
                        }
                    }
                }

                // TODO: For Projects and other
                defaultRecords = null;
                break;

            case FolderType.VirtualRooms:
                defaultRecords = null;

                if (entry is not Folder<T> || entry is Folder<T> folder && folder.FolderType != FolderType.VirtualRooms)
                {
                    break;
                }

                defaultRecords =
                [
                    new FileShareRecord<T>
                    {
                        Level = int.MaxValue,
                        EntryId = entry.Id,
                        EntryType = entry.FileEntryType,
                        Share = FileShare.Read,
                        Subject = Constants.GroupEveryone.ID,
                        TenantId = tenantId,
                        Owner = entry.RootCreateBy
                    }
                ];

                if (!shares.Any())
                {
                    foreach (var defaultRecord in defaultRecords)
                    {
                        directAccess.AddRange((await userManager.GetUsersByGroupAsync(defaultRecord.Subject)).Where(x => x.Status == EmployeeStatus.Active).Select(y => y.Id).Distinct());
                    }

                    return (directAccess, sharedAccess);
                }

                break;

            default:
                defaultRecords = null;
                break;
        }


        var defaultAccessUsers = (defaultRecords ?? []).ToAsyncEnumerable().SelectManyAwait(async x => await ToGuidAsync(x)).Distinct();

        await foreach (var userId in defaultAccessUsers)
        {
            if (await CheckAccessAsync(userId, action))
            {
                directAccess.Add(userId);
            }
        }

        var manyShares = shares.ToAsyncEnumerable().SelectManyAwait(async x => await ToGuidAsync(x)).Distinct();

        await foreach (var userId in manyShares)
        {
            if (await CheckAccessAsync(userId, action))
            {
                if (sharedAsDirect)
                {
                    directAccess.Add(userId);
                }
                else
                {
                    sharedAccess.Add(userId);
                }
            }
        }

        var linkUsers = await linksUsersTask;
        if (linkUsers.Count != 0)
        {
            sharedAccess.AddRange(linkUsers);
        }

        return (directAccess, sharedAccess);

        async Task<List<Guid>> GetLinksUsersAsync(IEnumerable<FileShareRecord<T>> linksRecords)
        {
            var tagDao = daoFactory.GetTagDao<T>();
            var users = new List<Guid>();

            foreach (var record in linksRecords)
            {
                var entryId = record.EntryId;
                var entryType = record.EntryType;

                if (entry.FileEntryType == FileEntryType.Folder)
                {
                    entryId = record.ParentId;
                }

                users.AddRange(await tagDao.GetTagsAsync(entryId, entryType, TagType.RecentByLink, null, record.Subject.ToString())
                    .Select(x => x.Owner)
                    .ToListAsync());
            }

            return users;
        }

        async Task<bool> CheckAccessAsync(Guid userId, FilesSecurityActions filesSecurityAction)
        {
            var userSubjects = await GetUserSubjectsAsync(userId);
            var userShares = new List<FileShareRecord<T>>();
            entry.Security = null;

            foreach (var subject in userSubjects)
            {
                if (copyShares.TryGetValue(subject, out var value))
                {
                    userShares.AddRange(value);
                }
            }

            return await CanAsync(entry, userId, filesSecurityAction, userShares, false);
        }
    }

    private async ValueTask<IAsyncEnumerable<Guid>> ToGuidAsync<T>(FileShareRecord<T> x)
    {
        if (x.SubjectType != SubjectType.Group)
        {
            return new[] { x.Subject }.ToAsyncEnumerable();
        }

        var groupInfo = await userManager.GetGroupInfoAsync(x.Subject);

        if (groupInfo.ID != Constants.LostGroupInfo.ID)
        {
            return (await userManager.GetUsersByGroupAsync(groupInfo.ID))
                .Where(p => p.Status == EmployeeStatus.Active)
                .Select(y => y.Id).ToAsyncEnumerable();
        }

        return new[] { x.Subject }.ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<FileEntry<T>> FilterReadAsync<T>(IAsyncEnumerable<FileEntry<T>> entries)
    {
        await foreach (var e in CanReadAsync(entries.Where(f => f != null)))
        {
            if (e.Item2)
            {
                yield return e.Item1;
            }
        }
    }

    public IAsyncEnumerable<FileEntry<T>> SetSecurity<T>(IAsyncEnumerable<FileEntry<T>> entries)
    {
        return SetSecurity(entries, authContext.CurrentAccount.ID);
    }

    private async IAsyncEnumerable<FileEntry<T>> SetSecurity<T>(IAsyncEnumerable<FileEntry<T>> entries, Guid userId)
    {
        var isOutsider = await userManager.IsOutsiderAsync(userId);
        var userType = await userManager.GetUserTypeAsync(userId);
        var isGuest = userType is EmployeeType.Guest;
        var isAuthenticated = authContext.IsAuthenticated;
        var isDocSpaceAdmin = userType is EmployeeType.DocSpaceAdmin;
        var isUser = userType is EmployeeType.User;

        await foreach (var entry in entries)
        {
            if (entry.Security != null)
            {
                yield return entry;
            }

            var security = new Dictionary<FilesSecurityActions, bool>();
            var parentFolders = await GetFileParentFolders(entry.ParentId);


            foreach (var action in Enum.GetValues<FilesSecurityActions>().Where(r => _securityEntries[entry.FileEntryType].Contains(r)))
            {
                security[action] = await FilterEntryAsync(entry, action, userId, null, isOutsider, isGuest, isAuthenticated, isDocSpaceAdmin, isUser, parentFolders);
            }

            entry.Security = security;

            yield return entry;
        }
    }

    public async IAsyncEnumerable<FileEntry<T>> FilterDownloadAsync<T>(IAsyncEnumerable<FileEntry<T>> entries)
    {
        await foreach (var e in CanAsync(entries.Where(f => f != null), authContext.CurrentAccount.ID, FilesSecurityActions.Download))
        {
            if (e.Item2)
            {
                yield return e.Item1;
            }
        }
    }

    public static FileShare GetHighFreeRole(FolderType folderType)
    {
        return folderType switch
        {
            FolderType.CustomRoom => FileShare.ContentCreator,
            FolderType.FillingFormsRoom => FileShare.ContentCreator,
            FolderType.EditingRoom => FileShare.ContentCreator,
            FolderType.PublicRoom => FileShare.ContentCreator,
            FolderType.VirtualDataRoom => FileShare.ContentCreator,
            FolderType.AiRoom => FileShare.ContentCreator,
            _ => FileShare.None
        };
    }

    public static EmployeeType GetTypeByShare(FileShare share)
    {
        return share switch
        {
            FileShare.RoomManager => EmployeeType.RoomAdmin,
            _ => EmployeeType.Guest
        };
    }

    private async Task<bool> CanAsync<T>(FileEntry<T> entry, Guid userId, FilesSecurityActions action, IEnumerable<FileShareRecord<T>> shares = null, bool setEntryAccess = true)
    {
        if (entry == null)
        {
            return false;
        }

        if (entry.Security != null && entry.Security.TryGetValue(action, out var result))
        {
            return result;
        }
        
        var isOutsider = await userManager.IsOutsiderAsync(userId);

        if (isOutsider && action != FilesSecurityActions.Read)
        {
            return false;
        }

        var userType = await userManager.GetUserTypeAsync(userId);
        var isGuest = userType is EmployeeType.Guest;
        var isDocSpaceAdmin = userType is EmployeeType.DocSpaceAdmin;
        var isUser = userType is EmployeeType.User;
        var isAuthenticated = authContext.IsAuthenticated || (await authManager.GetAccountByIDAsync(tenantManager.GetCurrentTenantId(), userId)).IsAuthenticated;

        var accessSnapshot = entry.Access;
        var parentFolders = await GetFileParentFolders(entry.ParentId);

        var haveAccess = await FilterEntryAsync(entry, action, userId, shares, isOutsider, isGuest, isAuthenticated, isDocSpaceAdmin, isUser, parentFolders);

        if (!setEntryAccess)
        {
            entry.Access = accessSnapshot;
            entry.ShareRecord = null;
        }

        return haveAccess;
    }


    private async IAsyncEnumerable<Tuple<FileEntry<T>, bool>> CanAsync<T>(IAsyncEnumerable<FileEntry<T>> entries, Guid userId, FilesSecurityActions action)
    { ;
        var isOutsider = await userManager.IsOutsiderAsync(userId);
        var userType = await userManager.GetUserTypeAsync(userId);
        var isGuest = userType is EmployeeType.Guest;
        var isAuthenticated = authContext.IsAuthenticated;
        var isDocSpaceAdmin = userType is EmployeeType.DocSpaceAdmin;
        var isUser = userType is EmployeeType.User;

        await foreach (var entry in entries)
        {
            var parentFolders = await GetFileParentFolders(entry.ParentId);
            yield return new Tuple<FileEntry<T>, bool>(entry, await FilterEntryAsync(entry, action, userId, null, isOutsider, isGuest, isAuthenticated, isDocSpaceAdmin, isUser, parentFolders));
        }
    }

    private async Task<bool> FilterEntryAsync<T>(FileEntry<T> e, FilesSecurityActions action, Guid userId, IEnumerable<FileShareRecord<T>> shares, bool isOutsider, bool isGuest,
        bool isAuthenticated, bool isDocSpaceAdmin, bool isUser, List<Folder<T>> parentFolders)
    {
        var file = e as File<T>;
        var folder = e as Folder<T>;
        var isRoom = folder != null && DocSpaceHelper.IsRoom(folder.FolderType);
        var cacheFileDao = daoFactory.GetCacheFileDao<T>();
        
        if (file != null && action == FilesSecurityActions.FillForms && !file.IsForm)
        {
            return false;
        }
        
        if (file != null && action == FilesSecurityActions.Edit && file.Category == (int)FilterType.Pdf && file.IsCompletedForm)
        {
            return false;
        }
        
        var room = parentFolders.FirstOrDefault(r => DocSpaceHelper.IsRoom(r.FolderType));

        if (room is { FolderType: FolderType.VirtualDataRoom })
        {
            var hasFullAccess = await HasFullAccessAsync(e, userId, isGuest, isRoom, isUser);
            
            if (file != null && !hasFullAccess && !await DocSpaceHelper.IsFormOrCompletedForm(file, daoFactory))
            {
                var shareRecord = await GetShareRecordAsync(room, userId, isDocSpaceAdmin, shares);
                if (shareRecord is { Share: FileShare.FillForms })
                {
                    return false;
                }
            }

            if (folder != null && !hasFullAccess)
            {
                var shareRecord = await GetShareRecordAsync(room, userId, isDocSpaceAdmin, shares);
                if (shareRecord is { Share: FileShare.FillForms })
                {
                    var folderDao = daoFactory.GetCacheFolderDao<T>();
                    if (!await folderDao.ContainsFormsInFolder(folder))
                    {
                        return false;
                    }
                }
            }
        }
        
        

        if (folder is { FolderType: FolderType.AiRoom } && 
            action is FilesSecurityActions.Create or 
                FilesSecurityActions.Copy or 
                FilesSecurityActions.CopyTo or 
                FilesSecurityActions.Move or 
                FilesSecurityActions.MoveTo or 
                FilesSecurityActions.Duplicate
            )
        {
            return false;
        }

        if (action is FilesSecurityActions.UseChat && folder is not { FolderType: FolderType.AiRoom })
        {
            return false;
        }

        if (action is FilesSecurityActions.AscAi &&
            (file == null || !vectorizationSettings.IsSupportedContentExtraction(file.Title)))
        {
            return false;
        }

        if (action == FilesSecurityActions.Vectorization)
        {
            if (file?.VectorizationStatus == null)
            {
                return false;
            }
            
            switch (file.VectorizationStatus)
            {
                case VectorizationStatus.Completed:
                case VectorizationStatus.InProgress when await vectorizationHelper.InProcessAsync(file.Id):
                    return false;
            }
        }

        if (file != null && room is { FolderType: FolderType.AiRoom } && parentFolders.Any(x => x.FolderType == FolderType.Knowledge))
        {
            if (action is not 
                (FilesSecurityActions.Read or 
                FilesSecurityActions.Download or 
                FilesSecurityActions.Delete or 
                FilesSecurityActions.Vectorization or 
                FilesSecurityActions.Copy))
            {
                return false;
            }

            if (action is FilesSecurityActions.Delete && 
                file is { VectorizationStatus: VectorizationStatus.InProgress } && 
                await vectorizationHelper.InProcessAsync(file.Id))
            {
                return false;
            }
        }

        if (action == FilesSecurityActions.IndexExport && (!isRoom || !folder.SettingsIndexing))
        {
            return false;
        }

        if (action is FilesSecurityActions.ReadHistory or FilesSecurityActions.EditHistory && e.ProviderEntry)
        {
            return false;
        }

        if (file != null && action == FilesSecurityActions.SubmitToFormGallery && !file.IsForm)
        {
            return false;
        }

        if (e.ProviderEntry && folder is { ProviderMapped: false } && e.CreateBy == userId)
        {
            return true;
        }

        if (action == FilesSecurityActions.Reconnect)
        {
            return isRoom && e.ProviderEntry && e.CreateBy == userId;
        }

        if (action == FilesSecurityActions.CreateRoomFrom)
        {
            return e.RootFolderType == FolderType.USER && e.RootCreateBy == userId && !isUser && !isGuest && (folder is { FolderType: FolderType.DEFAULT } || file != null);
        }

        if (action == FilesSecurityActions.Embed)
        {
            if (e.RootFolderType != FolderType.VirtualRooms)
            {
                return false;
            }

            if (folder != null && !(isRoom && folder.FullShared))
            {
                return false;
            }

            if (file != null && !(file.FullShared && fileUtility.CanWebView(file.Title)))
            {
                return false;
            }
        }

        if (e.FileEntryType == FileEntryType.Folder)
        {
            if (folder == null)
            {
                return false;
            }

            if (folder.FolderType == FolderType.Knowledge && 
                action is not (FilesSecurityActions.Read or FilesSecurityActions.MoveTo or FilesSecurityActions.CopyTo or FilesSecurityActions.Create))
            {
                return false;
            }

            if (folder.FolderType == FolderType.ResultStorage 
                && action is FilesSecurityActions.Rename or FilesSecurityActions.Delete or FilesSecurityActions.Copy or FilesSecurityActions.Move or FilesSecurityActions.Duplicate)
            {
                return false;
            }

            if (folder.FolderType is FolderType.Recent or FolderType.Favorites or FolderType.SHARE)
            {
                return action == FilesSecurityActions.Read;
            }

            if (userId.Equals(ASC.Core.Configuration.Constants.Guest.ID) &&
               folder.FolderType is
                   FolderType.ReadyFormFolder or
                   FolderType.InProcessFormFolder or
                   FolderType.FormFillingFolderDone or
                   FolderType.FormFillingFolderInProgress)
            {
                return false;
            }

            if (action != FilesSecurityActions.Read)
            {
                if (action is FilesSecurityActions.Delete &&
                    folder.FolderType is FolderType.ReadyFormFolder or FolderType.InProcessFormFolder)
                {
                    return false;
                }

                if (action is FilesSecurityActions.Duplicate or
                              FilesSecurityActions.EditAccess or
                              FilesSecurityActions.Edit or
                              FilesSecurityActions.Move or
                              FilesSecurityActions.MoveTo or
                              FilesSecurityActions.Rename or
                              FilesSecurityActions.Create
                    && folder.FolderType is
                        FolderType.ReadyFormFolder or
                        FolderType.InProcessFormFolder or
                        FolderType.FormFillingFolderDone or
                        FolderType.FormFillingFolderInProgress)
                {
                    return false;
                }

                if (action is FilesSecurityActions.Pin or FilesSecurityActions.Mute or FilesSecurityActions.ChangeOwner && !isRoom)
                {
                    return false;
                }

                if (action == FilesSecurityActions.Mute && isRoom && await IsAllGeneralNotificationSettingsOffAsync())
                {
                    return false;
                }

                if (action == FilesSecurityActions.CopyLink && DocSpaceHelper.IsFormsFillingSystemFolder(folder.FolderType))
                {
                    return false;
                }

                if (action is FilesSecurityActions.Copy or FilesSecurityActions.Duplicate && isRoom && folder.ProviderEntry)
                {
                    return false;
                }

                if (folder.FolderType == FolderType.Recent)
                {
                    return false;
                }

                if (action == FilesSecurityActions.EditInternal && folder.FolderType is FolderType.VirtualRooms or FolderType.VirtualDataRoom or FolderType.PublicRoom)
                {
                    return false;
                }

                if (action == FilesSecurityActions.EditExpiration && folder.FolderType is FolderType.FillingFormsRoom)
                {
                    return false;
                }

                if (!isGuest)
                {
                    if (folder.FolderType == FolderType.USER)
                    {
                        return folder.CreateBy == userId && action is FilesSecurityActions.Create or FilesSecurityActions.CopyTo or FilesSecurityActions.MoveTo or FilesSecurityActions.FillForms;
                    }

                    if (folder.FolderType == FolderType.Archive && action == FilesSecurityActions.MoveTo)
                    {
                        return true;
                    }

                    if (folder.FolderType == FolderType.VirtualRooms && !isUser)
                    {
                        return action is FilesSecurityActions.Create or FilesSecurityActions.MoveTo;
                    }

                    if (folder.FolderType == FolderType.AiAgents && !isUser)
                    {
                        return action is FilesSecurityActions.Create or FilesSecurityActions.MoveTo;
                    }

                    if (folder.FolderType == FolderType.RoomTemplates && !isUser)
                    {
                        return action is FilesSecurityActions.CreateFrom or FilesSecurityActions.MoveTo;
                    }

                    if (folder.FolderType == FolderType.TRASH)
                    {
                        return action == FilesSecurityActions.MoveTo;
                    }

                }
            }
            else if (isAuthenticated)
            {
                if (folder.FolderType == FolderType.VirtualRooms)
                {
                    return true;
                }

                if (folder.FolderType == FolderType.AiAgents)
                {
                    return true;
                }

                if (folder.FolderType == FolderType.Archive)
                {
                    return true;

                }
                if (folder.FolderType == FolderType.RoomTemplates)
                {
                    return true;
                }
            }
        }

        if (file == null || !await DocSpaceHelper.IsFormOrCompletedForm(file, daoFactory) || (file is { IsForm: true } && e.RootFolderType != FolderType.VirtualRooms))
        {
            switch (action)
            {
                case FilesSecurityActions.ResetFilling:
                case FilesSecurityActions.StopFilling:
                case FilesSecurityActions.StartFilling:
                case FilesSecurityActions.FillingStatus:
                    return false;
            }
        }

        if (file != null && action == FilesSecurityActions.EditAccess && parentFolders.Any(r => r.FolderType is FolderType.InProcessFormFolder or FolderType.ReadyFormFolder))
        {
            return false;
        }

        switch (e.RootFolderType)
        {
            case FolderType.DEFAULT:
                if (isDocSpaceAdmin)
                {
                    // administrator can work with crashed entries (crash in files_folder_tree)
                    return true;
                }
                break;
            case FolderType.TRASH:
                if (action != FilesSecurityActions.Read && action != FilesSecurityActions.Delete && action != FilesSecurityActions.Move)
                {
                    return false;
                }

                var myTrashId = await globalFolder.GetFolderTrashAsync(daoFactory);
                if (!Equals(myTrashId, 0))
                {
                    return Equals(e.RootId, myTrashId) && (folder == null || action != FilesSecurityActions.Delete || !Equals(e.Id, myTrashId));
                }
                break;
            case FolderType.USER:
                if (isOutsider || action == FilesSecurityActions.Lock)
                {
                    return false;
                }
                if (e.RootCreateBy == userId)
                {
                    if (isGuest && action != FilesSecurityActions.Read && action != FilesSecurityActions.Download)
                    {
                        return false;
                    }

                    // user has all right in his folder
                    return true;
                }
                break;
            case FolderType.RoomTemplates:
                if (action is
                    FilesSecurityActions.FillForms or
                    FilesSecurityActions.EditHistory or
                    FilesSecurityActions.ReadHistory or
                    FilesSecurityActions.SubmitToFormGallery or
                    FilesSecurityActions.Lock)
                {
                    return false;
                }

                if (action is FilesSecurityActions.EditAccess && !isRoom)
                {
                    return false;
                }

                if (await HasFullAccessAsync(e, userId, isGuest, isRoom, isUser))
                {
                    return true;
                }
                break;
            case FolderType.VirtualRooms:
            case FolderType.AiAgents:
                if (isDocSpaceAdmin && (folder is not { FolderType: FolderType.Knowledge} && !parentFolders.Any(p => p.FolderType is FolderType.Knowledge)))
                {
                    if (action == FilesSecurityActions.Download)
                    {
                        if (e.ProviderEntry)
                        {
                            return true;
                        }

                        if (isRoom)
                        {
                            if (!folder.SettingsDenyDownload)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (room is not { SettingsDenyDownload: true })
                            {
                                return true;
                            }
                        }
                    }

                    if (action == FilesSecurityActions.Duplicate && isRoom && !folder.SettingsDenyDownload)
                    {
                        return true;
                    }

                    if (action == FilesSecurityActions.EditAccess && !isRoom)
                    {
                        return true;
                    }

                    switch (action)
                    {
                        case FilesSecurityActions.Read or FilesSecurityActions.Copy:
                        case FilesSecurityActions.CopySharedLink when e.Shared:
                            return true;
                    }

                    if (isRoom && action is
                            FilesSecurityActions.Move or
                            FilesSecurityActions.Pin or
                            FilesSecurityActions.ChangeOwner or
                            FilesSecurityActions.IndexExport)
                    {
                        return true;
                    }
                }

                if (file != null && (action is
                    FilesSecurityActions.FillForms or
                    FilesSecurityActions.Edit or
                    FilesSecurityActions.StartFilling or
                    FilesSecurityActions.FillingStatus or
                    FilesSecurityActions.ResetFilling or
                    FilesSecurityActions.StopFilling or
                    FilesSecurityActions.SubmitToFormGallery or
                    FilesSecurityActions.CopyLink or
                    FilesSecurityActions.OpenForm)
                    && await DocSpaceHelper.IsFormOrCompletedForm(file, daoFactory))
                {

                    if (action == FilesSecurityActions.FillForms)
                    {
                        var fileParentFolder = parentFolders.LastOrDefault();
                        if (fileParentFolder != null && ((fileParentFolder.FolderType == FolderType.FormFillingFolderInProgress && file.CreateBy != userId) || fileParentFolder.FolderType == FolderType.FormFillingFolderDone))
                        {
                            return false;
                        }
                    }

                    var fileFolder = parentFolders.FirstOrDefault(r => DocSpaceHelper.IsRoom(r.FolderType));
                    if (fileFolder is { FolderType: FolderType.VirtualDataRoom } && !userId.Equals(ASC.Core.Configuration.Constants.Guest.ID))
                    {
                        var (currentStep, myRoles) = await cacheFileDao.GetUserFormRoles(file.Id, userId);
                        var role = myRoles.FirstOrDefault(r => !r.Submitted);
                        var properties = await cacheFileDao.GetProperties(file.Id);
                        var formFilling = properties?.FormFilling;

                        var userHasFullAccess = await HasFullAccessAsync(e, userId, isGuest, isRoom, isUser);

                        var shareRecord = await GetShareRecordAsync(room, userId, isDocSpaceAdmin, shares);
                        var formShareRecord = await GetCurrentShareAsync(file, userId, isDocSpaceAdmin, shares);

                        var hasFullAccessToForm = userHasFullAccess || (shareRecord is { Share: FileShare.ContentCreator or FileShare.RoomManager });

                        var isFillingStoped = formFilling?.FillingStopedDate != null && !DateTime.MinValue.Equals(formFilling?.FillingStopedDate);
                        return action switch
                        {
                            FilesSecurityActions.ResetFilling =>
                                (userHasFullAccess || shareRecord is { Share: FileShare.RoomManager } || (shareRecord is { Share: FileShare.ContentCreator }) && file.CreateBy.Equals(userId)) && formFilling?.StartFilling == true && isFillingStoped,

                            FilesSecurityActions.StopFilling =>
                                (userHasFullAccess || shareRecord is { Share: FileShare.RoomManager } || (shareRecord is { Share: FileShare.ContentCreator }) && file.CreateBy.Equals(userId)) && formFilling?.StartFilling == true && !isFillingStoped && currentStep > 0,

                            FilesSecurityActions.StartFilling =>
                                hasFullAccessToForm && (formFilling == null || formFilling?.StartFilling == false || formFilling?.StartFilling == null),

                            FilesSecurityActions.FillForms =>
                                (!isFillingStoped && myRoles.Count != 0 && (role != null && role.Sequence == currentStep)) || (formShareRecord is { Share: FileShare.FillForms } && myRoles.Count == 0),

                            FilesSecurityActions.Edit =>
                                (currentStep == -1 && (hasFullAccessToForm || e.Access is FileShare.Editing)) || formShareRecord is { Share: FileShare.Editing },

                            FilesSecurityActions.FillingStatus =>
                                formFilling?.StartFilling == true,

                            FilesSecurityActions.OpenForm =>
                                (formFilling?.StartFilling == true && role == null) || currentStep == 0 || isFillingStoped || (role != null && role.Sequence != currentStep),

                            _ => false
                        };
                    }

                    switch (action)
                    {
                        case FilesSecurityActions.ResetFilling:
                        case FilesSecurityActions.StopFilling:
                        case FilesSecurityActions.StartFilling:
                        case FilesSecurityActions.FillingStatus:
                            return false;
                    }
                }
                else if (file is not { IsForm: true } && action is FilesSecurityActions.OpenForm)
                {
                    return false;
                }

                if (action is
                       FilesSecurityActions.Rename or
                       FilesSecurityActions.Lock or
                       FilesSecurityActions.Move or
                       FilesSecurityActions.Duplicate or
                       FilesSecurityActions.EditHistory or
                       FilesSecurityActions.SubmitToFormGallery or
                       FilesSecurityActions.Embed or
                       FilesSecurityActions.EditInternal or
                       FilesSecurityActions.EditExpiration &&
                   file != null)
                {
                    var fileFolder = parentFolders?.LastOrDefault();
                    if (fileFolder?.FolderType is FolderType.FormFillingFolderInProgress or FolderType.FormFillingFolderDone)
                    {
                        return false;
                    }
                }

                if (action == FilesSecurityActions.CopyLink && file != null)
                {
                    if (parentFolders.Exists(parent => DocSpaceHelper.IsFormsFillingSystemFolder(parent.FolderType)))
                    {
                        return false;
                    }

                    if (isDocSpaceAdmin)
                    {
                        return true;
                    }
                }

                if (file != null || folder != null && !isRoom)
                {
                    var fileFolder = parentFolders?.FirstOrDefault(r => DocSpaceHelper.IsRoom(r.FolderType));

                    switch (action)
                    {
                        case FilesSecurityActions.EditInternal when fileFolder?.FolderType is FolderType.VirtualRooms or FolderType.VirtualDataRoom or FolderType.PublicRoom:
                        case FilesSecurityActions.EditExpiration when fileFolder?.FolderType is FolderType.FillingFormsRoom:
                            return false;
                    }
                }

                if (await HasFullAccessAsync(e, userId, isGuest, isRoom, isUser))
                {
                    return true;
                }
                break;
            case FolderType.Archive:
                if (action != FilesSecurityActions.Read &&
                    action != FilesSecurityActions.Delete &&
                    action != FilesSecurityActions.ReadHistory &&
                    action != FilesSecurityActions.Copy &&
                    action != FilesSecurityActions.Move &&
                    action != FilesSecurityActions.Download &&
                    action != FilesSecurityActions.ReadLinks &&
                    action != FilesSecurityActions.IndexExport
                    )
                {
                    return false;
                }

                if (action is FilesSecurityActions.Delete or FilesSecurityActions.Move &&
                    !isRoom)
                {
                    return false;
                }

                if (isDocSpaceAdmin && (folder is not { FolderType: FolderType.Knowledge} && !parentFolders.Any(p => p.FolderType is FolderType.Knowledge)))
                {
                    if (action == FilesSecurityActions.Download)
                    {
                        if (e.ProviderEntry)
                        {
                            return true;
                        }

                        if (isRoom)
                        {
                            if (!folder.SettingsDenyDownload)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (room is not { SettingsDenyDownload: true })
                            {
                                return true;
                            }
                        }
                    }

                    if (action is FilesSecurityActions.Read or FilesSecurityActions.Copy)
                    {
                        return true;
                    }

                    if (isRoom && action is FilesSecurityActions.Move or FilesSecurityActions.Delete or FilesSecurityActions.IndexExport)
                    {
                        return true;
                    }
                }

                if (await HasFullAccessAsync(e, userId, isGuest, isRoom, isUser))
                {
                    return true;
                }
                break;
            case FolderType.ThirdpartyBackup:
                if (isDocSpaceAdmin)
                {
                    return true;
                }
                break;
        }

        var ace = e.ShareRecord;

        if (ace == null)
        {
            var cachedRecords = GetCachedRecords<T>();
            if (((!isRoom && e.RootFolderType is FolderType.VirtualRooms or FolderType.RoomTemplates or FolderType.Archive &&
                 cachedRecords.TryGetValue(GetCacheKey(e.ParentId, userId), out var value)) ||
                cachedRecords.TryGetValue(GetCacheKey(e.ParentId, await externalShare.GetLinkIdAsync()), out value)) &&
                value != null)
            {
                ace = value.Clone();
                ace.EntryId = e.Id;
            }
            else
            {
                var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
                if (e.RootFolderType != FolderType.RoomTemplates || userType == EmployeeType.RoomAdmin || userType == EmployeeType.DocSpaceAdmin)
                {
                    ace = await GetCurrentShareAsync(e, userId, isDocSpaceAdmin, shares);

                    if (e.RootFolderType is FolderType.VirtualRooms or FolderType.RoomTemplates or FolderType.Archive &&
                        ace is { SubjectType: SubjectType.User or SubjectType.ExternalLink or SubjectType.PrimaryExternalLink })
                    {
                        var id = ace.SubjectType is SubjectType.ExternalLink or SubjectType.PrimaryExternalLink ? ace.Subject : userId;

                        cachedRecords.TryAdd(GetCacheKey(e.ParentId, id), ace);
                    }
                }
            }
        }

        var defaultShare =
            e.RootFolderType switch
            {
                FolderType.VirtualRooms => DefaultVirtualRoomsShare,
                FolderType.USER => DefaultMyShare,
                FolderType.Privacy => DefaultPrivacyShare,
                FolderType.Archive => DefaultArchiveShare,
                FolderType.RoomTemplates => DefaultRoomTemplatesShare,
                _ => DefaultCommonShare
            };

        e.ShareRecord = ace;
        e.Access = ace?.Share ?? defaultShare;
        e.Access = e.RootFolderType is FolderType.ThirdpartyBackup ? FileShare.Restrict : e.Access;

        if (file != null)
        {
            var fileType = FileUtility.GetFileTypeByFileName(file.Title);
            if (fileType is FileType.Pdf or FileType.Spreadsheet)
            {
                if (parentFolders.Exists(parent => parent.FolderType is FolderType.ReadyFormFolder or FolderType.InProcessFormFolder))
                {
                    if (ace is { Share: FileShare.FillForms } && !userId.Equals(file.CreateBy))
                    {
                        return false;
                    }
                }
            }
        }

        if (ace is { SubjectType: SubjectType.ExternalLink or SubjectType.PrimaryExternalLink } && ace.Subject != userId &&
            await externalShare.ValidateRecordAsync(ace, null, isAuthenticated, e) != Status.Ok)
        {
            return false;
        }

        switch (action)
        {
            case FilesSecurityActions.Read:
            case FilesSecurityActions.Pin:
            case FilesSecurityActions.Mute:
            case FilesSecurityActions.CopyLink:
                if (e is Folder<T> { FolderType: FolderType.Knowledge, Access: FileShare.Read or FileShare.None })
                {
                    return false;
                }
                
                return e.Access != FileShare.Restrict;
            case FilesSecurityActions.Comment:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access is FileShare.Editing or FileShare.Comment or FileShare.Review or FileShare.CustomFilter)
                        {
                            return true;
                        }

                        break;
                    default:
                        if (e.Access is FileShare.Comment or FileShare.Review or FileShare.RoomManager or FileShare.Editing or FileShare.FillForms or FileShare.ContentCreator)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.FillForms:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access is FileShare.FillForms)
                        {
                            return true;
                        }

                        break;
                    default:
                        if (e.Access is FileShare.FillForms or FileShare.RoomManager or FileShare.Editing or FileShare.ContentCreator)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.Review:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access is FileShare.Editing or FileShare.Review)
                        {
                            return true;
                        }

                        break;
                    default:
                        if (e.Access is FileShare.Review or FileShare.RoomManager or FileShare.Editing or FileShare.ContentCreator)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.Convert:
            case FilesSecurityActions.Create:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return e.Access == FileShare.ReadWrite;
                    default:
                        if (e.Access is FileShare.RoomManager or FileShare.ContentCreator)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.Edit:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access is FileShare.Editing or FileShare.ReadWrite)
                        {
                            return true;
                        }

                        break;
                    default:
                        if (isRoom)
                        {
                            if (e.Access is FileShare.RoomManager)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (e.Access is FileShare.RoomManager or FileShare.ContentCreator || (e.Access is FileShare.Editing && !MustConvert(e)))
                            {
                                return true;
                            }
                        }
                        break;
                }

                break;
            case FilesSecurityActions.Delete:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access != FileShare.ReadWrite)
                        {
                            return false;
                        }
                        switch (e)
                        {
                            case File<T> file1:
                                return !Equals(default(T), file1.Id) && ace.Level > -1;
                            case Folder<T> folder1:
                                return !Equals(default(T), folder1.Id) && ace.Level > 0;
                        }
                        break;
                    default:
                        if (e.Access == FileShare.RoomManager ||
                            (e.Access == FileShare.ContentCreator && e.CreateBy == authContext.CurrentAccount.ID))
                        {
                            if (file is { RootFolderType: FolderType.VirtualRooms })
                            {
                                return true;
                            }

                            if (folder is
                                {
                                    RootFolderType: FolderType.VirtualRooms,
                                    FolderType: FolderType.DEFAULT or FolderType.FormFillingFolderDone or FolderType.FormFillingFolderInProgress
                                })
                            {
                                return true;
                            }
                        }
                        else if (file != null && e.Access == FileShare.FillForms && e.CreateBy == userId)
                        {
                            var folderDao = daoFactory.GetFolderDao<T>();
                            var parentFolder = await folderDao.GetFolderAsync(file.ParentId);

                            if (parentFolder.FolderType is FolderType.FormFillingFolderDone or FolderType.FormFillingFolderInProgress)
                            {
                                return true;
                            }
                        }

                        break;
                }

                break;
            case FilesSecurityActions.CustomFilter:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access is FileShare.Editing or FileShare.CustomFilter or FileShare.ReadWrite)
                        {
                            return true;
                        }

                        break;
                    default:
                        if (e.Access is FileShare.RoomManager or FileShare.ContentCreator || (e.Access is FileShare.Editing && !MustConvert(e)))
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.EditRoom:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                        if (e.Access == FileShare.RoomManager)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.Rename:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return e.Access == FileShare.ReadWrite;
                    default:
                        if (e.Access == FileShare.RoomManager ||
                            (e.Access == FileShare.ContentCreator && e.CreateBy == authContext.CurrentAccount.ID))
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.ReadHistory:
                if (!isAuthenticated)
                {
                    return false;
                }

                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access != FileShare.Restrict)
                        {
                            return true;
                        }

                        break;
                    default:
                        if (e.Access is FileShare.RoomManager or FileShare.Editing or FileShare.ContentCreator)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.Lock:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                        if (e.Access is FileShare.RoomManager)
                        {
                            return true;
                        }

                        if (e.Access is FileShare.ContentCreator)
                        {
                            var tagDao = daoFactory.GetTagDao<T>();
                            var tagLocked = await tagDao.GetTagsAsync(file.Id, FileEntryType.File, TagType.Locked).FirstOrDefaultAsync();

                            return tagLocked == null || tagLocked.Owner == authContext.CurrentAccount.ID;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.EditHistory:
                if (file is { Encrypted: true })
                {
                    return false;
                }

                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access == FileShare.Editing && isAuthenticated)
                        {
                            return true;
                        }

                        break;
                    default:
                        if (e.Access is FileShare.RoomManager or FileShare.ContentCreator)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.MoveTo:
            case FilesSecurityActions.CopyTo:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return e.Access == FileShare.ReadWrite;

                    default:
                        if (e.Access is FileShare.RoomManager or FileShare.ContentCreator)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.Copy:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access != FileShare.Restrict && isAuthenticated && !isGuest && ace?.Options is not { DenyDownload: true })
                        {
                            return true;
                        }

                        break;
                    default:
                        if (ace is { SubjectType: SubjectType.ExternalLink or SubjectType.PrimaryExternalLink } && ace.Subject != userId)
                        {
                            return false;
                        }

                        if (isRoom)
                        {
                            if (!(isDocSpaceAdmin && (!folder.SettingsDenyDownload || e.Access is not (FileShare.Restrict or FileShare.Read or FileShare.None))))
                            {
                                break;
                            }
                        }

                        return true;
                }

                break;
            case FilesSecurityActions.Duplicate:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                        if (e.Access == FileShare.RoomManager ||
                            (e.Access == FileShare.ContentCreator && e.CreateBy == authContext.CurrentAccount.ID))
                        {
                            return true;
                        }

                        if (isDocSpaceAdmin && isRoom && (!folder.SettingsDenyDownload || e.Access is not (FileShare.Restrict or FileShare.Read or FileShare.None)))
                        {
                            return true;
                        }
                        break;
                }
                break;
            case FilesSecurityActions.EditExpiration:
            case FilesSecurityActions.EditInternal:
            case FilesSecurityActions.EditAccess:
            case FilesSecurityActions.ReadLinks:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return e.Access == FileShare.ReadWrite;
                    default:
                        if (e.Access == FileShare.RoomManager)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.Move:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access != FileShare.ReadWrite)
                        {
                            return false;
                        }

                        switch (e)
                        {
                            case File<T> file1:
                                return !Equals(default(T), file1.Id) && ace.Level > -1;
                            case Folder<T> folder1:
                                return !Equals(default(T), folder1.Id) && ace.Level > 0;
                        }
                        break;
                    default:
                        if ((e.Access == FileShare.RoomManager ||
                             (e.Access == FileShare.ContentCreator && e.CreateBy == authContext.CurrentAccount.ID))
                            && !isRoom)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.SubmitToFormGallery:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                }

                break;
            case FilesSecurityActions.Download:
                if (e.Access == FileShare.Restrict || ace?.Options is { DenyDownload: true })
                {
                    return false;
                }

                if (e.RootFolderType == FolderType.USER || (e.Access != FileShare.Read && e.Access != FileShare.None))
                {
                    return true;
                }

                if (e.RootFolderType == FolderType.RoomTemplates)
                {
                    return true;
                }

                if (isRoom)
                {
                    return !folder.SettingsDenyDownload;
                }

                return room is not { SettingsDenyDownload: true };
            case FilesSecurityActions.CopySharedLink:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                        if (e.Access == FileShare.RoomManager || (e.Access != FileShare.Restrict && e.Shared && ace is { IsLink: true }))
                        {
                            return true;
                        }

                        break;
                }

                break;

            case FilesSecurityActions.Embed:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                        if (e.Access == FileShare.RoomManager && ((isRoom && e.FullShared) || file is { FullShared: true }))
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.IndexExport:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                        if (e.Access == FileShare.RoomManager)
                        {
                            return true;
                        }

                        break;
                }

                break;
            case FilesSecurityActions.Vectorization:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                        if (e.Access == FileShare.RoomManager && file is { VectorizationStatus: VectorizationStatus.Failed })
                        {
                            return true;
                        }
                        
                        break;
                }
                break;
            case FilesSecurityActions.AscAi:
                return e.Access != FileShare.Restrict;
            case FilesSecurityActions.UseChat:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                        if (e.Access is FileShare.RoomManager or FileShare.ContentCreator)
                        {
                            return true;
                        }
                        
                        break;
                }
                break;
        }

        if (e.Access != FileShare.Restrict &&
            e.CreateBy == userId &&
            (e.FileEntryType == FileEntryType.File || folder.FolderType != FolderType.COMMON) &&
            e.RootFolderType != FolderType.Archive && e.RootFolderType != FolderType.RoomTemplates && e.RootFolderType != FolderType.VirtualRooms)
        {
            return true;
        }

        if (e.CreateBy == userId)
        {
            e.Access = FileShare.None; //HACK: for client
        }

        return false;

        bool MustConvert(FileEntry entry)
        {
            return entry.FileEntryType == FileEntryType.File && fileUtility.MustConvert(entry.Title);
        }
    }

    public async Task<FileShareRecord<T>> GetCurrentShareAsync<T>(FileEntry<T> entry, Guid userId, bool isDocSpaceAdmin, IEnumerable<FileShareRecord<T>> shares = null)
    {
        if (entry is Folder<T> { FolderType: FolderType.VirtualRooms or FolderType.Archive })
        {
            return null;
        }

        FileShareRecord<T> ace;
        var orderedSubjects = new List<OrderedSubject>();
        if (shares == null)
        {
            var includeAvailableLinks = entry switch
            {
                { RootFolderType: FolderType.USER } when entry.CreateBy != userId => true,
                { RootFolderType: FolderType.VirtualRooms } when !isDocSpaceAdmin => true,
                _ => false
            };

            orderedSubjects = await GetUserOrderedSubjectsAsync(userId, includeAvailableLinks);
            shares = await GetSharesAsync(entry, orderedSubjects.Select(s => s.Subject));
        }

        if (entry.FileEntryType == FileEntryType.File)
        {
            ace = shares
                .OrderBy(r => r, new OrderedSubjectComparer<T>(orderedSubjects))
                .ThenByDescending(r => r.Share, new FileShareRecord<T>.ShareComparer(entry.RootFolderType))
                .FirstOrDefault(r => Equals(r.EntryId, entry.Id) && r.EntryType == FileEntryType.File);

            if (ace == null)
            {
                // share on parent folders
                ace = shares.Where(r => Equals(r.EntryId, entry.ParentId) && r.EntryType == FileEntryType.Folder)
                    .OrderBy(r => r, new OrderedSubjectComparer<T>(orderedSubjects))
                    .ThenBy(r => r.Level)
                    .ThenBy(r => r.Share, new FileShareRecord<T>.ShareComparer(entry.RootFolderType))
                    .FirstOrDefault();
            }
        }
        else
        {
            ace = shares.Where(r => Equals(r.EntryId, entry.Id) && r.EntryType == FileEntryType.Folder)
                .OrderBy(r => r, new OrderedSubjectComparer<T>(orderedSubjects))
                .ThenBy(r => r.Level)
                .ThenBy(r => r.Share, new FileShareRecord<T>.ShareComparer(entry.RootFolderType))
                .FirstOrDefault();
        }

        return ace;
    }

    public async Task ShareAsync<T>(T entryId, FileEntryType entryType, Guid @for, FileShare share, SubjectType subjectType = default, FileShareOptions options = null, Guid? owner = null)
    {
        var securityDao = daoFactory.GetSecurityDao<T>();
        var r = new FileShareRecord<T>
        {
            TenantId = tenantManager.GetCurrentTenantId(),
            EntryId = entryId,
            EntryType = entryType,
            Subject = @for,
            Owner = owner ?? authContext.CurrentAccount.ID,
            Share = share,
            SubjectType = subjectType,
            Options = options
        };

        await securityDao.SetShareAsync(r);
    }

    public async Task<IEnumerable<FileShareRecord<T>>> GetSharesAsync<T>(FileEntry<T> entry, IEnumerable<Guid> subjects = null)
    {
        return await daoFactory.GetSecurityDao<T>().GetSharesAsync(entry, subjects);
    }

    public IAsyncEnumerable<FileShareRecord<T>> GetPureSharesAsync<T>(FileEntry<T> entry, IEnumerable<Guid> subjects)
    {
        return daoFactory.GetSecurityDao<T>().GetPureSharesAsync(entry, subjects);
    }

    public Task<bool> IsPublicAsync<T>(FileEntry<T> entry)
    {
        return daoFactory.GetSecurityDao<T>().IsPublicAsync(entry);
    }

    public IAsyncEnumerable<FileShareRecord<T>> GetPureSharesAsync<T>(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text, int offset = 0, int count = -1)
    {
        return daoFactory.GetSecurityDao<T>().GetPureSharesAsync(entry, filterType, status, text, offset, count);
    }

    public Task<int> GetPureSharesCountAsync<T>(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text)
    {
        return daoFactory.GetSecurityDao<T>().GetPureSharesCountAsync(entry, filterType, status, text);
    }

    public async IAsyncEnumerable<FileEntry> GetSharesForMeAsync(FilterType filterType, bool subjectGroup, Guid subjectID, string searchText = "", string[] extension = null, bool searchInContent = false, bool withSubfolders = false)
    {
        var securityDao = daoFactory.GetSecurityDao<string>();
        var orderedSubjects = await GetUserOrderedSubjectsAsync(authContext.CurrentAccount.ID, true);
        List<FileShareRecord<int>> recordsInternal = [];
        List<FileShareRecord<string>> recordsThirdParty = [];

        await foreach (var r in securityDao.GetSharesAsync(orderedSubjects.Select(s => s.Subject)))
        {
            if (int.TryParse(r.EntryId, out _))
            {
                recordsInternal.Add(r.MapToFileShareRecordInternal());
            }
            else
            {
                recordsThirdParty.Add(r);
            }
        }


        var firstTask = GetSharesForMeAsync(recordsInternal, orderedSubjects, filterType, subjectGroup, subjectID, searchText, extension, searchInContent, withSubfolders).ToListAsync();
        var secondTask = GetSharesForMeAsync(recordsThirdParty, orderedSubjects, filterType, subjectGroup, subjectID, searchText, extension, searchInContent, withSubfolders).ToListAsync();

        foreach (var items in await Task.WhenAll(firstTask.AsTask(), secondTask.AsTask()))
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }
    }

    public async Task<List<FileEntry>> GetVirtualRoomsAsync(
        IEnumerable<FilterType> filterTypes,
        Guid subjectId,
        string searchText,
        bool searchInContent,
        bool withSubfolders,
        SearchArea searchArea,
        bool withoutTags,
        IEnumerable<string> tagNames,
        bool excludeSubject,
        ProviderFilter provider,
        SubjectFilter subjectFilter,
        QuotaFilter quotaFilter,
        StorageFilter storageFilter)
    {
        var securityDao = daoFactory.GetSecurityDao<string>();

        var subjectEntries = subjectFilter is SubjectFilter.Member
            ? await securityDao.GetSharesAsync([subjectId]).Where(r => r.EntryType == FileEntryType.Folder).Select(r => r.EntryId.ToString()).ToListAsync()
            : null;

        var isAdmin = await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID);

        List<OrderedSubject> currentUserOrderedSubjects = [];
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

        if (searchArea != SearchArea.Templates || userType == EmployeeType.RoomAdmin || userType == EmployeeType.DocSpaceAdmin)
        {
            currentUserOrderedSubjects = await GetUserOrderedSubjectsAsync(authContext.CurrentAccount.ID, searchArea is SearchArea.Active or SearchArea.Any && !isAdmin);
        }

        var currentUsersRecords = await securityDao.GetSharesAsync(currentUserOrderedSubjects.Select(x => x.Subject))
            .Where(x => x.EntryType == FileEntryType.Folder)
            .ToListAsync();

        var internalRoomsRecords = new Dictionary<int, FileShareRecord<int>>();
        var thirdPartyRoomsRecords = new Dictionary<string, FileShareRecord<string>>();

        var recordGroup = currentUsersRecords.GroupBy(r => new { r.EntryId, r.EntryType }, (_, group) => new
        {
            firstRecord = group.OrderBy(r => r, new OrderedSubjectComparer<string>(currentUserOrderedSubjects))
                .ThenByDescending(r => r.Share, new FileShareRecord<string>.ShareComparer(FolderType.VirtualRooms))
                .First()
        });

        foreach (var record in recordGroup.Select(r => r.firstRecord))
        {
            if (int.TryParse(record.EntryId, out var roomId))
            {
                internalRoomsRecords.TryAdd(roomId, new FileShareRecord<int>
                {
                    TenantId = record.TenantId,
                    EntryId = roomId,
                    EntryType = record.EntryType,
                    SubjectType = record.SubjectType,
                    Subject = record.Subject,
                    Owner = record.Owner,
                    Share = record.Share,
                    Options = record.Options,
                    Level = record.Level
                });
            }
            else
            {
                thirdPartyRoomsRecords.TryAdd(record.EntryId, record);
            }
        }

        if (isAdmin && searchArea != SearchArea.Templates)
        {
            return await GetAllVirtualRoomsAsync(filterTypes, subjectId, searchText, searchInContent, withSubfolders, searchArea, withoutTags, tagNames, excludeSubject, provider,
                subjectFilter, subjectEntries, quotaFilter, storageFilter, internalRoomsRecords, thirdPartyRoomsRecords);
        }

        return await GetVirtualRoomsForMeAsync(filterTypes, subjectId, searchText, searchInContent, withSubfolders, searchArea, withoutTags, tagNames, excludeSubject, provider,
            subjectFilter, subjectEntries, storageFilter, internalRoomsRecords, thirdPartyRoomsRecords);
    }

    private async Task<List<FileEntry>> GetAllVirtualRoomsAsync(
        IEnumerable<FilterType> filterTypes,
        Guid subjectId,
        string search,
        bool searchInContent,
        bool withSubfolders,
        SearchArea searchArea,
        bool withoutTags,
        IEnumerable<string> tagNames,
        bool excludeSubject,
        ProviderFilter provider,
        SubjectFilter subjectFilter,
        IEnumerable<string> subjectEntries,
        QuotaFilter quotaFilter,
        StorageFilter storageFilter,
        Dictionary<int, FileShareRecord<int>> internalRecords,
        Dictionary<string, FileShareRecord<string>> thirdPartyRecords)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var folderThirdPartyDao = daoFactory.GetFolderDao<string>();
        var fileDao = daoFactory.GetFileDao<int>();
        var fileThirdPartyDao = daoFactory.GetFileDao<string>();
        var entries = new List<FileEntry>();

        var rootFoldersIds = searchArea switch
        {
            SearchArea.Active => [await globalFolder.GetFolderVirtualRoomsAsync(daoFactory)],
            SearchArea.Archive => [await globalFolder.GetFolderArchiveAsync(daoFactory)],
            SearchArea.Templates => [await globalFolder.GetFolderRoomTemplatesAsync(daoFactory)],
            SearchArea.AiAgents => [await globalFolder.GetFolderAiAgentsAsync(daoFactory)],
            _ => new[] { await globalFolder.GetFolderVirtualRoomsAsync(daoFactory), await globalFolder.GetFolderArchiveAsync(daoFactory) }
        };

        var roomsEntries = storageFilter == StorageFilter.ThirdParty ?
            [] :
            await folderDao.GetRoomsAsync(rootFoldersIds, filterTypes, tagNames, subjectId, search, withSubfolders, withoutTags, excludeSubject, provider, subjectFilter, subjectEntries, quotaFilter)
                .Where(r => withSubfolders || DocSpaceHelper.IsRoom(r.FolderType))
                .ToListAsync();

        var thirdPartyRoomsEntries = storageFilter == StorageFilter.Internal ?
            [] :
            await folderThirdPartyDao.GetProviderBasedRoomsAsync(searchArea, filterTypes, tagNames, subjectId, search, withoutTags, excludeSubject, provider, subjectFilter, subjectEntries)
                .Where(r => withSubfolders || DocSpaceHelper.IsRoom(r.FolderType))
                .Distinct()
                .ToListAsync();

        entries.AddRange(roomsEntries.Select(x => SetRecord(x, internalRecords)));
        entries.AddRange(thirdPartyRoomsEntries.Select(x => SetRecord(x, thirdPartyRecords)));

        if (withSubfolders && (filterTypes == null || !filterTypes.Contains(FilterType.FoldersOnly)))
        {
            List<File<int>> files;
            List<File<string>> thirdPartyFiles;

            if (!string.IsNullOrEmpty(search))
            {
                files = await fileDao.GetFilesAsync(roomsEntries.Select(r => r.Id), FilterType.None, false, Guid.Empty, search, null, searchInContent).ToListAsync();
                thirdPartyFiles = await fileThirdPartyDao.GetFilesAsync(thirdPartyRoomsEntries.Select(f => f.Id), FilterType.None, false, Guid.Empty, search, null, searchInContent).ToListAsync();
            }
            else
            {
                files = await fileDao.GetFilesAsync(roomsEntries.Where(r => DocSpaceHelper.IsRoom(r.FolderType)).Select(r => r.Id), FilterType.None, false, Guid.Empty, search, null, searchInContent).ToListAsync();
                thirdPartyFiles = await fileThirdPartyDao.GetFilesAsync(thirdPartyRoomsEntries.Select(r => r.Id), FilterType.None, false, Guid.Empty, search, null, searchInContent).ToListAsync();
            }

            entries.AddRange(files);
            entries.AddRange(thirdPartyFiles);
        }

        var t1 = SetTagsAsync(roomsEntries);
        var t2 = SetTagsAsync(thirdPartyRoomsEntries);
        var t3 = SetPinAsync(roomsEntries);
        var t4 = SetPinAsync(thirdPartyRoomsEntries);

        await Task.WhenAll(t1, t2, t3, t4);

        return entries;

        Folder<T> SetRecord<T>(Folder<T> folder, IReadOnlyDictionary<T, FileShareRecord<T>> records)
        {
            if (records.TryGetValue(folder.Id, out var record))
            {
                folder.ShareRecord = record;
                folder.Access = record.Share;
            }
            else
            {
                folder.Access = FileShare.None;
                folder.ShareRecord = new FileShareRecord<T>
                {
                    EntryId = folder.Id,
                    EntryType = FileEntryType.Folder,
                    Share = FileShare.None,
                    SubjectType = SubjectType.User,
                    Subject = authContext.CurrentAccount.ID
                };
            }

            return folder;
        }
    }


    private async Task<List<FileEntry>> GetVirtualRoomsForMeAsync(
        IEnumerable<FilterType> filterTypes,
        Guid subjectId,
        string search,
        bool searchInContent,
        bool withSubfolders,
        SearchArea searchArea,
        bool withoutTags,
        IEnumerable<string> tagNames,
        bool excludeSubject,
        ProviderFilter provider,
        SubjectFilter subjectFilter,
        IEnumerable<string> subjectEntries,
        StorageFilter storageFilter,
        Dictionary<int, FileShareRecord<int>> internalRecords,
        Dictionary<string, FileShareRecord<string>> thirdPartyRecords)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var folderThirdPartyDao = daoFactory.GetFolderDao<string>();
        var fileDao = daoFactory.GetFileDao<int>();
        var thirdPartyFileDao = daoFactory.GetFileDao<string>();
        var entries = new List<FileEntry>();

        var rootFoldersIds = searchArea switch
        {
            SearchArea.Active => [await globalFolder.GetFolderVirtualRoomsAsync(daoFactory)],
            SearchArea.Archive => [await globalFolder.GetFolderArchiveAsync(daoFactory)],
            SearchArea.Templates => [await globalFolder.GetFolderRoomTemplatesAsync(daoFactory)],
            SearchArea.AiAgents => [await globalFolder.GetFolderAiAgentsAsync(daoFactory)],
            _ => new[] { await globalFolder.GetFolderVirtualRoomsAsync(daoFactory), await globalFolder.GetFolderArchiveAsync(daoFactory) }
        };

        var rooms = storageFilter == StorageFilter.ThirdParty
            ? []
            : await folderDao.GetRoomsAsync(internalRecords.Keys, filterTypes, tagNames, subjectId, search, withSubfolders, withoutTags, excludeSubject, provider, subjectFilter, subjectEntries, rootFoldersIds)
                .Where(r => withSubfolders || DocSpaceHelper.IsRoom(r.FolderType))
                .Where(r => Filter(r, internalRecords))
                .ToListAsync();

        var thirdPartyRooms = storageFilter == StorageFilter.Internal
            ? []
            : await folderThirdPartyDao.GetProviderBasedRoomsAsync(searchArea, thirdPartyRecords.Keys, filterTypes, tagNames, subjectId, search, withoutTags, excludeSubject, provider, subjectFilter, subjectEntries)
                .Where(r => withSubfolders || DocSpaceHelper.IsRoom(r.FolderType))
                .Where(r => Filter(r, thirdPartyRecords))
                .Distinct()
                .ToListAsync();

        if (withSubfolders && (filterTypes == null || !filterTypes.Contains(FilterType.FoldersOnly)))
        {
            var files = await fileDao.GetFilesAsync(rooms.Select(r => r.Id), FilterType.None, false, Guid.Empty, search, null, searchInContent).ToListAsync();
            var thirdPartyFiles = await thirdPartyFileDao.GetFilesAsync(thirdPartyRooms.Select(r => r.Id), FilterType.None, false, Guid.Empty, search, null, searchInContent)
                .ToListAsync();

            entries.AddRange(files.Where(f => Filter(f, internalRecords)));
            entries.AddRange(thirdPartyFiles.Where(f => Filter(f, thirdPartyRecords)));
        }

        var t1 = SetTagsAsync(rooms);
        var t2 = SetTagsAsync(thirdPartyRooms);
        var t3 = SetPinAsync(rooms);
        var t4 = SetPinAsync(thirdPartyRooms);

        await Task.WhenAll(t1, t2, t3, t4);

        entries.AddRange(rooms);
        entries.AddRange(thirdPartyRooms);

        return entries;

        bool Filter<T>(FileEntry<T> entry, IReadOnlyDictionary<T, FileShareRecord<T>> records)
        {
            var id = entry.FileEntryType == FileEntryType.Folder ? entry.Id : entry.ParentId;
            var record = records.GetValueOrDefault(id);

            switch (searchArea)
            {
                case SearchArea.Archive when entry.RootFolderType == FolderType.Archive:
                case SearchArea.Templates when entry.RootFolderType == FolderType.RoomTemplates:
                    {
                        if (entry.CreateBy != authContext.CurrentAccount.ID)
                        {
                            entry.ShareRecord = record;
                            entry.Access = record?.Share ?? FileShare.None;
                        }
                        return true;
                    }
                case SearchArea.Active when entry.RootFolderType == FolderType.VirtualRooms:
                case SearchArea.Any when entry.RootFolderType is FolderType.VirtualRooms or FolderType.Archive or FolderType.AiAgents:
                case SearchArea.AiAgents when entry.RootFolderType == FolderType.AiAgents:
                    {
                        entry.ShareRecord = record;
                        entry.Access = record?.Share ?? FileShare.None;
                        return true;
                    }
                default:
                    return false;
            }
        }
    }

    private async Task SetTagsAsync<T>(List<Folder<T>> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tags = await tagDao.GetTagsAsync([TagType.Custom], entries).ToLookupAsync(f => (T)f.EntryId);

        foreach (var room in entries)
        {
            room.Tags = tags[room.Id];
        }
    }

    private async Task SetPinAsync<T>(List<Folder<T>> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tags = await tagDao.GetTagsAsync(authContext.CurrentAccount.ID, [TagType.Pin], entries).ToDictionaryAsync(t => (T)t.EntryId);

        foreach (var fileEntry in entries.Where(e => e.FileEntryType == FileEntryType.Folder))
        {
            if (tags.ContainsKey(fileEntry.Id))
            {
                fileEntry.Pinned = true;
            }
        }
    }

    private async IAsyncEnumerable<FileEntry> GetSharesForMeAsync<T>(IEnumerable<FileShareRecord<T>> records, List<OrderedSubject> orderedSubjects, FilterType filterType, bool subjectGroup,
        Guid subjectID, string searchText = "", string[] extension = null, bool searchInContent = false, bool withSubfolders = false)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var securityDao = daoFactory.GetSecurityDao<T>();

        var fileIds = new Dictionary<T, (FileShare, Guid)>();
        var folderIds = new Dictionary<T, (FileShare, Guid)>();

        var recordGroup = records.GroupBy(r => new { r.EntryId, r.EntryType }, (_, group) => new
        {
            firstRecord = group.OrderBy(r => r, new OrderedSubjectComparer<T>(orderedSubjects))
                .ThenByDescending(r => r.Share, new FileShareRecord<T>.ShareComparer(FolderType.SHARE))
                .First()
        });

        foreach (var r in recordGroup.Select(r => r.firstRecord).Where(r => r.Share != FileShare.Restrict))
        {
            if (r.EntryType == FileEntryType.Folder)
            {
                if (!folderIds.ContainsKey(r.EntryId))
                {
                    folderIds.Add(r.EntryId, (r.Share, r.Owner));
                }
            }
            else
            {
                if (!fileIds.ContainsKey(r.EntryId))
                {
                    fileIds.Add(r.EntryId, (r.Share, r.Owner));
                }
            }
        }

        var entries = new List<FileEntry<T>>();

        if (filterType != FilterType.FoldersOnly)
        {
            var files = fileDao.GetFilesFilteredAsync(fileIds.Keys.ToArray(), filterType, subjectGroup, subjectID, searchText, extension, searchInContent);
            var share = await globalFolder.GetFolderShareAsync<T>(daoFactory);

            await foreach (var x in files)
            {
                if (fileIds.TryGetValue(x.Id, out var tuple))
                {
                    x.Access = tuple.Item1;
                    x.SharedBy = tuple.Item2;
                    x.FolderIdDisplay = share;
                }

                entries.Add(x);
            }
        }

        if (filterType is FilterType.None or FilterType.FoldersOnly)
        {
            IAsyncEnumerable<FileEntry<T>> folders = folderDao.GetFoldersAsync(folderIds.Keys, filterType, subjectGroup, subjectID, searchText, withSubfolders && filterType == FilterType.FoldersOnly, false);

            if (withSubfolders)
            {
                folders = FilterReadAsync(folders);
            }

            var share = await globalFolder.GetFolderShareAsync<T>(daoFactory);

            await foreach (var folder in folders)
            {
                if (folderIds.TryGetValue(folder.Id, out var access))
                {
                    folder.Access = access.Item1;
                    folder.SharedBy = access.Item2;
                    folder.FolderIdDisplay = share;
                }

                entries.Add(folder);
            }
        }

        if (filterType != FilterType.FoldersOnly && filterType != FilterType.None && withSubfolders)
        {
            IAsyncEnumerable<FileEntry<T>> filesInSharedFolders = fileDao.GetFilesAsync(folderIds.Keys, filterType, subjectGroup, subjectID, searchText, extension, searchInContent);
            filesInSharedFolders = FilterReadAsync(filesInSharedFolders);
            entries.AddRange(await filesInSharedFolders.Distinct().ToListAsync());
        }

        var data = entries.Where(f =>
                f.RootFolderType is FolderType.USER or FolderType.VirtualRooms &&
                (f is File<T> || f is Folder<T> folder && !DocSpaceHelper.IsRoom(folder.FolderType)) &&
                f.RootCreateBy != authContext.CurrentAccount.ID
        );

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            data = data.Where(r => !r.ProviderEntry);
        }

        var failedEntries = entries.Where(x => !string.IsNullOrEmpty(x.Error));
        var failedRecords = new List<FileShareRecord<T>>();

        foreach (var failedEntry in failedEntries)
        {
            var entryType = failedEntry.FileEntryType;

            var failedRecord = records.First(x => x.EntryId.Equals(failedEntry.Id) && x.EntryType == entryType);

            failedRecord.Share = FileShare.None;

            failedRecords.Add(failedRecord);
        }

        if (failedRecords.Count > 0)
        {
            await securityDao.DeleteShareRecordsAsync(failedRecords);
        }

        data = data.Where(x => string.IsNullOrEmpty(x.Error));

        foreach (var e in data)
        {
            yield return e;
        }
    }

    public async IAsyncEnumerable<FileEntry> GetPrivacyForMeAsync(FilterType filterType, bool subjectGroup, Guid subjectID, string searchText = "", string[] extension = null, bool searchInContent = false, bool withSubfolders = false)
    {
        var securityDao = daoFactory.GetSecurityDao<string>();
        var orderedSubjects = await GetUserOrderedSubjectsAsync(authContext.CurrentAccount.ID);
        var records = await securityDao.GetSharesAsync(orderedSubjects.Select(x => x.Subject)).ToListAsync();
        List<FileShareRecord<int>> internalRecords = [];
        List<FileShareRecord<string>> thirdPartyRecords = [];

        foreach (var record in records)
        {
            if (int.TryParse(record.EntryId, out var eId))
            {
                internalRecords.Add(new FileShareRecord<int>
                {
                    TenantId = record.TenantId,
                    EntryId = eId,
                    EntryType = record.EntryType,
                    SubjectType = record.SubjectType,
                    Subject = record.Subject,
                    Owner = record.Owner,
                    Share = record.Share,
                    Options = record.Options,
                    Level = record.Level
                });
            }
            else
            {
                thirdPartyRecords.Add(record);
            }
        }

        await foreach (var e in GetPrivacyForMeAsync(internalRecords, orderedSubjects, filterType, subjectGroup, subjectID, searchText, extension, searchInContent, withSubfolders))
        {
            yield return e;
        }

        await foreach (var e in GetPrivacyForMeAsync(thirdPartyRecords, orderedSubjects, filterType, subjectGroup, subjectID, searchText, extension, searchInContent, withSubfolders))
        {
            yield return e;
        }
    }

    private async IAsyncEnumerable<FileEntry<T>> GetPrivacyForMeAsync<T>(IEnumerable<FileShareRecord<T>> records, List<OrderedSubject> orderedSubjects, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText = "", string[] extension = null, bool searchInContent = false, bool withSubfolders = false)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var fileIds = new Dictionary<T, FileShare>();
        var folderIds = new Dictionary<T, FileShare>();

        var recordGroup = records.GroupBy(r => new { r.EntryId, r.EntryType }, (_, group) => new
        {
            firstRecord = group.OrderBy(r => r, new OrderedSubjectComparer<T>(orderedSubjects))
                .ThenByDescending(r => r.Share, new FileShareRecord<T>.ShareComparer(FolderType.Privacy))
                .First()
        });

        foreach (var r in recordGroup.Select(r => r.firstRecord).Where(r => r.Share != FileShare.Restrict))
        {
            if (r.EntryType == FileEntryType.Folder)
            {
                if (!folderIds.ContainsKey(r.EntryId))
                {
                    folderIds.Add(r.EntryId, r.Share);
                }
            }
            else
            {
                if (!fileIds.ContainsKey(r.EntryId))
                {
                    fileIds.Add(r.EntryId, r.Share);
                }
            }
        }

        var entries = new List<FileEntry<T>>();

        if (filterType != FilterType.FoldersOnly)
        {
            var files = fileDao.GetFilesFilteredAsync(fileIds.Keys.ToArray(), filterType, subjectGroup, subjectID, searchText, extension, searchInContent);
            var privateFolder = await globalFolder.GetFolderPrivacyAsync<T>(daoFactory);

            await foreach (var x in files)
            {
                if (fileIds.TryGetValue(x.Id, out var access))
                {
                    x.Access = access;
                    x.FolderIdDisplay = privateFolder;
                }

                entries.Add(x);
            }
        }

        if (filterType is FilterType.None or FilterType.FoldersOnly)
        {
            IAsyncEnumerable<FileEntry<T>> folders = folderDao.GetFoldersAsync(folderIds.Keys, filterType, subjectGroup, subjectID, searchText, withSubfolders, false);

            if (withSubfolders)
            {
                folders = FilterReadAsync(folders);
            }

            var privacyFolder = await globalFolder.GetFolderPrivacyAsync<T>(daoFactory);

            await foreach (var folder in folders)
            {
                if (folderIds.TryGetValue(folder.Id, out var access))
                {
                    folder.Access = access;
                    folder.FolderIdDisplay = privacyFolder;
                }

                entries.Add(folder);
            }
        }

        if (filterType != FilterType.FoldersOnly && withSubfolders)
        {
            IAsyncEnumerable<FileEntry<T>> filesInSharedFolders = fileDao.GetFilesAsync(folderIds.Keys, filterType, subjectGroup, subjectID, searchText, extension, searchInContent);
            filesInSharedFolders = FilterReadAsync(filesInSharedFolders);
            entries.AddRange(await filesInSharedFolders.Distinct().ToListAsync());
        }

        var data = entries.Where(f =>
                f.RootFolderType == FolderType.Privacy // show users files
                && f.RootCreateBy != authContext.CurrentAccount.ID // don't show my files
        );

        foreach (var e in data)
        {
            yield return e;
        }
    }


    public Task RemoveSubjectAsync(Guid subject, bool withoutOwner)
    {
        return daoFactory.GetSecurityDao<int>().RemoveBySubjectAsync(subject, withoutOwner);
    }

    public Task RemoveSecuritiesAsync(Guid subject, Guid owner, SubjectType subjectType)
    {
        return daoFactory.GetSecurityDao<int>().RemoveSecuritiesAsync(subject, owner, subjectType);
    }

    public async Task<List<Guid>> GetUserSubjectsAsync(Guid userId, bool includeAvailableLinks = false)
    {
        return (await GetUserOrderedSubjectsAsync(userId, includeAvailableLinks)).Select(r=> r.Subject).ToList();
    }

    private async Task<List<OrderedSubject>> GetUserOrderedSubjectsAsync(Guid userId, bool includeAvailableLinks = false)
    {        
        if (string.Equals(httpContextAccessor?.HttpContext?.Request.Method, nameof(HttpMethod.Get), StringComparison.OrdinalIgnoreCase))
        {
            return await _subjects.GetOrAdd(new SubjectRecord(userId, includeAvailableLinks), s =>
                new Lazy<Task<List<OrderedSubject>>>(GetUserOrderedSubjectsAsync<int>(s.UserId, s.IncludeLinks))).Value;
        }
        
        return await GetUserOrderedSubjectsAsync<int>(userId, includeAvailableLinks);
    }

    public async IAsyncEnumerable<FileShareRecord<string>> GetUserRecordsAsync()
    {
        var securityDao = daoFactory.GetSecurityDao<string>();
        var currentUserSubjects = await GetUserSubjectsAsync(authContext.CurrentAccount.ID);

        await foreach (var record in securityDao.GetSharesAsync(currentUserSubjects))
        {
            yield return record;
        }
    }

    public static void CorrectSecurityByLockedStatus<T>(FileEntry<T> entry)
    {
        if (entry is not File<T> file || file.Security == null)
        {
            return;
        }

        if (file.LockedBy != null)
        {
            foreach (var action in _securityEntries[FileEntryType.File])
            {
                if (action != FilesSecurityActions.Read &&
                    action != FilesSecurityActions.ReadHistory &&
                    action != FilesSecurityActions.Copy &&
                    action != FilesSecurityActions.Duplicate &&
                    action != FilesSecurityActions.Lock &&
                    action != FilesSecurityActions.Download)
                {
                    file.Security[action] = false;
                }
            }
        }

        if (file.CustomFilterEnabledBy != null)
        {
            file.Security[FilesSecurityActions.Edit] = false;
        }
    }


    public async Task<IDictionary<SubjectType, IEnumerable<FileShare>>> GetAccesses<T>(File<T> file)
    {
        var result = new Dictionary<SubjectType, IEnumerable<FileShare>>();

        var mustConvert = fileUtility.MustConvert(file.Title);
        var canEdit = fileUtility.CanWebEdit(file.Title);
        var canCustomFiltering = fileUtility.CanWebCustomFilterEditing(file.Title);
        var canComment = fileUtility.CanWebComment(file.Title);
        var canReview = fileUtility.CanWebReview(file.Title);

        var parentRoomType = file.RootFolderType == FolderType.USER ? FolderType.USER : file.ParentRoomType;
        var folderId = file.ParentId;

        if (parentRoomType == null)
        {
            var room = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(folderId).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));

            if (room != null)
            {
                parentRoomType = room.FolderType;
            }
        }

        foreach (var subjectType in Enum.GetValues<SubjectType>())
        {
            if (!parentRoomType.HasValue ||
                !_availableRoomFileAccesses.TryGetValue(parentRoomType.Value, out var subjectShares) ||
                !subjectShares.TryGetValue(subjectType, out var shares))
            {
                continue;
            }

            List<FileShare> sharesToAdd = [];

            foreach (var s in shares)
            {
                if (s is FileShare.Restrict || (s is FileShare.Read && !file.IsForm))
                {
                    sharesToAdd.Add(s);
                    continue;
                }
                
                if (s is FileShare.None)
                {
                    if (file.CreateBy == authContext.CurrentAccount.ID)
                    {
                        sharesToAdd.Add(s);
                    }

                    continue;
                }

                if (mustConvert)
                {
                    continue;
                }

                switch (s)
                {
                    case FileShare.Editing when (file.IsForm && parentRoomType != FolderType.FillingFormsRoom || !file.IsForm) && canEdit:
                    case FileShare.FillForms when file.IsForm:
                    case FileShare.CustomFilter when !file.IsForm && canCustomFiltering:
                    case FileShare.Comment when !file.IsForm && canComment:
                    case FileShare.Review when !file.IsForm && canReview:
                    case FileShare.ReadWrite:
                        sharesToAdd.Add(s);
                        break;
                }
            }

            result.Add(subjectType, sharesToAdd);
        }

        return result;
    }

    public async Task<IDictionary<SubjectType, IEnumerable<FileShare>>> GetAccesses<T>(Folder<T> folder)
    {
        var result = new Dictionary<SubjectType, IEnumerable<FileShare>>();
        var isRoom = DocSpaceHelper.IsRoom(folder.FolderType);
        var room = isRoom ? folder : null;

        var parentRoomType = folder.RootFolderType == FolderType.USER ?
            FolderType.USER :
            room?.FolderType ?? folder.ParentRoomType;
        var folderId = folder.Id;

        if (parentRoomType == null)
        {
            room = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(folderId).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));

            if (room != null)
            {
                parentRoomType = room.FolderType;
            }
        }

        foreach (var subjectType in Enum.GetValues<SubjectType>())
        {
            if (!parentRoomType.HasValue ||
                !_availableRoomFileAccesses.TryGetValue(parentRoomType.Value, out var subjectShares) ||
                !subjectShares.TryGetValue(subjectType, out var shares))
            {
                continue;
            }

            List<FileShare> sharesToAdd = [];

            foreach (var s in shares.Where(r => parentRoomType == FolderType.FillingFormsRoom || r != FileShare.FillForms))
            {
                if (s is FileShare.None)
                {
                    if (folder.CreateBy == authContext.CurrentAccount.ID)
                    {
                        sharesToAdd.Add(s);
                    }

                    continue;
                }

                sharesToAdd.Add(s);
            }

            result.Add(subjectType, sharesToAdd);
        }

        return result;

    }

    public async Task<int> GetLinksSettings<T>(FileEntry<T> fileEntry, SubjectType subjectType)
    {
        FrozenDictionary<FolderType, FrozenDictionary<SubjectType, int>> linkSettings;

        var folder = fileEntry as Folder<T>;
        var file = folder == null && fileEntry is File<T> ? fileEntry : null;
        var room = folder != null && DocSpaceHelper.IsRoom(folder.FolderType) ? folder : null;
        var parentRoomType = fileEntry.RootFolderType == FolderType.USER ? FolderType.USER : fileEntry.ParentRoomType;

        if (room != null)
        {
            parentRoomType = room.FolderType;
            linkSettings = _linkCountRoomSettingsAccesses;
        }
        else
        {
            T folderId = default;
            if (file != null)
            {
                folderId = file.ParentId;
            }
            else if (folder != null)
            {
                folderId = folder.Id;
            }

            if (folderId != null)
            {
                room = await daoFactory.GetCacheFolderDao<T>().GetParentFoldersAsync(folderId).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));
                if (room != null)
                {
                    parentRoomType = room.FolderType;
                }
            }
            linkSettings = _linkCountRoomFileSettingsAccesses;
        }

        if (parentRoomType.HasValue && linkSettings.TryGetValue(parentRoomType.Value, out var access) && access.TryGetValue(subjectType, out var i))
        {
            return i;
        }

        return 0;
    }

    public static bool IsAvailableAccess(FileShare share, SubjectType subjectType, FolderType roomType)
    {
        return AvailableRoomAccesses.TryGetValue(roomType, out var availableRoles) &&
               availableRoles.TryGetValue(subjectType, out var availableRolesBySubject) &&
               availableRolesBySubject.Contains(share);
    }

    private record SubjectRecord(Guid UserId, bool IncludeLinks);
    
    private readonly ConcurrentDictionary<SubjectRecord, Lazy<Task<List<OrderedSubject>>>> _subjects = new();
    
    private async Task<List<OrderedSubject>> GetUserOrderedSubjectsAsync<T>(Guid userId, bool includeAvailableLinks = false)
    {
        // priority order
        // User, Group, admin, everyone

        var result = new List<OrderedSubject> { new(userId, SubjectOrderType.User) };

        var groups = await userManager.GetUserGroupsAsync(userId);

        foreach (var g in groups)
        {
            result.Add(new(g.ID, SubjectOrderType.Group));
        }

        if (await fileSecurityCommon.IsDocSpaceAdministratorAsync(userId))
        {
            result.Add(new(Constants.GroupAdmin.ID, SubjectOrderType.Group));
        }

        result.Add(new(Constants.GroupEveryone.ID, SubjectOrderType.Group));

        var linkId = await externalShare.GetLinkIdAsync();
        if (linkId != Guid.Empty)
        {
            result.Add(new(linkId, SubjectOrderType.CurrentLink));
        }

        if (includeAvailableLinks)
        {
            await foreach (var tag in daoFactory.GetTagDao<T>().GetTagsAsync(userId, default, TagType.RecentByLink))
            {
                if (Guid.TryParse(tag.Name, out var tagId) && linkId != tagId)
                {
                    result.Add(new(tagId, SubjectOrderType.RecentByLink));
                }
            }
        }

        return result;
    }


    /// Access priority order
    public enum SubjectOrderType
    {
        User = 0,
        Group = 1,
        CurrentLink = 2,
        RecentByLink = 3
    }

    public record OrderedSubject(Guid Subject, SubjectOrderType OrderType);

    private async Task<List<Folder<T>>> GetFileParentFolders<T>(T fileParentId)
    {
        var folderDao = daoFactory.GetCacheFolderDao<T>();

        var parentFolders = await folderDao.GetParentFoldersAsync(fileParentId).ToListAsync();

        return parentFolders;
    }

    private async ValueTask<bool> HasFullAccessAsync<T>(FileEntry<T> entry, Guid userId, bool isGuest, bool isRoom, bool isUser)
    {
        if (isGuest || isUser)
        {
            return false;
        }

        if (isRoom)
        {
            return entry.CreateBy == userId;
        }

        if (_cachedRoomOwner.TryGetValue(GetCacheKey(entry.ParentId), out var roomOwner))
        {
            return roomOwner == userId;
        }

        var folderDao = daoFactory.GetCacheFolderDao<T>();
        var room = await DocSpaceHelper.GetParentRoom(entry, folderDao);

        if (room == null)
        {
            return false;
        }

        _cachedRoomOwner.TryAdd(GetCacheKey(entry.ParentId), room.CreateBy);

        return room.CreateBy == userId;
    }

    private async Task<bool> IsAllGeneralNotificationSettingsOffAsync()
    {
        var userId = authContext.CurrentAccount.ID;

        if (!await badgesSettingsHelper.GetEnabledForCurrentUserAsync()
            && !await studioNotifyHelper.IsSubscribedToNotifyAsync(userId, Actions.RoomsActivity)
            && !await studioNotifyHelper.IsSubscribedToNotifyAsync(userId, Actions.SendWhatsNew))
        {
            return true;
        }

        return false;
    }

    private string GetCacheKey<T>(T parentId, Guid userId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return $"{tenantId}-{userId}-{parentId}";
    }

    private string GetCacheKey<T>(T parentId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return $"{tenantId}-{parentId}";
    }
    
    private sealed class OrderedSubjectComparer<T>(List<OrderedSubject> orderedSubjects) : IComparer<FileShareRecord<T>>
    {
        public int Compare(FileShareRecord<T> x, FileShareRecord<T> y)
        {
            var orderedSubjectX = orderedSubjects.Find(s => s.Subject == x.Subject);
            if (orderedSubjectX == null)
            {
                return -1;
            }

            var orderedSubjectY = orderedSubjects.Find(s => s.Subject == y.Subject);
            if (orderedSubjectY == null)
            {
                return 1;
            }

            return orderedSubjectX.OrderType.CompareTo(orderedSubjectY.OrderType);
        }
    }

    async Task<FileShareRecord<T>> GetShareRecordAsync<T>(Folder<T> room, Guid userId, bool isDocSpaceAdmin, IEnumerable<FileShareRecord<T>> shares)
    {
        var cachedRecords = GetCachedRecords<T>();
        var cacheKey = GetCacheKey(room.Id, userId);

        if (!cachedRecords.TryGetValue(cacheKey, out var record))
        {
            record = await GetCurrentShareAsync(room, userId, isDocSpaceAdmin, shares);
            cachedRecords.TryAdd(cacheKey, record);
        }

        return record;
    }

    /// <summary>
    /// The actions that can be performed with the file.
    /// </summary>
    public enum FilesSecurityActions
    {
        [SwaggerEnum("Read")]
        Read,

        [SwaggerEnum("Comment")]
        Comment,

        [SwaggerEnum("Fill forms")]
        FillForms,

        [SwaggerEnum("Review")]
        Review,

        [SwaggerEnum("Create")]
        Create,

        [SwaggerEnum("CreateFrom")]
        CreateFrom,

        [SwaggerEnum("Edit")]
        Edit,

        [SwaggerEnum("Delete")]
        Delete,

        [SwaggerEnum("Custom filter")]
        CustomFilter,

        [SwaggerEnum("Edit room")]
        EditRoom,

        [SwaggerEnum("Rename")]
        Rename,

        [SwaggerEnum("Read history")]
        ReadHistory,

        [SwaggerEnum("Lock")]
        Lock,

        [SwaggerEnum("Edit history")]
        EditHistory,

        [SwaggerEnum("Copy to")]
        CopyTo,

        [SwaggerEnum("Copy")]
        Copy,

        [SwaggerEnum("Move to")]
        MoveTo,

        [SwaggerEnum("Move")]
        Move,

        [SwaggerEnum("Pin")]
        Pin,

        [SwaggerEnum("Mute")]
        Mute,

        [SwaggerEnum("Edit access")]
        EditAccess,

        [SwaggerEnum("Duplicate")]
        Duplicate,

        [SwaggerEnum("Submit to form gallery")]
        SubmitToFormGallery,

        [SwaggerEnum("Download")]
        Download,

        [SwaggerEnum("Convert")]
        Convert,

        [SwaggerEnum("Copy shared link")]
        CopySharedLink,

        [SwaggerEnum("Read links")]
        ReadLinks,

        [SwaggerEnum("Reconnect")]
        Reconnect,

        [SwaggerEnum("Create room from")]
        CreateRoomFrom,

        [SwaggerEnum("Copy link")]
        CopyLink,

        [SwaggerEnum("Embed")]
        Embed,

        [SwaggerEnum("Change owner")]
        ChangeOwner,

        [SwaggerEnum("Index export")]
        IndexExport,

        [SwaggerEnum("Start filling")]
        StartFilling,

        [SwaggerEnum("Filling status")]
        FillingStatus,

        [SwaggerEnum("Reset filling")]
        ResetFilling,

        [SwaggerEnum("Start filling")]
        StopFilling,

        [SwaggerEnum("Open form")]
        OpenForm,

        [Description("Edit internal")]
        EditInternal,

        [Description("Edit expiration")]
        EditExpiration,
        
        [Description("Vectorization")]
        Vectorization,
        
        [Description("Asc AI")]
        AscAi,
        
        [Description("Use chat")]
        UseChat
    }
}