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
/// The outcome of one completed form-filling session, as the person who has just filled the form sees it.
/// </summary>
public class FillingFormResultDto<T>
{
    /// <summary>
    /// The number this copy was given among the copies made of the same form, counting up from 1. It is the number
    /// the results of the form are ordered by and the one the title of the copy carries.
    /// </summary>
    /// <example>1</example>
    public required int FormNumber { get; set; }

    /// <summary>
    /// The filled copy that the session produced, as an ordinary file: it can be read and downloaded with the file
    /// operations of this API.
    /// </summary>
    /// <example>{"id": 10, "title": "completed_form.pdf"}</example>
    public FileDto<T> CompletedForm { get; set; }

    /// <summary>
    /// The form the copy was made from, so that a client can offer filling it once more.
    /// </summary>
    /// <example>{"id": 5, "title": "form_template.pdf"}</example>
    public FileDto<T> OriginalForm { get; set; }

    /// <summary>
    /// The account that owns the original form, reported with its email address, so that the person who has just
    /// filled the form knows who receives it and whom to ask about it.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeFullDto Manager { get; set; }

    /// <summary>
    /// The room the form was filled in. It comes back as 0 when the session was reached through a link shared for
    /// that single form rather than for its room, in which case there is no room the caller could be sent to.
    /// </summary>
    /// <example>123</example>
    public required T RoomId { get; set; }

    /// <summary>
    /// Tells whether the calling account may open that room: true for a member of the room and for a portal
    /// administrator, in which case a client can offer going to the room; false for the anonymous caller who filled
    /// the form through a link and can only be shown the copy itself.
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