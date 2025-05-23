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
public class CreateRoomTemplateOperation : DistributedTaskProgress
{
    private Guid _userId;
    private LogoSettings _logo;
    private bool _copyLogo;
    private IEnumerable<string> _tags;
    private IEnumerable<string> _emails;
    private IEnumerable<Guid> _groups;
    private string _title;
    private string _cover;
    private string _color;
    private long? _quota;

    private int _roomId;
    private int _totalCount;
    private int _count;
    private readonly IServiceProvider _serviceProvider;
    public int TenantId { get; set; }

    public int TemplateId { get; set; }
    
    public CreateRoomTemplateOperation()
    {
        
    }
    
    public CreateRoomTemplateOperation(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public void Init(int tenantId,
        Guid userId,
        int roomId,
        string title,
        IEnumerable<string> emails,
        LogoSettings logo,
        bool copyLogo,
        IEnumerable<string> tags,
        IEnumerable<Guid> groups,
        string cover,
        string color,
        long? quota)
    {
        TenantId = tenantId;
        _userId = userId;
        _roomId = roomId;
        _logo = logo;
        _copyLogo = copyLogo;
        _tags = tags;
        _emails = emails;
        _title = title;
        _groups = groups;
        _cover = cover;
        _color = color;
        _quota = quota;
        TemplateId = -1;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var securityContext = scope.ServiceProvider.GetService<SecurityContext>();
        var fileStorageService = scope.ServiceProvider.GetService<FileStorageService>();
        var roomLogoManager = scope.ServiceProvider.GetService<RoomLogoManager>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        var logger = scope.ServiceProvider.GetService<ILogger<CreateRoomTemplateOperation>>();
        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();

        try
        {
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

            var template = await fileStorageService.CreateRoomTemplateAsync(_roomId, _title, new List<FileShareParams>(), _tags, dtoLogo, _cover, _color);
            TemplateId = template.Id;

            List<AceWrapper> wrappers = null;
            
            if (_emails != null)
            {
                wrappers = _emails.Select(e => new AceWrapper { Email = e, Access = FileShare.Read }).ToList();
            }
            if (_groups != null)
            {
                var groupWrappers = _groups.Select(e => new AceWrapper { Id = e, Access = FileShare.Read, SubjectType = SubjectType.Group }).ToList();

                if (wrappers == null)
                {
                    wrappers = groupWrappers;
                }
                else
                {
                    wrappers.AddRange(groupWrappers);
                }
            }

            if (wrappers != null)
            {
                var aceCollection = new AceCollection<int>
                {
                    Files = [],
                    Folders = [TemplateId],
                    Aces = wrappers,
                    Message = string.Empty
                };

                await fileStorageService.SetAceObjectAsync(aceCollection, false);
            }

            if (_copyLogo)
            {
                try
                {
                    var room = await folderDao.GetFolderAsync(_roomId);
                    if (await roomLogoManager.CopyAsync(room, template))
                    {
                        template.SettingsHasLogo = true;
                        await folderDao.SaveFolderAsync(template);
                    }
                }
                catch(Exception ex)
                {
                    logger.WarningCanNotCopyLogo(ex);
                }
            }

            _totalCount = await fileDao.GetFilesCountAsync(_roomId, FilterType.None, false, Guid.Empty, string.Empty, null, false, true);
            var files = fileDao.GetFilesAsync(_roomId);
            var folders = folderDao.GetFoldersAsync(_roomId).Where(f=> f.FolderType == FolderType.DEFAULT).Select(r => r.Id);
            
            await foreach (var file in files)
            {
                try
                {
                    await fileDao.CopyFileAsync(file, TemplateId);
                    await PublishAsync();
                }
                catch(Exception ex)
                {
                    logger.WarningCanNotCopyFile(ex);
                }
            }

            await foreach (var f in folders)
            {
                try
                {
                    var folder = await folderDao.GetFolderAsync(f);
                    if (folder.FolderType != FolderType.DEFAULT)
                    {
                        continue;
                    }
                    var newFolder = await folderDao.CopyFolderAsync(f, TemplateId, CancellationToken);
                    var folderFiles = fileDao.GetFilesAsync(f);
                    await foreach (var file in folderFiles)
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
                await fileStorageService.FolderQuotaChangeAsync(template.Id, _quota.Value);
            }

            Percentage = 100;
            IsCompleted = true;
        }
        catch (Exception ex)
        {
            logger.ErrorCreateRoomTemplate(ex);
            Exception = ex;
            IsCompleted = true;
            if (TemplateId != -1) 
            {
                await folderDao.DeleteFolderAsync(TemplateId);
            }
        }
        finally
        {
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
