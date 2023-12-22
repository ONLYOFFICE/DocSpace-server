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

namespace ASC.Files.Service.RoomIndexExport;

[Scope]
public class RoomIndexExportIntegrationEventHandler : IIntegrationEventHandler<RoomIndexExportIntegrationEvent>
{
    private readonly ILogger _logger;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly TenantManager _tenantManager;
    private readonly AuthManager _authManager;
    private readonly SecurityContext _securityContext;
    private readonly DocumentBuilderScriptHelper _documentBuilderScriptHelper;
    private readonly DocumentBuilderTaskManager _documentBuilderTaskManager;
    private readonly IServiceProvider _serviceProvider;

    public RoomIndexExportIntegrationEventHandler(
        ILogger<RoomIndexExportIntegrationEventHandler> logger,
        CommonLinkUtility commonLinkUtility,
        TenantManager tenantManager,
        AuthManager authManager,
        SecurityContext securityContext,
        DocumentBuilderScriptHelper documentBuilderScriptHelper,
        DocumentBuilderTaskManager documentBuilderTaskManager,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _commonLinkUtility = commonLinkUtility;
        _tenantManager = tenantManager;
        _authManager = authManager;
        _securityContext = securityContext;
        _documentBuilderScriptHelper = documentBuilderScriptHelper;
        _documentBuilderTaskManager = documentBuilderTaskManager;
        _serviceProvider = serviceProvider;
    }

    public async Task Handle(RoomIndexExportIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();

        using (_logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}-{Program.AppName}") }))
        {
            _logger.InformationHandlingIntegrationEvent(@event.Id, Program.AppName, @event);

            try
            {
                if (@event.Terminate)
                {
                    _documentBuilderTaskManager.TerminateTask(@event.TenantId, @event.CreateBy);
                    return;
                }

                _commonLinkUtility.ServerUri = @event.BaseUri;

                await _tenantManager.SetCurrentTenantAsync(@event.TenantId);

                var account = await _authManager.GetAccountByIDAsync(@event.TenantId, @event.CreateBy);

                await _securityContext.AuthenticateMeWithoutCookieAsync(account);

                var (script, tempFileName, outputFileName) = await _documentBuilderScriptHelper.GetRoomIndexExportScript(@event.CreateBy, @event.RoomId);

                var task = _serviceProvider.GetService<DocumentBuilderTask<int>>();

                task.Init(@event.BaseUri, @event.TenantId, @event.CreateBy, script, tempFileName, outputFileName);

                _documentBuilderTaskManager.StartTask(task, true);
            }
            catch (Exception ex)
            {
                _logger.ErrorWithException(ex);
            }
        }
    }
}