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

using ASC.Files.Core.Services.DocumentBuilderService;

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The document builder task parameters.
/// </summary>
public class DocumentBuilderTaskDto
{
    /// <summary>
    /// The document builder ID.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The error message occured while the document building process.
    /// </summary>
    public required string Error { get; set; }

    /// <summary>
    /// The percentage of the progress of the document building process.
    /// </summary>
    public required int Percentage { get; set; }

    /// <summary>
    /// Specifies whether the document building process is completed or not.
    /// </summary>
    public required bool IsCompleted { get; set; }

    /// <summary>
    /// The status of the document building process.
    /// </summary>
    public required DistributedTaskStatus Status { get; set; }

    /// <summary>
    /// The result file ID.
    /// </summary>
    public required object ResultFileId { get; set; }

    /// <summary>
    /// The result file name.
    /// </summary>
    public required string ResultFileName { get; set; }

    /// <summary>
    /// The result file URL.
    /// </summary>
    public required string ResultFileUrl { get; set; }

    public static DocumentBuilderTaskDto Get<TId, TData>(DocumentBuilderTask<TId, TData> task)
    {
        return task == null
            ? null
            : new DocumentBuilderTaskDto
            {
                Id = task.Id,
                Error = task.Exception?.Message,
                Percentage = (int)task.Percentage,
                IsCompleted = task.IsCompleted,
                Status = task.Status,
                ResultFileId = task.ResultFileId,
                ResultFileName = task.ResultFileName,
                ResultFileUrl = task.ResultFileUrl
            };
    }
}
