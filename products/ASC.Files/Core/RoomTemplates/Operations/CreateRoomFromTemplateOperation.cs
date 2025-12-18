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

namespace ASC.Files.Core.RoomTemplates.Operations;

[Transient]
public class CreateRoomFromTemplateOperation : DistributedTaskProgress
{
    private Guid _userId;
    private string _title;
    private string _cover;
    private string _color;
    private long? _quota;
    private bool? _indexing;
    private bool? _denyDownload;
    private bool? _private;
    private RoomLifetime _lifetime;
    private WatermarkRequest _watermark;
    private LogoSettings _logo;
    private bool _copyLogo;
    private IEnumerable<string> _tags;
    private int _templateId;
    private int _totalCount;
    private int _count;
    private readonly IServiceProvider _serviceProvider;

    public CreateRoomFromTemplateOperation()
    {

    }

    public CreateRoomFromTemplateOperation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public int TenantId { get; set; }
    public int RoomId { get; set; }

    public void Init(int tenantId,
        Guid userId,
        int templateId,
        string title,
        LogoSettings logo,
        bool copyLogo,
        IEnumerable<string> tags,
        string cover,
        string color,
        long? quota,
        bool? indexing,
        bool? denyDownload,
        RoomLifetime lifetime,
        WatermarkRequest watermark,
        bool? @private)
    {
        TenantId = tenantId;
        _userId = userId;
        _templateId = templateId;
        _title = title;
        _logo = logo;
        _copyLogo = copyLogo;
        _tags = tags;
        _cover = cover;
        _color = color;
        _quota = quota;
        _indexing = indexing;
        _denyDownload = denyDownload;
        _lifetime = lifetime;
        _watermark = watermark;
        _private = @private;
        RoomId = -1;
    }

    protected override async Task DoJob()
    {

        await using var scope = _serviceProvider.CreateAsyncScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var securityContext = scope.ServiceProvider.GetService<SecurityContext>();
        var fileStorageService = scope.ServiceProvider.GetService<FileStorageService>();
        var roomLogoManager = scope.ServiceProvider.GetService<RoomLogoManager>();
        var logger = scope.ServiceProvider.GetService<ILogger<CreateRoomFromTemplateOperation>>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        var folderDao = daoFactory.GetFolderDao<int>();

        try
        {
            Percentage = 0;
            await PublishChanges();

            await tenantManager.SetCurrentTenantAsync(TenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            LogoRequest dtoLogo = null;
            if (_logo != null && !_copyLogo)
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

            var room = await fileStorageService.CreateRoomFromTemplateAsync(_templateId, _title, _tags, dtoLogo, _cover, _color, _indexing, _denyDownload, _lifetime, _watermark, _private);
            RoomId = room.Id;

            if (_copyLogo)
            {
                try
                {
                    var template = await folderDao.GetFolderAsync(_templateId);
                    if (await roomLogoManager.CopyAsync(template, room))
                    {
                        room.SettingsHasLogo = true;
                        await folderDao.SaveFolderAsync(room);
                    }
                }
                catch (Exception ex)
                {
                    logger.WarningCanNotCopyLogo(ex);
                }
            }

            var fileDao = daoFactory.GetFileDao<int>();
            var files = await fileDao.GetFilesAsync(_templateId).ToListAsync();
            var folders = await folderDao.GetFoldersAsync(_templateId).Where(f => f.FolderType == FolderType.DEFAULT).Select(r => r.Id).ToListAsync();
            _totalCount = await fileDao.GetFilesCountAsync(_templateId, FilterType.None, false, Guid.Empty, string.Empty, null, false, true);

            foreach (var file in files)
            {
                try
                {
                    await fileDao.CopyFileAsync(file, RoomId);
                    await PublishAsync();
                }
                catch (Exception ex)
                {
                    logger.WarningCanNotCopyFile(ex);
                }
            }

            foreach (var f in folders)
            {
                try
                {
                    var newFolder = await folderDao.CopyFolderAsync(f, RoomId, CancellationToken);
                    var folderFiles = await fileDao.GetFilesAsync(f).ToListAsync();
                    foreach (var file in folderFiles)
                    {
                        await fileDao.CopyFileAsync(file, newFolder.Id);
                        await PublishAsync();
                    }
                }
                catch (Exception ex)
                {
                    logger.WarningCanNotCopyFolder(ex);
                }
            }

            if (_quota.HasValue)
            {
                await fileStorageService.FolderQuotaChangeAsync(room.Id, _quota.Value);
            }

            Percentage = 100;
        }
        catch (Exception ex)
        {
            logger.ErrorCreateRoomFromTemplate(ex);
            Exception = ex;
            if (RoomId != -1)
            {
                await folderDao.DeleteFolderAsync(RoomId);
            }
        }
        finally
        {
            IsCompleted = true;
            await PublishChanges();
        }
    }

    private async Task PublishAsync()
    {
        _count++;
        Percentage = _count * 0.9 / _totalCount;
        await PublishChanges();
    }
}