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


namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// </summary>
public class FillingFormResultDto<T>
{
    /// <summary>Form number</summary>
    /// <type>System.String, System</type>
    public int FormNumber { get; set; }

    /// <summary>Completed form</summary>
    /// <type>ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto, ASC.Files.Core</type>
    public FileDto<T> CompletedForm { get; set; }

    /// <summary>Original form</summary>
    /// <type>ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto, ASC.Files.Core</type>
    public FileDto<T> OriginalForm { get; set; }

    /// <summary>Manager</summary>
    /// <type>ASC.Web.Api.Models.EmployeeDto, ASC.Api.Core</type>
    public EmployeeFullDto Manager { get; set; }

    public T RoomId { get; set; }
    public bool isRoomMember { get; set; }

}

[Scope]
public class FillingFormResultDtoHelper(
    UserManager userManager,
    IDaoFactory daoFactory,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    ExternalShare externalShare,
    FileSharing fileSharing,
    AuthContext authContext)
{
    public async Task<FillingFormResultDto<T>> GetAsync<T>(T completedFormId)
    {
        var fileDao = daoFactory.GetFileDao<T>();

        var file = await fileStorageService.GetFileAsync(completedFormId, -1);

        var linkId = await externalShare.GetLinkIdAsync();
        var securityDao = daoFactory.GetSecurityDao<int>();
        var record = await securityDao.GetSharesAsync([linkId]).FirstOrDefaultAsync();


        if (file != null)
        {
            var properties = await fileDao.GetProperties(file.Id);

            if (properties is { FormFilling: not null })
            {

                var originalForm = await fileStorageService.GetFileAsync(properties.FormFilling.OriginalFormId, -1);
                var manager = await userManager.GetUsersAsync(originalForm.CreateBy);

                var folderDao = daoFactory.GetFolderDao<T>();

                var currentRoom = await folderDao.GetFolderAsync(properties.FormFilling.RoomId);
                var aces = await fileSharing.GetSharedInfoAsync(currentRoom);

                var currentType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

                var result = new FillingFormResultDto<T>
                {
                    CompletedForm = await fileDtoHelper.GetAsync(file),
                    OriginalForm = await fileDtoHelper.GetAsync(originalForm),
                    FormNumber = properties.FormFilling.ResultFormNumber,
                    Manager = await employeeFullDtoHelper.GetSimpleWithEmail(manager),
                    RoomId = record == null || record.EntryType == FileEntryType.Folder ? properties.FormFilling.RoomId : default,
                    isRoomMember = currentType == EmployeeType.DocSpaceAdmin || aces.Exists(u => u.Id == authContext.CurrentAccount.ID)
                };
                return result;
            }
        }


        return null;
    }
}
