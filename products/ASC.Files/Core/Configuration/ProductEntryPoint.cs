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

namespace ASC.Web.Files.Configuration;

[Scope]
public class ProductEntryPoint : Product
{
    private const string ProductPath = "/";

    private readonly FilesSpaceUsageStatManager _filesSpaceUsageStatManager;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly UserManager _userManager;
    private readonly NotifyConfiguration _notifyConfiguration;
    private readonly AuditEventsRepository _auditEventsRepository;
    private readonly IDaoFactory _daoFactory;
    private readonly TenantManager _tenantManager;
    private readonly RoomsNotificationSettingsHelper _roomsNotificationSettingsHelper;
    private readonly PathProvider _pathProvider;
    private readonly FilesLinkUtility _filesLinkUtility;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly FileSecurity _fileSecurity;
    private readonly GlobalFolder _globalFolder;
    private readonly ILogger<ProductEntryPoint> _logger;

    //public SubscriptionManager SubscriptionManager { get; }

    public ProductEntryPoint() { }

    public ProductEntryPoint(
        FilesSpaceUsageStatManager filesSpaceUsageStatManager,
        CoreBaseSettings coreBaseSettings,
        UserManager userManager,
        NotifyConfiguration notifyConfiguration,
        AuditEventsRepository auditEventsRepository,
        IDaoFactory daoFactory,
        TenantManager tenantManager,
        RoomsNotificationSettingsHelper roomsNotificationSettingsHelper,
        PathProvider pathProvider,
        FilesLinkUtility filesLinkUtility,
        FileSecurity fileSecurity,
        GlobalFolder globalFolder,
        CommonLinkUtility commonLinkUtility,
        ILogger<ProductEntryPoint> logger
        //            SubscriptionManager subscriptionManager
        )
    {
        _filesSpaceUsageStatManager = filesSpaceUsageStatManager;
        _coreBaseSettings = coreBaseSettings;
        _userManager = userManager;
        _notifyConfiguration = notifyConfiguration;
        _auditEventsRepository = auditEventsRepository;
        _daoFactory = daoFactory;
        _tenantManager = tenantManager;
        _roomsNotificationSettingsHelper = roomsNotificationSettingsHelper;
        _pathProvider = pathProvider;
        _filesLinkUtility = filesLinkUtility;
        _fileSecurity = fileSecurity;
        _globalFolder = globalFolder;
        _commonLinkUtility = commonLinkUtility;
        _logger = logger;
        //SubscriptionManager = subscriptionManager;
    }

    public static readonly Guid ID = WebItemManager.DocumentsProductID;

    private ProductContext _productContext;

    public override bool Visible => true;
    public override bool IsPrimary => true;

    public override void Init()
    {
        _productContext =
            new ProductContext
            {
                DisabledIconFileName = "product_disabled_logo.png",
                IconFileName = "images/files.menu.svg",
                LargeIconFileName = "images/files.svg",
                DefaultSortOrder = 10,
                //SubscriptionManager = SubscriptionManager,
                SpaceUsageStatManager = _filesSpaceUsageStatManager,
                AdminOpportunities = AdminOpportunities,
                UserOpportunities = UserOpportunities,
                CanNotBeDisabled = true
            };

        _notifyConfiguration?.Configure();
        return;

        List<string> UserOpportunities() => (_coreBaseSettings.CustomMode
            ? CustomModeResource.ProductUserOpportunitiesCustomMode
            : FilesCommonResource.ProductUserOpportunities).Split('|').ToList();

        //SearchHandlerManager.Registry(new SearchHandler());
        List<string> AdminOpportunities() => (_coreBaseSettings.CustomMode
            ? CustomModeResource.ProductAdminOpportunitiesCustomMode
            : FilesCommonResource.ProductAdminOpportunities).Split('|').ToList();
    }

    public override async Task<IEnumerable<ActivityInfo>> GetAuditEventsAsync(DateTime scheduleDate, Guid userId, Tenant tenant, WhatsNewType whatsNewType, CultureInfo cultureInfo)
    {
        IEnumerable<AuditEvent> events;
        _tenantManager.SetCurrentTenant(tenant);

        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        if (whatsNewType == WhatsNewType.RoomsActivity)
        {
            events = await _auditEventsRepository.GetByFilterWithActionsAsync(
                withoutUserId: userId,
                actions: StudioWhatsNewNotify.RoomsActivityActions,
                from: scheduleDate.AddHours(-1),
                to: scheduleDate.AddSeconds(-1),
                limit: 100);
        }
        else
        {
            events = await _auditEventsRepository.GetByFilterWithActionsAsync(
                withoutUserId: userId,
                actions: StudioWhatsNewNotify.DailyActions,
                from: scheduleDate.Date.AddDays(-1),
                to: scheduleDate.Date.AddSeconds(-1),
                limit: 100);
        }

        var docSpaceAdmin = await _userManager.IsDocSpaceAdminAsync(userId);

        var disabledRooms = await _roomsNotificationSettingsHelper.GetDisabledRoomsForCurrentUserAsync();

        var userRoomsWithRole = await GetUserRoomsWithRoleAsync(userId, docSpaceAdmin);

        var userRoomsWithRoleForSend = userRoomsWithRole.Where(r => !disabledRooms.Contains(r.Key)).ToList();
        var userRoomsForSend = userRoomsWithRoleForSend.Select(r => r.Key).ToList();

        var result = new List<ActivityInfo>();

        foreach (var e in events)
        {
            var activityInfo = new ActivityInfo
            {
                UserId = e.UserId,
                Action = (MessageAction)e.Action,
                Data = e.Date,
                FileTitle = e.Action != (int)MessageAction.UserFileUpdated ? e.Description[0] : e.Description[1]
            };

            switch (e.Action)
            {
                case (int)MessageAction.RoomCreated when !docSpaceAdmin:
                    continue;
                case (int)MessageAction.FileCreated or (int)MessageAction.FileUpdatedRevisionComment or (int)MessageAction.FileUploaded or (int)MessageAction.UserFileUpdated:
                    activityInfo.FileUrl = _commonLinkUtility.GetFullAbsolutePath(_filesLinkUtility.GetFileWebEditorUrl(e.Target.GetItems().FirstOrDefault()));
                    break;
            }

            EventDescription<JsonElement> additionalInfo;

            try
            {
                additionalInfo = JsonSerializer.Deserialize<EventDescription<JsonElement>>(e.Description.LastOrDefault()!);
            }
            catch (Exception ex)
            {
                _logger.ErrorDeserializingAuditEvent(e.Id, ex);
                continue;
            }

            activityInfo.TargetUsers = additionalInfo.UserIds;

            switch (e.Action)
            {
                case (int)MessageAction.UserCreated or (int)MessageAction.UserUpdated:
                    {
                        if (docSpaceAdmin)
                        {
                            result.Add(activityInfo);
                        }

                        continue;
                    }
                case (int)MessageAction.UsersUpdatedType:
                    {
                        if (docSpaceAdmin)
                        {
                            activityInfo.UserRole = GetDocSpaceRoleString((EmployeeType)additionalInfo.UserRole);
                            result.Add(activityInfo);
                        }
                        continue;
                    }
            }

            var roomId = additionalInfo.RoomId.ValueKind switch
            {
                JsonValueKind.String when int.TryParse(additionalInfo.RoomId.GetString(), out var id) => id,
                JsonValueKind.Number => additionalInfo.RoomId.GetInt32(),
                _ => 0
            };

            if (e.Action != (int)MessageAction.RoomCreated)
            {
                if (roomId <= 0 || !userRoomsForSend.Contains(roomId.ToString()))
                {
                    continue;
                }

                var isRoomAdmin = userRoomsWithRoleForSend
                    .Where(r => r.Key == roomId.ToString())
                    .Select(r => r.Value)
                    .FirstOrDefault();

                if (!CheckRightsToReceive(userId, (MessageAction)e.Action, isRoomAdmin, activityInfo.TargetUsers))
                {
                    continue;
                }
            }

            activityInfo.RoomUri = _pathProvider.GetRoomsUrl(roomId.ToString());
            activityInfo.RoomTitle = additionalInfo.RoomTitle;
            activityInfo.RoomOldTitle = additionalInfo.RoomOldTitle;

            if (e.Action == (int)MessageAction.RoomUpdateAccessForUser)
            {
                activityInfo.UserRole = GetRoomRoleString((FileShare)additionalInfo.UserRole);
            }

            result.Add(activityInfo);
        }
        
        return result;
    }

    public override Guid ProductID => ID;
    public override string Name => FilesCommonResource.ProductName;

    public override string Description => "";
    public override string StartURL => ProductPath;
    public override string HelpURL => PathProvider.StartURL;
    public override string ProductClassName => "files";
    public override ProductContext Context => _productContext;
    public override string ApiURL => string.Empty;

    private async Task<Dictionary<string, bool>> GetUserRoomsWithRoleAsync(Guid userId, bool isDocSpaceAdmin)
    {
        var result = new Dictionary<string, bool>();

        var folderDao = _daoFactory.GetFolderDao<int>();
        var securityDao = _daoFactory.GetSecurityDao<string>();

        var currentUserSubjects = await _fileSecurity.GetUserSubjectsAsync(userId);
        var currentUsersRecords = await securityDao.GetSharesAsync(currentUserSubjects).ToListAsync();

        foreach (var record in currentUsersRecords)
        {
            if (record.Owner == userId || record.Share == FileShare.RoomManager)
            {
                result.TryAdd(record.EntryId, true);
            }
            else if (record.Share != FileShare.Restrict)
            {
                result.TryAdd(record.EntryId, false);
            }
        }

        if (!isDocSpaceAdmin)
        {
            return result;
        }

        var virtualRoomsFolderId = await _globalFolder.GetFolderVirtualRoomsAsync(_daoFactory);
        var archiveFolderId = await _globalFolder.GetFolderArchiveAsync(_daoFactory);

        var rooms = await folderDao.GetRoomsAsync(new List<int> { virtualRoomsFolderId, archiveFolderId }, FilterType.None, null, Guid.Empty, null, false, false, false, ProviderFilter.None, SubjectFilter.Owner, null).ToListAsync();

        foreach (var room in rooms)
        {
            var roomId = room.Id.ToString();

            if (!result.ContainsKey(roomId))
            {
                result.TryAdd(roomId, true);
            }
        }

        return result;
    }

    private bool CheckRightsToReceive(Guid userId, MessageAction action, bool isRoomAdmin, List<Guid> targetUsers)
    {
        if (isRoomAdmin)
        {
            return true;
        }

        if (IsRoomAdminAction())
        {
            return false;
        }

        if (targetUsers != null
            && !targetUsers.Contains(userId)
            && IsRoomAdminOrTargetUserAction())
        {
            return false;
        }

        return true;

        bool IsRoomAdminAction()
        {
            if (action is MessageAction.RoomRenamed or MessageAction.RoomArchived or MessageAction.RoomCreateUser or MessageAction.RoomRemoveUser)
            {
                return true;
            }

            return false;
        }

        bool IsRoomAdminOrTargetUserAction()
        {
            if (action is MessageAction.RoomUpdateAccessForUser or MessageAction.RoomDeleted or MessageAction.UsersUpdatedType)
            {
                return true;
            }

            return false;
        }
    }

    private static string GetDocSpaceRoleString(EmployeeType employeeType)
    {
        return employeeType switch
        {
            EmployeeType.Guest or 
            EmployeeType.RoomAdmin or 
            EmployeeType.DocSpaceAdmin or 
            EmployeeType.User => FilesCommonResource.ResourceManager.GetString("RoleEnum_" + employeeType.ToStringFast()),
            _ => string.Empty
        };
    }

    private static string GetRoomRoleString(FileShare userRoomRole)
    {
        return userRoomRole switch
        {
            FileShare.Read or 
            FileShare.Review or 
            FileShare.Comment or 
            FileShare.FillForms or 
            FileShare.RoomManager or 
            FileShare.Editing or 
            FileShare.ContentCreator => FilesCommonResource.ResourceManager.GetString("RoleEnum_" + userRoomRole.ToStringFast()),
            _ => string.Empty
        };
    }
}
