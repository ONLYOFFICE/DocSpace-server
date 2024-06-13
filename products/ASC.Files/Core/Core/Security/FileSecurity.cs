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

[Scope]
public class FileSecurity(IDaoFactory daoFactory,
        UserManager userManager,
        TenantManager tenantManager,
        AuthContext authContext,
        GlobalFolder globalFolder,
        FileSecurityCommon fileSecurityCommon,
        FileUtility fileUtility,
        StudioNotifyHelper studioNotifyHelper,
        BadgesSettingsHelper badgesSettingsHelper,
        ExternalShare externalShare,
        AuthManager authManager)
    : IFileSecurity
{
    public readonly FileShare DefaultMyShare = FileShare.Restrict;
    public readonly FileShare DefaultCommonShare = FileShare.Read;
    public readonly FileShare DefaultPrivacyShare = FileShare.Restrict;
    public readonly FileShare DefaultArchiveShare = FileShare.Restrict;
    public readonly FileShare DefaultVirtualRoomsShare = FileShare.Restrict;

    public static readonly FrozenDictionary<FolderType, FrozenDictionary<SubjectType, HashSet<FileShare>>> AvailableFileAccesses =
        new Dictionary<FolderType, FrozenDictionary<SubjectType, HashSet<FileShare>>>
    {
        {
            FolderType.USER, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                {
                        SubjectType.ExternalLink,
                        [FileShare.Editing, FileShare.CustomFilter, FileShare.Review, FileShare.Comment, FileShare.Read, FileShare.Restrict, FileShare.None]
                },
                {
                        SubjectType.PrimaryExternalLink,
                        [FileShare.Editing, FileShare.CustomFilter, FileShare.Review, FileShare.Comment, FileShare.Read, FileShare.Restrict, FileShare.None]
                    }
                }.ToFrozenDictionary()
        }
    }.ToFrozenDictionary();

    public static readonly FrozenDictionary<FolderType, FrozenDictionary<SubjectType, HashSet<FileShare>>> AvailableRoomAccesses =
        new Dictionary<FolderType, FrozenDictionary<SubjectType, HashSet<FileShare>>>
    {
        {
            FolderType.CustomRoom, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                {
                    SubjectType.User, [
                        FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Editing, FileShare.Review,
                        FileShare.Comment, FileShare.FillForms, FileShare.Read, FileShare.None
                    ]
                },
                {
                    SubjectType.Group, [
                        FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.FillForms, FileShare.Read, FileShare.None
                    ]
                }, 
                {
                    SubjectType.InvitationLink, [
                        FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Editing, FileShare.Review,
                        FileShare.Comment, FileShare.FillForms, FileShare.Read, FileShare.None
                    ] 
                },
                { SubjectType.ExternalLink, [FileShare.Read, FileShare.None] },
                { SubjectType.PrimaryExternalLink, [FileShare.Read, FileShare.None] }
            }.ToFrozenDictionary()
        },
        {
            FolderType.PublicRoom, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { SubjectType.User, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.None] },
                { SubjectType.InvitationLink, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Read, FileShare.None] },
                { SubjectType.ExternalLink, [FileShare.Read, FileShare.None] },
                { SubjectType.PrimaryExternalLink, [FileShare.Read, FileShare.None] }
            }.ToFrozenDictionary()
        },
        {
            FolderType.FillingFormsRoom, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { SubjectType.User, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.FillForms, FileShare.Read, FileShare.None] },
                { SubjectType.Group , [FileShare.FillForms, FileShare.Read, FileShare.None] },
                { SubjectType.InvitationLink, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.FillForms, FileShare.Read, FileShare.None] }
            }.ToFrozenDictionary()
        },
        { 
            FolderType.EditingRoom, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { SubjectType.User, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Editing, FileShare.Read, FileShare.None] },
                { SubjectType.Group , [FileShare.Editing, FileShare.Read, FileShare.None] },
                { SubjectType.InvitationLink, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Editing, FileShare.Read, FileShare.None] }
            }.ToFrozenDictionary()
        },
        {
            FolderType.ReviewRoom, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { 
                    SubjectType.User, [
                            FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Review, FileShare.Comment,
                            FileShare.Read, FileShare.None
                        ]
                },
                {
                    SubjectType.Group, [
                            FileShare.Review, FileShare.Comment, FileShare.Read, FileShare.None
                        ]  
                },
                {
                    SubjectType.InvitationLink, [
                            FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Review, FileShare.Comment,
                            FileShare.Read, FileShare.None
                        ]
                }
            }.ToFrozenDictionary()
        },
        {
            FolderType.ReadOnlyRoom, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { SubjectType.User, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Read, FileShare.None] },
                { SubjectType.Group, [FileShare.Read, FileShare.None] },
                { SubjectType.InvitationLink, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Read, FileShare.None] }
            }.ToFrozenDictionary()
        },
        {
            FolderType.FormRoom, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { SubjectType.User, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Editing, FileShare.FillForms, FileShare.Read, FileShare.None] },
                { SubjectType.Group, [FileShare.Editing, FileShare.FillForms, FileShare.Read, FileShare.None] },
                { SubjectType.InvitationLink, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Editing, FileShare.Read, FileShare.None] }
            }.ToFrozenDictionary()
        },
        {
            FolderType.VirtualDataRoom, new Dictionary<SubjectType, HashSet<FileShare>>
            {
                { SubjectType.User, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Read, FileShare.None] },
                { SubjectType.InvitationLink, [FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Read, FileShare.None] }
            }.ToFrozenDictionary()
        }
    }.ToFrozenDictionary();

    public static readonly FrozenDictionary<EmployeeType, HashSet<FileShare>> AvailableUserAccesses = new Dictionary<EmployeeType, HashSet<FileShare>>
    {
        {
            EmployeeType.DocSpaceAdmin, [FileShare.RoomAdmin, FileShare.None]
        },
        {
            EmployeeType.RoomAdmin,
            [
                FileShare.RoomAdmin, FileShare.Collaborator, FileShare.Editing, FileShare.Review, FileShare.Comment,
                FileShare.FillForms, FileShare.Read, FileShare.None
            ]
        },
        {
            EmployeeType.Collaborator,
            [
                FileShare.Collaborator, FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.FillForms,
                FileShare.Read, FileShare.None
            ]
        },
        {
            EmployeeType.User,
            [
                FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.FillForms, FileShare.Read,
                FileShare.None
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
                    FilesSecurityActions.Convert
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
                    FilesSecurityActions.Reconnect
                }
            }
    }.ToFrozenDictionary();

    private readonly ConcurrentDictionary<string, FileShareRecord> _cachedRecords = new();
    private readonly ConcurrentDictionary<string, Guid> _cachedRoomOwner = new();

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
        if (!await CanAsync(entry, userId, FilesSecurityActions.Download))
        {
            return false;
        }

        return CheckDenyDownload(entry);
    }

    public async Task<bool> CanShareAsync<T>(FileEntry<T> entry, Guid userId)
    {
        if (!await CanEditAsync(entry, userId))
        {
            return false;
        }

        return CheckDenySharing(entry);
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

    public async Task<bool> CanEditHistoryAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.EditHistory);
    }

    public async Task<bool> CanReadLinksAsync<T>(FileEntry<T> entry)
    {
        return await CanAsync(entry, authContext.CurrentAccount.ID, FilesSecurityActions.ReadLinks);
    }
    
    public async Task<IEnumerable<Guid>> WhoCanReadAsync<T>(FileEntry<T> entry, bool includeLinks = false)
    {
        return await WhoCanAsync(entry, FilesSecurityActions.Read, includeLinks);
    }

    private async Task<IEnumerable<Guid>> WhoCanAsync<T>(FileEntry<T> entry, FilesSecurityActions action, bool includeLinks = false)
    {
        var shares = await GetSharesAsync(entry);

        if (!includeLinks)
        {
            shares = shares.Where(r => !r.IsLink);
        }

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var copyShares = shares.GroupBy(k => k.Subject).ToDictionary(k => k.Key);

        FileShareRecord[] defaultRecords;

        switch (entry.RootFolderType)
        {
            case FolderType.COMMON:
                defaultRecords =
                [
                    new FileShareRecord
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

                    if (defaultShareRecord != null &&((defaultShareRecord.Share == FileShare.Read && action == FilesSecurityActions.Read) ||
                        (defaultShareRecord.Share == FileShare.ReadWrite)))
                    {
                        return (await userManager.GetUsersByGroupAsync(defaultShareRecord.Subject))
                                          .Where(x => x.Status == EmployeeStatus.Active).Select(y => y.Id).Distinct();
                    }

                    return Enumerable.Empty<Guid>();
                }

                break;

            case FolderType.USER:
                defaultRecords =
                [
                    new FileShareRecord
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

                if (!shares.Any())
                {
                    return new List<Guid>
                    {
                        entry.RootCreateBy
                    };
                }
                break;

            case FolderType.Privacy:
                defaultRecords =
                [
                    new FileShareRecord
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

                if (!shares.Any())
                {
                    return new List<Guid>
                    {
                        entry.RootCreateBy
                    };
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
                            return await adapter.WhoCanReadAsync(entry);
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
                    new FileShareRecord
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
                    var users = new List<Guid>();

                    foreach (var defaultRecord in defaultRecords)
                    {
                        users.AddRange((await userManager.GetUsersByGroupAsync(defaultRecord.Subject)).Where(x => x.Status == EmployeeStatus.Active).Select(y => y.Id));
                    }

                    return users.Distinct();
                }

                break;

            default:
                defaultRecords = null;
                break;
        }

        if (defaultRecords != null)
        {
            shares = shares.Concat(defaultRecords);
        }

        var manyShares = shares.ToAsyncEnumerable().SelectManyAwait(async x => await ToGuidAsync(x)).Distinct();

        var result = new List<Guid>();

        await foreach (var userId in manyShares)
        {
            var userSubjects = await GetUserSubjectsAsync(userId);
            var userShares = new List<FileShareRecord>();

            foreach (var subject in userSubjects)
            {
                if (copyShares.TryGetValue(subject, out var value))
                {
                    userShares.AddRange(value);
                }
            }
            
            if (await CanAsync(entry, userId, action, userShares))
            {
                result.Add(userId);
            }
        }

        return result;
    }

    private async ValueTask<IAsyncEnumerable<Guid>> ToGuidAsync(FileShareRecord x)
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
        var user = await userManager.GetUsersAsync(userId);
        var isOutsider = await userManager.IsOutsiderAsync(user);
        var userType = await userManager.GetUserTypeAsync(user);
        var isUser = userType is EmployeeType.User;
        var isAuthenticated =  authContext.IsAuthenticated;
        var isDocSpaceAdmin = userType is EmployeeType.DocSpaceAdmin;
        var isCollaborator = userType is EmployeeType.Collaborator;

        await foreach (var entry in entries)
        {
            if (entry.Security != null)
            {
                yield return entry;
            }

            var tasks = Enum.GetValues<FilesSecurityActions>()
                .Where(r => _securityEntries[entry.FileEntryType].Contains(r))
                .Select(async e =>
                {
                    var t = await FilterEntryAsync(entry, e, userId, null, isOutsider, isUser, isAuthenticated, isDocSpaceAdmin, isCollaborator);
                    return new KeyValuePair<FilesSecurityActions, bool>(e, t);
                });

            entry.Security = (await Task.WhenAll(tasks)).ToDictionary(a => a.Key, b => b.Value);

            yield return entry;
        }
    }

    public IAsyncEnumerable<FileEntry<T>> FilterDownloadAsync<T>(IAsyncEnumerable<FileEntry<T>> entries)
    {
        return FilterReadAsync(entries).Where(CheckDenyDownload);
    }

    public static FileShare GetHighFreeRole(FolderType folderType)
    {
        return folderType switch
        {
            FolderType.CustomRoom => FileShare.Editing,
            FolderType.FillingFormsRoom => FileShare.FillForms,
            FolderType.EditingRoom => FileShare.Editing,
            FolderType.ReviewRoom => FileShare.Review,
            FolderType.ReadOnlyRoom => FileShare.Read,
            FolderType.VirtualDataRoom => FileShare.Read,
            FolderType.FormRoom => FileShare.FillForms,
            _ => FileShare.None
        };
    }

    public static EmployeeType GetTypeByShare(FileShare share)
    {
        return share switch
        {
            FileShare.RoomAdmin => EmployeeType.RoomAdmin,
            FileShare.Collaborator => EmployeeType.Collaborator,
            _ => EmployeeType.User
        };
    }

    private bool CheckDenyDownload<T>(FileEntry<T> entry)
    {
        return !entry.DenyDownload || entry.Access != FileShare.Read && entry.Access != FileShare.Comment;
    }

    private bool CheckDenySharing<T>(FileEntry<T> entry)
    {
        return !entry.DenySharing || entry.Access != FileShare.ReadWrite;
    }

    private async Task<bool> CanAsync<T>(FileEntry<T> entry, Guid userId, FilesSecurityActions action, IEnumerable<FileShareRecord> shares = null)
    {
        if (entry.Security != null && entry.Security.TryGetValue(action, out var result))
        {
            return result;
        }

        var user = await userManager.GetUsersAsync(userId);
        var isOutsider = await userManager.IsOutsiderAsync(user);

        if (isOutsider && action != FilesSecurityActions.Read)
        {
            return false;
        }

        var userType = await userManager.GetUserTypeAsync(user);
        var isUser = userType is EmployeeType.User;
        var isDocSpaceAdmin = userType is EmployeeType.DocSpaceAdmin;
        var isCollaborator = userType is EmployeeType.Collaborator;
        var isAuthenticated =  authContext.IsAuthenticated || (await authManager.GetAccountByIDAsync(await tenantManager.GetCurrentTenantIdAsync(), userId)).IsAuthenticated;

        return await FilterEntryAsync(entry, action, userId, shares, isOutsider, isUser, isAuthenticated, isDocSpaceAdmin, isCollaborator);
    }
    
    private async IAsyncEnumerable<Tuple<FileEntry<T>, bool>> CanAsync<T>(IAsyncEnumerable<FileEntry<T>> entry, Guid userId, FilesSecurityActions action)
    {
        await foreach (var r in SetSecurity(entry, userId))
        {
            if (r.Security != null && r.Security.TryGetValue(action, out var security))
            {
                yield return new Tuple<FileEntry<T>, bool>(r, security);
            }
            else
            {
                yield return new Tuple<FileEntry<T>, bool>(r, await CanAsync(r, userId, action));
            }
        }
    }

    private async Task<bool> FilterEntryAsync<T>(FileEntry<T> e, FilesSecurityActions action, Guid userId, IEnumerable<FileShareRecord> shares, bool isOutsider, bool isUser, bool isAuthenticated, bool isDocSpaceAdmin, bool isCollaborator)
    {
        var file = e as File<T>;
        var folder = e as Folder<T>;
        var isRoom = folder != null && DocSpaceHelper.IsRoom(folder.FolderType);

        if (file != null &&
            action == FilesSecurityActions.Edit &&
            fileUtility.CanWebRestrictedEditing(file.Title))
        {
            return false;
        }

        if (action is FilesSecurityActions.ReadHistory or FilesSecurityActions.EditHistory &&
             e.ProviderEntry)
        {
            return false;
        }

        if (action == FilesSecurityActions.SubmitToFormGallery &&
            file is not { FilterType: FilterType.OFormTemplateOnly })
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

        if (e.FileEntryType == FileEntryType.Folder)
        {
            if (folder == null)
            {
                return false;
            }

            if (folder.FolderType == FolderType.Recent && isUser)
            {
                return false;
            }

            if (action != FilesSecurityActions.Read)
            {
                if (action is FilesSecurityActions.Duplicate or
                              FilesSecurityActions.Delete or
                              FilesSecurityActions.EditAccess or
                              FilesSecurityActions.Edit or
                              FilesSecurityActions.Move or
                              FilesSecurityActions.MoveTo or
                              FilesSecurityActions.Rename or
                              FilesSecurityActions.Create
                    && (folder.FolderType == FolderType.ReadyFormFolder || 
                    folder.FolderType == FolderType.InProcessFormFolder || 
                    folder.FolderType == FolderType.FormFillingFolderDone ||
                    folder.FolderType == FolderType.FormFillingFolderInProgress))
                {
                    return false;
                }
                if (action is FilesSecurityActions.Pin or FilesSecurityActions.EditAccess or FilesSecurityActions.Mute &&
                    !isRoom)
                {
                    return false;
                }

                if (action == FilesSecurityActions.Mute && isRoom && await IsAllGeneralNotificationSettingsOffAsync())
                {
                    return false;
                }

                if (action == FilesSecurityActions.CopySharedLink && folder.FolderType is not (FolderType.CustomRoom or FolderType.PublicRoom))
                {
                    return false;
                }

                if (action is FilesSecurityActions.Copy or FilesSecurityActions.Duplicate && isRoom)
                {
                    return false;
                }

                if (folder.FolderType == FolderType.Recent)
                {
                    return false;
                }

                if (!isUser)
                {
                    if (folder.FolderType == FolderType.USER)
                    {
                        return folder.CreateBy == userId && action is FilesSecurityActions.Create or FilesSecurityActions.CopyTo or FilesSecurityActions.MoveTo or FilesSecurityActions.FillForms;
                    }

                    if (folder.FolderType == FolderType.Archive && action == FilesSecurityActions.MoveTo)
                    {
                        return true;
                    }

                    if (folder.FolderType == FolderType.VirtualRooms && !isCollaborator)
                    {
                        return action is FilesSecurityActions.Create or FilesSecurityActions.MoveTo;
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

                if (folder.FolderType == FolderType.Archive)
                {
                    return true;
                }
            }
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
                if (isOutsider || action == FilesSecurityActions.Lock || (isUser && !e.Shared))
                {
                    return false;
                }
                if (e.RootCreateBy == userId)
                {
                    // user has all right in his folder
                    return true;
                }
                break;
            case FolderType.VirtualRooms:
                if (action == FilesSecurityActions.Delete && isRoom)
                {
                    return false;
                }
                if (action == FilesSecurityActions.FillForms && file != null)
                {
                    var fileFolder = await daoFactory.GetFolderDao<T>().GetFolderAsync(file.ParentId);
                    if ((fileFolder.FolderType == FolderType.FormFillingFolderInProgress && file.CreateBy != userId) || fileFolder.FolderType == FolderType.FormFillingFolderDone)
                    {
                        return false;
                    }
                }
                if (await HasFullAccessAsync(e, userId, isUser, isDocSpaceAdmin, isRoom, isCollaborator))
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
                    action != FilesSecurityActions.ReadLinks
                    )
                {
                    return false;
                }

                if (action is FilesSecurityActions.Delete or FilesSecurityActions.Move &&
                    !isRoom)
                {
                    return false;
                }

                if (await HasFullAccessAsync(e, userId, isUser, isDocSpaceAdmin, isRoom, isCollaborator))
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
            if ((!isRoom && e.RootFolderType is FolderType.VirtualRooms or FolderType.Archive &&
                 _cachedRecords.TryGetValue(await GetCacheKey(e.ParentId, userId), out var value)) ||
                _cachedRecords.TryGetValue(await GetCacheKey(e.ParentId, await externalShare.GetLinkIdAsync()), out value))
            {
                ace = value.Clone();
                ace.EntryId = e.Id;
            }
            else
            {
                var subjects = new List<Guid>();
                if (shares == null)
                {
                    subjects = await GetUserSubjectsAsync(userId, e);
                    shares = await GetSharesAsync(e, subjects);
                }

                if (e.FileEntryType == FileEntryType.File)
                {
                    ace = shares
                        .OrderBy(r => r, new SubjectComparer(subjects))
                        .ThenByDescending(r => r.Share, new FileShareRecord.ShareComparer(e.RootFolderType))
                        .FirstOrDefault(r => Equals(r.EntryId, e.Id.ToString()) && r.EntryType == FileEntryType.File);

                    if (ace == null)
                    {
                        // share on parent folders
                        ace = shares.Where(r => Equals(r.EntryId, file.ParentId) && r.EntryType == FileEntryType.Folder)
                            .OrderBy(r => r, new SubjectComparer(subjects))
                            .ThenBy(r => r.Level)
                            .FirstOrDefault();
                    }
                }
                else
                {
                    ace = shares.Where(r => Equals(r.EntryId, e.Id) && r.EntryType == FileEntryType.Folder)
                        .OrderBy(r => r, new SubjectComparer(subjects))
                        .ThenBy(r => r.Level)
                        .ThenByDescending(r => r.Share, new FileShareRecord.ShareComparer(e.RootFolderType))
                        .FirstOrDefault();
                }
            
                if (e.RootFolderType is FolderType.VirtualRooms or FolderType.Archive && 
                    ace is { SubjectType: SubjectType.User or SubjectType.ExternalLink or SubjectType.PrimaryExternalLink })
                {
                    var id = ace.SubjectType is SubjectType.ExternalLink or SubjectType.PrimaryExternalLink ? ace.Subject : userId;

                    _cachedRecords.TryAdd(await GetCacheKey(e.ParentId, id), ace);
                }
            }
        }

        var defaultShare =
            e.RootFolderType == FolderType.VirtualRooms ? DefaultVirtualRoomsShare :
            e.RootFolderType == FolderType.USER ? DefaultMyShare :
            e.RootFolderType == FolderType.Privacy ? DefaultPrivacyShare :
            e.RootFolderType == FolderType.Archive ? DefaultArchiveShare :
            DefaultCommonShare;

        e.ShareRecord = ace;
        e.Access = ace?.Share ?? defaultShare;
        e.Access = e.RootFolderType is FolderType.ThirdpartyBackup ? FileShare.Restrict : e.Access;

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
                        if (e.Access is FileShare.Comment or FileShare.Review or FileShare.RoomAdmin or FileShare.Editing or FileShare.FillForms or FileShare.Collaborator)
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
                        if (e.Access is FileShare.Editing or FileShare.Review or FileShare.FillForms) 
                        {
                    return true;
                }
                break;
                    default:
                        if (e.Access is FileShare.FillForms or FileShare.RoomAdmin or FileShare.Editing or FileShare.Collaborator)
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
                        if (e.Access is FileShare.Review or FileShare.RoomAdmin or FileShare.Editing or FileShare.Collaborator)
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
                        return false;
                    default:
                        if (e.Access is FileShare.RoomAdmin or FileShare.Collaborator)
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
                        if (e.Access == FileShare.Editing)
                        {
                    return true;
                }
                break;
                    default:
                        if (e.Access is FileShare.RoomAdmin or FileShare.Collaborator || e.Access is FileShare.Editing && !MustConvert(e))
                        {
                            return true;
                        }
                        break;
                }
                break;
            case FilesSecurityActions.Delete:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                if (e.Access == FileShare.RoomAdmin ||
                    (e.Access == FileShare.Collaborator && e.CreateBy == authContext.CurrentAccount.ID))
                {
                    if (file is { RootFolderType: FolderType.VirtualRooms })
                    {
                        return true;
                    }

                    if (folder is { RootFolderType: FolderType.VirtualRooms, FolderType: FolderType.DEFAULT })
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
                        if (e.Access is FileShare.Editing or FileShare.CustomFilter)
                        {
                    return true;
                }
                break;
                    default:
                        if (e.Access is FileShare.RoomAdmin or FileShare.Collaborator || e.Access is FileShare.Editing && !MustConvert(e))
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
                if (e.Access == FileShare.RoomAdmin)
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
                        return false;
                    default:
                        if (e.Access == FileShare.RoomAdmin ||
                    (e.Access == FileShare.Collaborator && e.CreateBy == authContext.CurrentAccount.ID))
                {
                    return true;
                }
                break;
                }
                break;
            case FilesSecurityActions.ReadHistory:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        if (e.Access != FileShare.Restrict && isAuthenticated)
                        {
                            return true;
                        }
                        break;
                    default:
                if (e.Access is FileShare.RoomAdmin or FileShare.Editing or FileShare.Collaborator)
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
                if (e.Access is FileShare.RoomAdmin or FileShare.Collaborator)
                {
                    return true;
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
                        if (e.Access is FileShare.RoomAdmin or FileShare.Collaborator)
                        {
                            return true;
                        }
                        break;
                }
                break;
            case FilesSecurityActions.CopyTo:
            case FilesSecurityActions.MoveTo:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                if (e.Access is FileShare.RoomAdmin or FileShare.Collaborator)
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
                        if (e.Access != FileShare.Restrict && isAuthenticated && !isUser)
                        {
                            return true;
                        }
                        break;
                    default:
                        if (e.Access == FileShare.RoomAdmin ||
                            (e.Access == FileShare.Collaborator && e.CreateBy == authContext.CurrentAccount.ID))
                        {
                            return true;
                        }
                        break;
                }
                break;
            case FilesSecurityActions.Duplicate:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                if (e.Access == FileShare.RoomAdmin ||
                    (e.Access == FileShare.Collaborator && e.CreateBy == authContext.CurrentAccount.ID))
                {
                    return true;
                }
                break;
                }
                break;
            case FilesSecurityActions.EditAccess:
            case FilesSecurityActions.ReadLinks:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                if (e.Access == FileShare.RoomAdmin)
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
                        return false;
                    default:
                if ((e.Access == FileShare.RoomAdmin ||
                     e.Access == FileShare.Collaborator && e.CreateBy == authContext.CurrentAccount.ID)
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
                    default:
                if (e.Access is FileShare.RoomAdmin or FileShare.Collaborator && 
                    file is { FilterType: FilterType.OFormTemplateOnly })
                {
                    return true;
                }
                break;
                }
                break;
            case FilesSecurityActions.Download:
                if (e.Access != FileShare.Restrict && ace?.Options is not { DenyDownload: true })
                {
                    return true;
                }
                break;
            case FilesSecurityActions.CopySharedLink:
                switch (e.RootFolderType)
                {
                    case FolderType.USER:
                        return false;
                    default:
                if (e.Access == FileShare.RoomAdmin || (e.Access != FileShare.Restrict && e.Shared))
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
            e.RootFolderType != FolderType.Archive && e.RootFolderType != FolderType.VirtualRooms)
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

    public async Task ShareAsync<T>(T entryId, FileEntryType entryType, Guid @for, FileShare share, SubjectType subjectType = default, FileShareOptions options = null)
    {
        var securityDao = daoFactory.GetSecurityDao<T>();
        var r = new FileShareRecord
        {
            TenantId = await tenantManager.GetCurrentTenantIdAsync(),
            EntryId = entryId,
            EntryType = entryType,
            Subject = @for,
            Owner = authContext.CurrentAccount.ID,
            Share = share,
            SubjectType = subjectType,
            Options = options
        };

        await securityDao.SetShareAsync(r);
    }

    public async Task<IEnumerable<FileShareRecord>> GetSharesAsync<T>(FileEntry<T> entry, IEnumerable<Guid> subjects = null)
    {
        return await daoFactory.GetSecurityDao<T>().GetSharesAsync(entry, subjects);
    }

    public IAsyncEnumerable<FileShareRecord> GetPureSharesAsync<T>(FileEntry<T> entry, IEnumerable<Guid> subjects)
    {
        return daoFactory.GetSecurityDao<T>().GetPureSharesAsync(entry, subjects);
    }

    public IAsyncEnumerable<FileShareRecord> GetPureSharesAsync<T>(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text, int offset = 0, int count = -1)
    {
        return daoFactory.GetSecurityDao<T>().GetPureSharesAsync(entry, filterType, status, text, offset, count);
    }

    public Task<int> GetPureSharesCountAsync<T>(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text)
    {
        return daoFactory.GetSecurityDao<T>().GetPureSharesCountAsync(entry, filterType, status, text);
    }

    public async IAsyncEnumerable<FileEntry> GetSharesForMeAsync(FilterType filterType, bool subjectGroup, Guid subjectID, string searchText = "", string[] extension = null, bool searchInContent = false, bool withSubfolders = false)
    {
        var securityDao = daoFactory.GetSecurityDao<int>();
        var subjects = await GetUserSubjectsAsync(authContext.CurrentAccount.ID);
        var records = await securityDao.GetSharesAsync(subjects).ToListAsync();

        var firstTask = GetSharesForMeAsync<int>(records.Where(r => r.EntryId is int), subjects, filterType, subjectGroup, subjectID, searchText, extension, searchInContent, withSubfolders).ToListAsync();
        var secondTask = GetSharesForMeAsync<string>(records.Where(r => r.EntryId is string), subjects, filterType, subjectGroup, subjectID, searchText, extension, searchInContent, withSubfolders).ToListAsync();

        foreach (var items in await Task.WhenAll(firstTask.AsTask(), secondTask.AsTask()))
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }
    }

    public async Task<List<FileEntry>> GetVirtualRoomsAsync(FilterType filterType, Guid subjectId, string searchText, bool searchInContent, bool withSubfolders,
        SearchArea searchArea, bool withoutTags, IEnumerable<string> tagNames, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, QuotaFilter quotaFilter, RoomDataLifetimeFilter lifetimeFilter)
    {
        var securityDao = daoFactory.GetSecurityDao<int>();

        var subjectEntries = subjectFilter is SubjectFilter.Member
                ? await securityDao.GetSharesAsync(new[] { subjectId }).Where(r => r.EntryType == FileEntryType.Folder).Select(r => r.EntryId.ToString()).ToListAsync()
                : null;

        if (await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
        {
            return await GetAllVirtualRoomsAsync(filterType, subjectId, searchText, searchInContent, withSubfolders, searchArea, withoutTags, tagNames, excludeSubject, provider, subjectFilter, subjectEntries, quotaFilter, lifetimeFilter);
        }

        return await GetVirtualRoomsForMeAsync(filterType, subjectId, searchText, searchInContent, withSubfolders, searchArea, withoutTags, tagNames, excludeSubject, provider, subjectFilter, subjectEntries);
    }

    private async Task<List<FileEntry>> GetAllVirtualRoomsAsync(FilterType filterType, Guid subjectId, string search, bool searchInContent, bool withSubfolders,
        SearchArea searchArea, bool withoutTags, IEnumerable<string> tagNames, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter,
        IEnumerable<string> subjectEntries, QuotaFilter quotaFilter, RoomDataLifetimeFilter lifetimeFilter)
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
            _ => new[] { await globalFolder.GetFolderVirtualRoomsAsync(daoFactory), await globalFolder.GetFolderArchiveAsync(daoFactory) }
        };

        var roomsEntries = await folderDao.GetRoomsAsync(rootFoldersIds, filterType, tagNames, subjectId, search, withSubfolders, withoutTags, excludeSubject,
            provider, subjectFilter, subjectEntries, quotaFilter, lifetimeFilter).ToListAsync();
        var thirdPartyRoomsEntries = await folderThirdPartyDao.GetProviderBasedRoomsAsync(searchArea, filterType, tagNames, subjectId, search, withoutTags, 
            excludeSubject, provider, subjectFilter, subjectEntries).ToListAsync();

        entries.AddRange(roomsEntries);
        entries.AddRange(thirdPartyRoomsEntries);

        if (withSubfolders && filterType != FilterType.FoldersOnly)
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

        await SetTagsAsync(roomsEntries);
        await SetTagsAsync(thirdPartyRoomsEntries);
        await SetPinAsync(roomsEntries);
        await SetPinAsync(thirdPartyRoomsEntries);

        return entries;
    }

    private async Task<List<FileEntry>> GetVirtualRoomsForMeAsync(FilterType filterType, Guid subjectId, string search, bool searchInContent, bool withSubfolders,
        SearchArea searchArea, bool withoutTags, IEnumerable<string> tagNames, bool excludeSubject, ProviderFilter provider, SubjectFilter subjectFilter, IEnumerable<string> subjectEntries)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var folderThirdPartyDao = daoFactory.GetFolderDao<string>();
        var fileDao = daoFactory.GetFileDao<int>();
        var thirdPartyFileDao = daoFactory.GetFileDao<string>();
        var securityDao = daoFactory.GetSecurityDao<int>();

        var entries = new List<FileEntry>();

        var currentUserSubjects = await GetUserSubjectsAsync(authContext.CurrentAccount.ID);
        var currentUsersRecords = await securityDao.GetSharesAsync(currentUserSubjects).ToListAsync();

        var internalRoomsRecords = new Dictionary<int, FileShareRecord>();
        var thirdPartyRoomsRecords = new Dictionary<string, FileShareRecord>();

        var recordGroup = currentUsersRecords.GroupBy(r => new { r.EntryId, r.EntryType }, (_, group) => new
        {
            firstRecord = group.OrderBy(r => r, new SubjectComparer(currentUserSubjects))
            .ThenByDescending(r => r.Share, new FileShareRecord.ShareComparer(FolderType.VirtualRooms))
            .First()
        });

        foreach (var record in recordGroup.Select(r=> r.firstRecord).Where(r => r.Share != FileShare.Restrict))
        {
            if (record.EntryType != FileEntryType.Folder)
            {
                continue;
            }

            switch (record.EntryId)
            {
                case int roomId when !internalRoomsRecords.ContainsKey(roomId):
                    internalRoomsRecords.Add(roomId, record);
                    break;
                case string thirdPartyRoomId when !thirdPartyRoomsRecords.ContainsKey(thirdPartyRoomId):
                    thirdPartyRoomsRecords.Add(thirdPartyRoomId, record);
                    break;
            }
        }

        var rootFoldersIds = searchArea switch
        {
            SearchArea.Active => [await globalFolder.GetFolderVirtualRoomsAsync(daoFactory)],
            SearchArea.Archive => [await globalFolder.GetFolderArchiveAsync(daoFactory)],
            _ => new[] { await globalFolder.GetFolderVirtualRoomsAsync(daoFactory), await globalFolder.GetFolderArchiveAsync(daoFactory) }
        };

        var rooms = await folderDao.GetRoomsAsync(internalRoomsRecords.Keys, filterType, tagNames, subjectId, search, withSubfolders, withoutTags, excludeSubject, provider, subjectFilter, subjectEntries, rootFoldersIds)
             .Where(r => Filter(r, internalRoomsRecords)).ToListAsync();
        var thirdPartyRooms = await folderThirdPartyDao.GetProviderBasedRoomsAsync(searchArea, thirdPartyRoomsRecords.Keys, filterType,
                tagNames, subjectId, search, withoutTags, excludeSubject, provider, subjectFilter, subjectEntries)
            .Where(r => Filter(r, thirdPartyRoomsRecords)).ToListAsync();

        if (withSubfolders && filterType != FilterType.FoldersOnly)
        {
            var files = await fileDao.GetFilesAsync(rooms.Select(r => r.Id), FilterType.None, false, Guid.Empty, search, null, searchInContent).ToListAsync();
            var thirdPartyFiles = await thirdPartyFileDao.GetFilesAsync(thirdPartyRooms.Select(r => r.Id), FilterType.None, false, Guid.Empty, search, null, searchInContent).ToListAsync();

            entries.AddRange(files.Where(f => Filter(f, internalRoomsRecords)));
            entries.AddRange(thirdPartyFiles.Where(f => Filter(f, thirdPartyRoomsRecords)));
        }

        await SetTagsAsync(rooms);
        await SetTagsAsync(thirdPartyRooms);
        await SetPinAsync(rooms);
        await SetPinAsync(thirdPartyRooms);

        entries.AddRange(rooms);
        entries.AddRange(thirdPartyRooms);

        bool Filter<T>(FileEntry<T> entry, IReadOnlyDictionary<T, FileShareRecord> records)
        {
            var id = entry.FileEntryType == FileEntryType.Folder ? entry.Id : entry.ParentId;
            var record = records.GetValueOrDefault(id);

            switch (searchArea)
            {
                case SearchArea.Archive when entry.RootFolderType == FolderType.Archive:
                case SearchArea.Active when entry.RootFolderType == FolderType.VirtualRooms:
                case SearchArea.Any when entry.RootFolderType is FolderType.VirtualRooms or FolderType.Archive:
                    {
                        entry.ShareRecord = record;
                        entry.Access = record?.Share ?? FileShare.None;
                        return true;
                    }
                default:
                    return false;
            }
        }

        return entries;
    }

    private async Task SetTagsAsync<T>(IEnumerable<FileEntry<T>> entries)
    {
        if (!entries.Any())
        {
            return;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tags = await tagDao.GetTagsAsync(TagType.Custom, entries).ToLookupAsync(f => (T)f.EntryId);

        foreach (var room in entries)
        {
            room.Tags = tags[room.Id];
        }
    }

    private async Task SetPinAsync<T>(IEnumerable<FileEntry<T>> entries)
    {
        if (!entries.Any())
        {
            return;
        }

        var tagDao = daoFactory.GetTagDao<T>();

        var tags = await tagDao.GetTagsAsync(authContext.CurrentAccount.ID, TagType.Pin, entries).ToDictionaryAsync(t => (T)t.EntryId);

        foreach (var fileEntry in entries.Where(e => e.FileEntryType == FileEntryType.Folder))
        {
            var room = (Folder<T>)fileEntry;
            if (tags.ContainsKey(room.Id))
            {
                room.Pinned = true;
            }
        }
    }

    private async IAsyncEnumerable<FileEntry> GetSharesForMeAsync<T>(IEnumerable<FileShareRecord> records, List<Guid> subjects, FilterType filterType, bool subjectGroup, 
        Guid subjectID, string searchText = "", string[] extension = null, bool searchInContent = false, bool withSubfolders = false)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var securityDao = daoFactory.GetSecurityDao<T>();

        var fileIds = new Dictionary<T, FileShare>();
        var folderIds = new Dictionary<T, FileShare>();

        var recordGroup = records.GroupBy(r => new { r.EntryId, r.EntryType }, (_, group) => new
        {
            firstRecord = group.OrderBy(r => r, new SubjectComparer(subjects))
               .ThenByDescending(r => r.Share, new FileShareRecord.ShareComparer(FolderType.SHARE))
               .First()
        });

        foreach (var r in recordGroup.Select(r=> r.firstRecord).Where(r => r.Share != FileShare.Restrict))
        {
            if (r.EntryType == FileEntryType.Folder)
            {
                if (!folderIds.ContainsKey((T)r.EntryId))
                {
                    folderIds.Add((T)r.EntryId, r.Share);
                }
            }
            else
            {
                if (!fileIds.ContainsKey((T)r.EntryId))
                {
                    fileIds.Add((T)r.EntryId, r.Share);
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
                if (fileIds.TryGetValue(x.Id, out _))
                {
                    x.Access = fileIds[x.Id];
                    x.FolderIdDisplay = share;
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

            var share = await globalFolder.GetFolderShareAsync<T>(daoFactory);

            await foreach (var folder in folders)
            {
                if (folderIds.TryGetValue(folder.Id, out _))
                {
                    folder.Access = folderIds[folder.Id];
                    folder.FolderIdDisplay = share;
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
                                f.RootFolderType == FolderType.USER // show users files
                                && f.RootCreateBy != authContext.CurrentAccount.ID // don't show my files
            );

        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            data = data.Where(r => !r.ProviderEntry);
        }

        var failedEntries = entries.Where(x => !string.IsNullOrEmpty(x.Error));
        var failedRecords = new List<FileShareRecord>();

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
        var securityDao = daoFactory.GetSecurityDao<int>();
        var subjects = await GetUserSubjectsAsync(authContext.CurrentAccount.ID);
        var records = await securityDao.GetSharesAsync(subjects).ToListAsync();

        await foreach (var e in GetPrivacyForMeAsync<int>(records.Where(r => r.EntryId is int), subjects, filterType, subjectGroup, subjectID, searchText,
                           extension, searchInContent, withSubfolders))
        {
            yield return e;
        }

        await foreach (var e in GetPrivacyForMeAsync<string>(records.Where(r => r.EntryId is string), subjects, filterType, subjectGroup, subjectID, searchText, 
                           extension, searchInContent, withSubfolders))
        {
            yield return e;
        }
    }

    private async IAsyncEnumerable<FileEntry<T>> GetPrivacyForMeAsync<T>(IEnumerable<FileShareRecord> records, List<Guid> subjects, FilterType filterType, bool subjectGroup, 
        Guid subjectID, string searchText = "", string[] extension = null, bool searchInContent = false, bool withSubfolders = false)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var fileIds = new Dictionary<T, FileShare>();
        var folderIds = new Dictionary<T, FileShare>();

        var recordGroup = records.GroupBy(r => new { r.EntryId, r.EntryType }, (_, group) => new
        {
            firstRecord = group.OrderBy(r => r, new SubjectComparer(subjects))
                .ThenByDescending(r => r.Share, new FileShareRecord.ShareComparer(FolderType.Privacy))
                .First()
        });

        foreach (var r in recordGroup.Select(r=> r.firstRecord).Where(r => r.Share != FileShare.Restrict))
        {
            if (r.EntryType == FileEntryType.Folder)
            {
                if (!folderIds.ContainsKey((T)r.EntryId))
                {
                    folderIds.Add((T)r.EntryId, r.Share);
                }
            }
            else
            {
                if (!fileIds.ContainsKey((T)r.EntryId))
                {
                    fileIds.Add((T)r.EntryId, r.Share);
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

    public async Task<List<Guid>> GetUserSubjectsAsync(Guid userId)
    {
        return await GetUserSubjectsAsync<int>(userId, null);
        }

    public async IAsyncEnumerable<FileShareRecord> GetUserRecordsAsync<T>()
        {
        var securityDao = daoFactory.GetSecurityDao<T>();
        var currentUserSubjects = await GetUserSubjectsAsync(authContext.CurrentAccount.ID);

        await foreach (var record in securityDao.GetSharesAsync(currentUserSubjects))
        {
            yield return record;
        }
    }

    public static void CorrectSecurityByLockedStatus<T>(FileEntry<T> entry)
    {
        if (entry is not File<T> file || file.Security == null || file.LockedBy == null)
        {
            return;
        }

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

    public IDictionary<string, bool> GetFileAccesses<T>(File<T> file, SubjectType subjectType)
    {
        var result = new Dictionary<string, bool>();

        var mustConvert = fileUtility.MustConvert(file.Title);
        var canEdit = fileUtility.CanWebEdit(file.Title);
        var canCustomFiltering = fileUtility.CanWebCustomFilterEditing(file.Title);
        var canComment = fileUtility.CanWebComment(file.Title);
        var canReview = fileUtility.CanWebReview(file.Title);
        var fileType = FileUtility.GetFileTypeByFileName(file.Title);

        if (!AvailableFileAccesses.TryGetValue(file.RootFolderType, out var subjectShares)
            || !subjectShares.TryGetValue(subjectType, out var shares))
        {
            return null;
        }

        foreach (var s in shares)
        {
            if (s is FileShare.Read or FileShare.Restrict or FileShare.None)
            {
                result.Add(s.ToStringFast(), true);
                continue;
            }
            
            if (mustConvert)
            {
                result.Add(s.ToStringFast(), false);
                continue;
            }

            switch (s)
            {
                case FileShare.Editing when canEdit:
                case FileShare.FillForms when fileType is FileType.OForm or FileType.Pdf:
                case FileShare.CustomFilter when canCustomFiltering:
                case FileShare.Comment when canComment:
                case FileShare.Review when canReview:
                    result.Add(s.ToStringFast(), true);
                    break;
                default:
                    result.Add(s.ToStringFast(), false);
                    break;
            }
        }

        return result;
    }
    
    private async Task<List<Guid>> GetUserSubjectsAsync<T>(Guid userId, FileEntry<T> entry)
    {
        // priority order
        // User, Departments, admin, everyone

        var result = new List<Guid> { userId };
        
        var linkId = await externalShare.GetLinkIdAsync();
        if (linkId != Guid.Empty)
        {
            result.Add(linkId);
        }

        if (entry is { RootFolderType: FolderType.USER } and not IFolder { FolderType: FolderType.USER } &&
            entry.RootCreateBy != userId && linkId == Guid.Empty)
        {
            var linkTag = await daoFactory.GetTagDao<T>().GetTagsAsync(userId, TagType.RecentByLink, [entry]).FirstOrDefaultAsync();
            if (linkTag != null && Guid.TryParse(linkTag.Name, out var linkTagId))
            {
                result.Add(linkTagId);
            }
        }
        
        result.AddRange((await userManager.GetUserGroupsAsync(userId)).Select(g => g.ID));
        
        if (await fileSecurityCommon.IsDocSpaceAdministratorAsync(userId))
        {
            result.Add(Constants.GroupAdmin.ID);
        }

        result.Add(Constants.GroupEveryone.ID);

        return result;
    }

    private async Task<bool> HasFullAccessAsync<T>(FileEntry<T> entry, Guid userId, bool isUser, bool isDocSpaceAdmin, bool isRoom, bool isCollaborator)
    {
        if (isUser || isCollaborator)
        {
            return false;
        }

        if (isDocSpaceAdmin)
        {
            return true;
        }

        if (isRoom)
        {
            return entry.CreateBy == userId;
        }

        if (_cachedRoomOwner.TryGetValue(await GetCacheKey(entry.ParentId), out var roomOwner))
        {
            return roomOwner == userId;
        }

        var room = await daoFactory.GetFolderDao<T>().GetParentFoldersAsync(entry.ParentId)
            .Where(f => DocSpaceHelper.IsRoom(f.FolderType))
            .FirstOrDefaultAsync();

        if (room == null)
        {
            return false;
        }

        _cachedRoomOwner.TryAdd(await GetCacheKey(entry.ParentId), room.CreateBy);

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

    private async Task<string> GetCacheKey<T>(T parentId, Guid userId)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        return $"{tenantId}-{userId}-{parentId}";
        }

    private async Task<string> GetCacheKey<T>(T parentId)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        return $"{tenantId}-{parentId}";
    }

    private sealed class SubjectComparer(List<Guid> subjects) : IComparer<FileShareRecord>
    {
        public int Compare(FileShareRecord x, FileShareRecord y)
        {
            if (x.Subject == y.Subject)
            {
                return 0;
            }

            var index1 = subjects.IndexOf(x.Subject);
            var index2 = subjects.IndexOf(y.Subject);
            if (index1 == 0 || index2 == 0 // UserId
                || Constants.SystemGroups.Any(g => g.ID == x.Subject) || Constants.SystemGroups.Any(g => g.ID == y.Subject)) // System Groups
            {
                return index1.CompareTo(index2);
            }

            // Departments are equal.
            return 0;
        }
    }

    public enum FilesSecurityActions
    {
        Read,
        Comment,
        FillForms,
        Review,
        Create,
        Edit,
        Delete,
        CustomFilter,
        EditRoom,
        Rename,
        ReadHistory,
        Lock,
        EditHistory,
        CopyTo,
        Copy,
        MoveTo,
        Move,
        Pin,
        Mute,
        EditAccess,
        Duplicate,
        SubmitToFormGallery,
        Download,
        Convert,
        CopySharedLink,
        ReadLinks,
        Reconnect
    }
}
