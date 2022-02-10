/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


namespace ASC.Data.Reassigns
{
    [Transient]
    public class ReassignProgressItem : DistributedTaskProgress
    {
        public Guid FromUser { get; private set; }
        public Guid ToUser { get; private set; }

        private readonly IServiceProvider _serviceProvider;
        private readonly QueueWorkerRemove _queueWorkerRemove;
        private readonly IDictionary<string, StringValues> _httpHeaders;
        private int _tenantId;
        private Guid _currentUserId;
        private bool _deleteProfile;

        //private readonly IFileStorageService _docService;
        //private readonly ProjectsReassign _projectsReassign;

        public ReassignProgressItem(
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            QueueWorkerRemove queueWorkerRemove)
        {
            _serviceProvider = serviceProvider;
            _queueWorkerRemove = queueWorkerRemove;
            _httpHeaders = QueueWorker.GetHttpHeaders(httpContextAccessor.HttpContext.Request);

            //_docService = Web.Files.Classes.Global.FileStorageService;
            //_projectsReassign = new ProjectsReassign();
        }

        public void Init(int tenantId, Guid fromUserId, Guid toUserId, Guid currentUserId, bool deleteProfile)
        {
            _tenantId = tenantId;
            FromUser = fromUserId;
            ToUser = toUserId;
            _currentUserId = currentUserId;
            _deleteProfile = deleteProfile;

            //_docService = Web.Files.Classes.Global.FileStorageService;
            //_projectsReassign = new ProjectsReassign();

            Id = QueueWorkerReassign.GetProgressItemId(tenantId, fromUserId);
            Status = DistributedTaskStatus.Created;
            Exception = null;
            Percentage = 0;
            IsCompleted = false;
        }

        protected override void DoJob()
        {
            using var scope = _serviceProvider.CreateScope();
            var scopeClass = scope.ServiceProvider.GetService<ReassignProgressItemScope>();
            var (tenantManager, coreBaseSettings, messageService, studioNotifyService, securityContext, userManager, userPhotoManager, displayUserSettingsHelper, messageTarget, options) = scopeClass;
            var logger = options.Get("ASC.Web");
            var tenant = tenantManager.SetCurrentTenant(_tenantId);

            try
            {
                Percentage = 0;
                Status = DistributedTaskStatus.Running;

                securityContext.AuthenticateMeWithoutCookie(_currentUserId);

                logger.InfoFormat("reassignment of data from {0} to {1}", FromUser, ToUser);

                logger.Info("reassignment of data from documents");


                //_docService.ReassignStorage(_fromUserId, _toUserId);
                Percentage = 33;
                PublishChanges();

                logger.Info("reassignment of data from projects");

                //_projectsReassign.Reassign(_fromUserId, _toUserId);
                Percentage = 66;
                PublishChanges();

                if (!coreBaseSettings.CustomMode)
                {
                    logger.Info("reassignment of data from crm");

                    //using (var scope = DIHelper.Resolve(_tenantId))
                    //{
                    //    var crmDaoFactory = scope.Resolve<CrmDaoFactory>();
                    //    crmDaoFactory.ContactDao.ReassignContactsResponsible(_fromUserId, _toUserId);
                    //    crmDaoFactory.DealDao.ReassignDealsResponsible(_fromUserId, _toUserId);
                    //    crmDaoFactory.TaskDao.ReassignTasksResponsible(_fromUserId, _toUserId);
                    //    crmDaoFactory.CasesDao.ReassignCasesResponsible(_fromUserId, _toUserId);
                    //}
                    Percentage = 99;
                    PublishChanges();
                }

                SendSuccessNotify(userManager, studioNotifyService, messageService, messageTarget, displayUserSettingsHelper);

                Percentage = 100;
                Status = DistributedTaskStatus.Completed;

                if (_deleteProfile)
                {
                    DeleteUserProfile(userManager, userPhotoManager, messageService, messageTarget, displayUserSettingsHelper);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                Status = DistributedTaskStatus.Failted;
                Exception = ex;
                SendErrorNotify(userManager, studioNotifyService, ex.Message);
            }
            finally
            {
                logger.Info("data reassignment is complete");
                IsCompleted = true;
            }
            PublishChanges();
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        private void SendSuccessNotify(UserManager userManager, StudioNotifyService studioNotifyService, MessageService messageService, MessageTarget messageTarget, DisplayUserSettingsHelper displayUserSettingsHelper)
        {
            var fromUser = userManager.GetUsers(FromUser);
            var toUser = userManager.GetUsers(ToUser);

            studioNotifyService.SendMsgReassignsCompleted(_currentUserId, fromUser, toUser);

            var fromUserName = fromUser.DisplayUserName(false, displayUserSettingsHelper);
            var toUserName = toUser.DisplayUserName(false, displayUserSettingsHelper);

            if (_httpHeaders != null)
            {
                messageService.Send(_httpHeaders, MessageAction.UserDataReassigns, messageTarget.Create(FromUser), new[] { fromUserName, toUserName });
            }
            else
            {
                messageService.Send(MessageAction.UserDataReassigns, messageTarget.Create(FromUser), fromUserName, toUserName);
            }
        }

        private void SendErrorNotify(UserManager userManager, StudioNotifyService studioNotifyService, string errorMessage)
        {
            var fromUser = userManager.GetUsers(FromUser);
            var toUser = userManager.GetUsers(ToUser);

            studioNotifyService.SendMsgReassignsFailed(_currentUserId, fromUser, toUser, errorMessage);
        }

        private void DeleteUserProfile(UserManager userManager, UserPhotoManager userPhotoManager, MessageService messageService, MessageTarget messageTarget, DisplayUserSettingsHelper displayUserSettingsHelper)
        {
            var user = userManager.GetUsers(FromUser);
            var userName = user.DisplayUserName(false, displayUserSettingsHelper);

            userPhotoManager.RemovePhoto(user.ID);
            userManager.DeleteUser(user.ID);
            _queueWorkerRemove.Start(_tenantId, user, _currentUserId, false);

            if (_httpHeaders != null)
            {
                messageService.Send(_httpHeaders, MessageAction.UserDeleted, messageTarget.Create(FromUser), new[] { userName });
            }
            else
            {
                messageService.Send(MessageAction.UserDeleted, messageTarget.Create(FromUser), userName);
            }
        }
    }

    [Scope]
    public class ReassignProgressItemScope
    {
        private readonly TenantManager _tenantManager;
        private readonly CoreBaseSettings _coreBaseSettings;
        private readonly MessageService _messageService;
        private readonly StudioNotifyService _studioNotifyService;
        private readonly SecurityContext _securityContext;
        private readonly UserManager _userManager;
        private readonly UserPhotoManager _userPhotoManager;
        private readonly DisplayUserSettingsHelper _displayUserSettingsHelper;
        private readonly MessageTarget _messageTarget;
        private readonly IOptionsMonitor<ILog> _options;

        public ReassignProgressItemScope(TenantManager tenantManager,
            CoreBaseSettings coreBaseSettings,
            MessageService messageService,
            StudioNotifyService studioNotifyService,
            SecurityContext securityContext,
            UserManager userManager,
            UserPhotoManager userPhotoManager,
            DisplayUserSettingsHelper displayUserSettingsHelper,
            MessageTarget messageTarget,
            IOptionsMonitor<ILog> options)
        {
            _tenantManager = tenantManager;
            _coreBaseSettings = coreBaseSettings;
            _messageService = messageService;
            _studioNotifyService = studioNotifyService;
            _securityContext = securityContext;
            _userManager = userManager;
            _userPhotoManager = userPhotoManager;
            _displayUserSettingsHelper = displayUserSettingsHelper;
            _messageTarget = messageTarget;
            _options = options;
        }

        public void Deconstruct(out TenantManager tenantManager,
            out CoreBaseSettings coreBaseSettings,
            out MessageService messageService,
            out StudioNotifyService studioNotifyService,
            out SecurityContext securityContext,
            out UserManager userManager,
            out UserPhotoManager userPhotoManager,
            out DisplayUserSettingsHelper displayUserSettingsHelper,
            out MessageTarget messageTarget,
            out IOptionsMonitor<ILog> optionsMonitor)
        {
            tenantManager = _tenantManager;
            coreBaseSettings = _coreBaseSettings;
            messageService = _messageService;
            studioNotifyService = _studioNotifyService;
            securityContext = _securityContext;
            userManager = _userManager;
            userPhotoManager = _userPhotoManager;
            displayUserSettingsHelper = _displayUserSettingsHelper;
            messageTarget = _messageTarget;
            optionsMonitor = _options;
        }
    }

    public class ReassignProgressItemExtension
    {
        public static void Register(DIHelper services)
        {
            services.TryAdd<ReassignProgressItemScope>();
            services.AddDistributedTaskQueueService<ReassignProgressItem>(1);
        }
    }
}
