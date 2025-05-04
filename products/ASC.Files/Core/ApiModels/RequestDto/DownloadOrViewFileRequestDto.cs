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

/// Represents the data transfer object for handling requests to download or view a file.
/// This class is used to process incoming requests related to files, including downloading
/// specific versions or viewing files with specified output options. It includes properties
/// for identifying the target file, the version of the file, and customization options such
/// as the desired file format and whether to use preview conversions during the request.
/// Type Parameter:
/// T:
/// The type representing the file identifier, which can vary depending on the implementation.
public class DownloadOrViewFileRequestDto<T>
{
    /// Gets or sets the identifier of the file to be downloaded or viewed.
    /// This property is used as part of the route in API operations for file handling.
    /// The value represents a unique identifier for the file, which could vary in type
    /// depending on the implementation.
    [FromRoute]
    public required T FileId { get; set; }

    /// Represents the version of the file being downloaded or viewed.
    /// This property is used to specify the particular version of the file
    /// to be accessed in a request. It is provided as part of the route data
    /// and allows operations to target a specific version of the resource.
    [FromQuery]
    public int Version { get; set; }

    /// Gets or sets the type of output for the operation (e.g., file content or file metadata).
    /// This property specifies the format or representation of the requested file output.
    /// It is used in file download or view operations to determine how the content should be delivered.
    [FromQuery]
    public string OutputType { get; set; }

    /// Gets or sets a value indicating whether the file conversion for preview should be triggered.
    /// This property is used to determine if the file should be converted to a preview-compatible format
    /// during the download or view operation.
    [FromQuery]
    public bool ConvPreview { get; set; }
}
