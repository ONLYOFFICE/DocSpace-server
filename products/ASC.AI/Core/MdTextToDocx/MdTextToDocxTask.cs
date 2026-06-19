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

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.AI.Core.MdTextToDocx;

[ProtoContract]
public class MdTextToDocxTaskData
{
    [ProtoMember(1)]
    public required string Title { get; set; }

    [ProtoMember(2)]
    public required string Content { get; set; }

    [ProtoMember(3)]
    public string? BaseUri { get; set; }

    [ProtoMember(4)]
    public int FolderId { get; set; }

    [ProtoMember(5)]
    public string? ThirdpartyFolderId { get; set; }
}

[Transient]
public class MdTextToDocxTask(IServiceScopeFactory serviceScopeFactory) : DistributedTask
{
    private MdTextToDocxTaskData _data = null!;
    private Guid _userId;
    private int _tenantId;

    public void Init(int tenantId, Guid userId, MdTextToDocxTaskData data)
    {
        _tenantId = tenantId;
        _userId = userId;
        _data = data;
    }

    protected override async Task DoJob()
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MdTextToDocxTask>>();

        try
        {
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            _ = await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            var commonLinkUtility = scope.ServiceProvider.GetRequiredService<CommonLinkUtility>();

            if (!string.IsNullOrEmpty(_data.BaseUri))
            {
                commonLinkUtility.ServerUri = _data.BaseUri;
            }

            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileSecurity = scope.ServiceProvider.GetRequiredService<FileSecurity>();

            // Authoritative security gate: runs at execution time under the task's authenticated
            // identity (see AuthenticateMeWithoutCookieAsync above). The matching check in
            // TextToDocxTaskPublisher.PublishAsync is only an early-rejection optimisation; this
            // re-resolution and re-check is what actually protects against acting on a folder that
            // was deleted or whose permissions changed after the event was published.
            var target = await Target.InitializeAsync(daoFactory, fileSecurity, _data.FolderId, _data.ThirdpartyFolderId);

            var pathProvider = scope.ServiceProvider.GetRequiredService<PathProvider>();

            var bytes = Encoding.UTF8.GetBytes(_data.Content);
            await using var ms = new MemoryStream(bytes);
            var fileUri = await pathProvider.GetTempUrlAsync(ms, ".md");

            var docService = scope.ServiceProvider.GetRequiredService<DocumentServiceConnector>();

            var (_, outFileUri, outFileType) = await docService.GetConvertedUriAsync(
                fileUri,
                "md",
                "docx",
                Guid.NewGuid().ToString("n"),
                null,
                CultureInfo.CurrentUICulture.Name,
                null,
                null,
                null,
                false,
                false);

            var fileConverter = scope.ServiceProvider.GetRequiredService<FileConverter>();

            await target.SaveFile(fileConverter, outFileUri, outFileType, _data.Title, false);

            if (Status <= DistributedTaskStatus.Running)
            {
                Status = DistributedTaskStatus.Completed;
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
        }
    }
}
