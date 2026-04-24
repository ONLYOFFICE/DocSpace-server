// (c) Copyright Ascensio System SIA 2009-2026
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
/// The external DB synchronization task parameters.
/// </summary>
public class ExternalDbSyncTaskDto
{
    /// <summary>
    /// The task ID.
    /// </summary>
    /// <example>ExternalDbSyncTask_1_42</example>
    public required string Id { get; set; }

    /// <summary>
    /// The error message if the synchronization failed.
    /// </summary>
    /// <example>Connection refused</example>
    public string? Error { get; set; }

    /// <summary>
    /// The progress percentage of the synchronization.
    /// </summary>
    /// <example>75</example>
    public required int Percentage { get; set; }

    /// <summary>
    /// Specifies whether the synchronization is completed or not.
    /// </summary>
    /// <example>false</example>
    public required bool IsCompleted { get; set; }

    /// <summary>
    /// The status of the synchronization task.
    /// </summary>
    /// <example>0</example>
    public required DistributedTaskStatus Status { get; set; }

    /// <summary>
    /// The synchronization results for all original forms in the room.
    /// </summary>
    public required List<ExternalDbSyncFormResultDto> Forms { get; set; }

    public static ExternalDbSyncTaskDto? Get(ExternalDbSyncTask task)
    {
        return task == null
            ? null
            : new ExternalDbSyncTaskDto
            {
                Id = task.Id,
                Error = task.Exception?.Message,
                Percentage = (int)task.Percentage,
                IsCompleted = task.IsCompleted,
                Status = task.Status,
                Forms = task.Forms
            };
    }
}
