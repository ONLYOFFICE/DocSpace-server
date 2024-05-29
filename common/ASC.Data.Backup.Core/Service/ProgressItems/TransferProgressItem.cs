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


namespace ASC.Data.Backup.Services;

[Transient]
public class TransferProgressItem : BaseBackupProgressItem
{
    private TenantManager _tenantManager;
    private readonly ILogger<TransferProgressItem> _logger;
    private readonly NotifyHelper _notifyHelper;
    private readonly IConfiguration _configuration;

    public TransferProgressItem(
        ILogger<TransferProgressItem> logger,
        IServiceScopeFactory serviceScopeFactory,
        NotifyHelper notifyHelper,
        IConfiguration configuration) :
        base(logger, serviceScopeFactory)
    {
        _logger = logger;
        _notifyHelper = notifyHelper;
        BackupProgressItemType = BackupProgressItemType.Transfer;
        _configuration = configuration;
    }

    public string TargetRegion { get; set; }
    public bool Notify { get; set; }
    public string TempFolder { get; set; }
    public int Limit { get; set; }

    public void Init(
        string targetRegion,
        int tenantId,
        string tempFolder,
        int limit,
        bool notify)
    {
        TenantId = tenantId;
        TargetRegion = targetRegion;
        Notify = notify;
        TempFolder = tempFolder;
        Limit = limit;

    }

    protected override async Task DoJob()
    {
        var tempFile = PathHelper.GetTempFileName(TempFolder);
        var tenant = await _tenantManager.GetTenantAsync(TenantId);
        var alias = tenant.Alias;

        try
        {
            await using var scope = _serviceScopeProvider.CreateAsyncScope();
            _tenantManager = scope.ServiceProvider.GetService<TenantManager>();
            var transferProgressItem = scope.ServiceProvider.GetService<TransferPortalTask>();


            await _notifyHelper.SendAboutTransferStartAsync(tenant, TargetRegion, Notify);
            transferProgressItem.Init(TenantId, TargetRegion, Limit, TempFolder);
            transferProgressItem.ProgressChanged = async (args) =>
            {
                Percentage = args.Progress;
                await PublishChanges();
            };

            await transferProgressItem.RunJob();

            Link = GetLink(alias, false);
            await _notifyHelper.SendAboutTransferCompleteAsync(tenant, TargetRegion, Link, !Notify, transferProgressItem.ToTenantId);

            await PublishChanges();
        }
        catch (Exception error)
        {
            _logger.ErrorTransferProgressItem(error);
            Exception = error;

            Link = GetLink(alias, true);
            await _notifyHelper.SendAboutTransferErrorAsync(tenant, TargetRegion, Link, !Notify);
        }
        finally
        {
            try
            {
                await PublishChanges();
            }
            catch (Exception error)
            {
                _logger.ErrorPublish(error);
            }

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private string GetLink(string alias, bool isErrorLink)
    {
        var domain = isErrorLink ? "core:base-domain" : $"{TargetRegion}:core:base-domain";
        return "https://" + alias + "." + _configuration[domain];
    }

    public override object Clone()
    {
        return MemberwiseClone();
    }
}
