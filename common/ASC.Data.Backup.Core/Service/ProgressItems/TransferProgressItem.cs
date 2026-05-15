// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Data.Backup.Services;

[Transient]
public class TransferProgressItem : BaseBackupProgressItem
{
    private TenantManager _tenantManager;
    private readonly ILogger<TransferProgressItem> _logger;
    private readonly NotifyHelper _notifyHelper;
    private readonly IConfiguration _configuration;

    public TransferProgressItem()
    {

    }

    public TransferProgressItem(
        ILogger<TransferProgressItem> logger,
        IServiceScopeFactory serviceScopeFactory,
        NotifyHelper notifyHelper,
        IConfiguration configuration) :
        base(serviceScopeFactory)
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
        Init();
        BackupProgressItemType = BackupProgressItemType.Transfer;
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
            using var transferProgressItem = scope.ServiceProvider.GetService<TransferPortalTask>();


            await _notifyHelper.SendAboutTransferStartAsync(tenant, TargetRegion, Notify);
            transferProgressItem.Init(TenantId, TargetRegion, Limit, TempFolder);
            transferProgressItem.ProgressChanged = async args =>
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