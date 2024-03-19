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

namespace ASC.Files.Core.Security;

[Scope]
public class FileValidator : IDataStoreValidator
{
    private readonly IDaoFactory _daoFactory;
    private readonly FileSecurity _fileSecurity;
    private readonly FileUtility _fileUtility;

    public FileValidator(FileSecurity fileSecurity, IDaoFactory daoFactory, FileUtility fileUtility)
    {
        _fileSecurity = fileSecurity;
        _daoFactory = daoFactory;
        _fileUtility = fileUtility;
    }

    public async Task<bool> Validate(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));
        
        if (FileDao.TryGetFileId(path, out var fileId))
        {
            var file = await _daoFactory.GetFileDao<int>().GetFileAsync(fileId);
            if (file == null)
            {
                return false;
            }

            if (_fileUtility.CanImageView(file.Title) || _fileUtility.CanMediaView(file.Title))
            {
                return true;
            }

            return await _fileSecurity.CanDownloadAsync(file);
        }

        var pathPart = path.Split(Path.DirectorySeparatorChar).FirstOrDefault();
        if (string.IsNullOrEmpty(pathPart) || !Guid.TryParse(pathPart, out var id))
        {
            return true;
        }

        var record = await _daoFactory.GetSecurityDao<int>().GetSharesAsync(new[] { id }).FirstOrDefaultAsync();
        if (record is { IsLink: true, Options: not null })
        {
            return !record.Options.DenyDownload;
        }

        return true;
    }
}