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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// Represents a data transfer object for creating a file in the editor.
/// This class contains the necessary information for creating a new file,
/// including details like the file URI, title, document type, and whether to open the folder.
/// </summary>
public class CreateFileInEditorRequestDto
{
    /// <summary>
    /// Gets or sets the response data used for a file creation process in the editor API.
    /// </summary>
    /// <remarks>
    /// This property is used to pass or receive additional response information
    /// when creating a new file in the editor. Its specific usage may vary depending
    /// on the implementation context within the file handling ecosystem.
    /// </remarks>
    [FromQuery]
    public string Response { get; set; }

    /// <summary>
    /// Gets or sets the URI of the file to be created or referenced in the editor.
    /// </summary>
    /// <remarks>
    /// This property typically represents the URL or path to the location where the file is
    /// being addressed. It is used within the request data transfer object during operations such as
    /// file creation in editors or other file handling contexts.
    /// </remarks>
    [FromQuery]
    public string FileUri { get; set; }

    /// Gets or sets the title of the file to be created in the editor.
    /// This property represents the name or title that will be assigned to the new file during its creation.
    /// It is expected to be a string value that identifies the file meaningfully.
    [FromQuery]
    public string Title { get; set; }

    /// <summary>
    /// Specifies the type or format of the document to be created or handled within the request.
    /// </summary>
    [FromQuery]
    public string DocType { get; set; }

    /// <summary>
    /// Determines whether the folder should be automatically opened after the file creation process.
    /// </summary>
    /// <remarks>
    /// When set to true, the folder where the file is created will be opened automatically in the application's UI.
    /// If set to false, the folder will not be opened, and no additional folder navigation will occur after file creation.
    /// </remarks>
    [FromQuery]
    public bool OpenFolder { get; set; }
}

/// <summary>
/// Represents a data transfer object to handle requests for creating a file in the editor.
/// </summary>
public class CreateFileInEditorRequestDto<T> : CreateFileInEditorRequestDto
{
    /// <summary>
    /// Gets or sets the identifier of the folder where the file will be created.
    /// </summary>
    /// <remarks>
    /// This property represents the unique identifier of a folder used as the destination
    /// when creating a file. The value type is generic, allowing flexibility
    /// in the format of the folder identifier (e.g., string, GUID, integer).
    /// Ensure the value corresponds to a valid folder identifier in the system.
    /// </remarks>
    [FromRoute]
    public T FolderId { get; set; }
}
