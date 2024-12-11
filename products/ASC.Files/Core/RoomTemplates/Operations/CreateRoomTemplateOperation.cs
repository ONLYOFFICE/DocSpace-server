﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using ASC.Files.Core.RoomTemplates.Events;

namespace ASC.Files.Core.RoomTemplates.Operations;

[Transient]
public class CreateRoomTemplateOperation(IServiceProvider serviceProvider) : DistributedTaskProgress
{
    private Guid _userId;
    private LogoSettings _logo;
    private IEnumerable<string> _tags;
    private IEnumerable<string> _emails;
    private string _title;

    private int? _roomId;
    private int? _templateId;
    private int? _tenantId;
    private int _totalCount;
    private int _count;
    public int TenantId
    {
        get => _tenantId ?? this[nameof(_tenantId)];
        set
        {
            _tenantId = value;
            this[nameof(_tenantId)] = value;
        }
    }

    public int RoomId
    {
        get => _roomId ?? this[nameof(_roomId)];
        set
        {
            _roomId = value;
            this[nameof(_roomId)] = value;
        }
    }

    public int TemplateId
    {
        get => _templateId ?? this[nameof(_templateId)];
        set
        {
            _templateId = value;
            this[nameof(_templateId)] = value;
        }
    }

    public void Init(int tenantId,
        Guid userId,
        int roomId,
        string title,
        IEnumerable<string> emails,
        LogoSettings logo,
        IEnumerable<string> tags)
    {
        TenantId = tenantId;
        _userId = userId;
        RoomId = roomId;
        _logo = logo;
        _tags = tags;
        _emails = emails;
        _title = title;
    }

    protected override async Task DoJob()
    {
        var tenantManager = serviceProvider.GetService<TenantManager>();
        var securityContext = serviceProvider.GetService<SecurityContext>();
        var globalHelper = serviceProvider.GetService<GlobalFolderHelper>();
        var fileStorageService = serviceProvider.GetService<FileStorageService>();
        var dbFactory = serviceProvider.GetService<IDbContextFactory<FilesDbContext>>();
        var daoFactory = serviceProvider.GetService<IDaoFactory>();
        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();

        try
        {
            await tenantManager.SetCurrentTenantAsync(TenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            LogoRequest dtoLogo = null;
            if (_logo != null)
            {
                dtoLogo = new LogoRequest
                {
                    TmpFile = _logo.TmpFile,
                    Height = _logo.Height,
                    Width = _logo.Width,
                    X = _logo.X,
                    Y = _logo.Y
                };
            }

            TemplateId = (await fileStorageService.CreateRoomTemplateAsync(RoomId, _title, _emails, _tags, dtoLogo)).Id;

            _totalCount = await fileDao.GetFilesCountAsync(RoomId, FilterType.None, false, Guid.Empty, string.Empty, null, false, true);
            var files = fileDao.GetFilesAsync(RoomId);
            var folders = folderDao.GetFoldersAsync(RoomId).Select(r => r.Id);
            
            await foreach (var file in files)
            {
                await fileDao.CopyFileAsync(file, TemplateId);
                _count++;
                await PublishAsync();
            }

            await foreach (var f in folders)
            {
                var newFolder = await folderDao.CopyFolderAsync(f, TemplateId, CancellationToken);
                var folderFiles = fileDao.GetFilesAsync(f);
                await foreach (var file in folderFiles)
                {
                    await fileDao.CopyFileAsync(file, newFolder.Id);
                    await PublishAsync();
                }
            }

            Percentage = 100;
            IsCompleted = true;
        }
        catch (Exception ex)
        {
            await folderDao.DeleteFolderAsync(TemplateId);
            Exception = ex;
            IsCompleted = true;
        }
        finally
        {
            await PublishChanges();
        }
    }

    private async Task PublishAsync()
    {
        Percentage = _count * 0.9 / _totalCount;
        await PublishChanges();
    }
}
