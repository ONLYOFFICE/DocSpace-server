﻿// (c) Copyright Ascensio System SIA 2009-2025
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

using Status = ASC.Files.Core.Security.Status;

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The external sharing information and validation data.
/// </summary>
public class ExternalShareDto : IMapFrom<ValidationInfo>
{
    /// <summary>
    /// The external data status.
    /// </summary>
    public Status Status { get; set; }

    /// <summary>
    /// The external data ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The external data title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// The unique identifier of the shared entity.
    /// </summary>
    public string EntityId { get; set; }
   
    /// <summary>
    /// The title of the shared entry.
    /// </summary>
    public string EntryTitle { get; set; }

    /// <summary>
    /// Specifies whether to share the external data or not.
    /// </summary>
    public bool Shared { get; set; }

    /// <summary>
    /// The link ID of the external data.
    /// </summary>
    public Guid LinkId { get; set; }
    
    /// <summary>
    /// Specifies whether the user is authenticated or not.
    /// </summary>
    public bool IsAuthenticated { get; set; }
}