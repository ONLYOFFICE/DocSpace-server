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

public class EditHistoryDto
{
    [SwaggerSchemaCustom(Example = "1234", Description = "File ID", Format = "int32")]
    public int ID { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Key")]
    public string Key { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "File version", Format = "int32")]
    public int Version { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "Version group", Format = "int32")]
    public int VersionGroup { get; set; }

    [SwaggerSchemaCustom(Description = "A user who updated a file")]
    public EditHistoryAuthor User { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "Creation time")]
    public ApiDateTime Created { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "History changes in the string format")]
    public string ChangesHistory { get; set; }

    [SwaggerSchemaCustom(Description = "List of history changes")]
    public List<EditHistoryChangesWrapper> Changes { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Server version")]
    public string ServerVersion { get; set; }

    public EditHistoryDto(EditHistory editHistory, ApiDateTimeHelper apiDateTimeHelper, UserManager userManager, DisplayUserSettingsHelper displayUserSettingsHelper)
    {
        ID = editHistory.ID;
        Key = editHistory.Key;
        Version = editHistory.Version;
        VersionGroup = editHistory.VersionGroup;
        Changes = editHistory.Changes.Select(r => new EditHistoryChangesWrapper(r, apiDateTimeHelper)).ToList();
        ChangesHistory = editHistory.ChangesString;
        Created = apiDateTimeHelper.Get(editHistory.ModifiedOn);
        User = new EditHistoryAuthor(userManager, displayUserSettingsHelper) { Id = editHistory.ModifiedBy.ToString() };
        ServerVersion = editHistory.ServerVersion;
    }
}

public class EditHistoryChangesWrapper(EditHistoryChanges historyChanges, ApiDateTimeHelper apiDateTimeHelper)
{
    [SwaggerSchemaCustom(Description = "")]
    public EditHistoryAuthor User { get; set; } = historyChanges.Author;

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "")]
    public ApiDateTime Created { get; set; } = apiDateTimeHelper.Get(historyChanges.Date);
}
