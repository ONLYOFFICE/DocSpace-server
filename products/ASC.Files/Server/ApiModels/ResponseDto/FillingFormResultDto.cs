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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The parameters of the form filling result.
/// </summary>
public class FillingFormResultDto<T>
{
    /// <summary>
    /// The filling form number.
    /// </summary>
    /// <example>1</example>
    public required int FormNumber { get; set; }

    /// <summary>
    /// The file with the completed forms.
    /// </summary>
    /// <example>{"id": 10, "title": "completed_form.pdf"}</example>
    public FileDto<T> CompletedForm { get; set; }

    /// <summary>
    /// The file with the original forms.
    /// </summary>
    /// <example>{"id": 5, "title": "form_template.pdf"}</example>
    public FileDto<T> OriginalForm { get; set; }

    /// <summary>
    /// The manager who is filling the form.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeFullDto Manager { get; set; }

    /// <summary>
    /// The room ID where filling the form.
    /// </summary>
    /// <example>123</example>
    public required T RoomId { get; set; }

    /// <summary>
    /// Specifies if the manager who fills the form is a room member or not.
    /// </summary>
    /// <example>true</example>
    public bool IsRoomMember { get; set; }

}

[Scope]
public class FillingFormResultDtoHelper(
    UserManager userManager,
    IDaoFactory daoFactory,
    FileDtoHelper fileDtoHelper,
    EmployeeFullDtoHelper employeeFullDtoHelper,
    ExternalShare externalShare,
    FileSharing fileSharing,
    AuthContext authContext)
{
    public async Task<FillingFormResultDto<T>> GetAsync<T>(T completedFormId)
    {
        var fileDao = daoFactory.GetFileDao<T>();

        var file = await fileDao.GetFileAsync(completedFormId);

        var linkId = await externalShare.GetLinkIdAsync();
        var securityDao = daoFactory.GetSecurityDao<int>();
        var record = await securityDao.GetSharesAsync([linkId]).FirstOrDefaultAsync();


        if (file != null)
        {
            var properties = await fileDao.GetProperties(file.Id);

            if (properties is { FormFilling: not null })
            {

                var originalForm = await fileDao.GetFileAsync(properties.FormFilling.OriginalFormId);
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
                    IsRoomMember = currentType == EmployeeType.DocSpaceAdmin || aces.Exists(u => u.Id == authContext.CurrentAccount.ID)
                };
                return result;
            }
        }


        return null;
    }
}