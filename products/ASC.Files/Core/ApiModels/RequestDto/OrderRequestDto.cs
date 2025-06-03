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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The parameters for ordering requests.
/// </summary>
public class OrderRequestDto
{
    /// <summary>
    /// The order value.
    /// </summary>
    [Range(1, int.MaxValue)]
    [JsonConverter(typeof(OrderRequestDtoConverter))]
    public int Order { get; set; }
}

/// <summary>
/// An item in the ordering request with its entry type and ID.
/// </summary>
public class OrdersItemRequestDto<T>
{
    /// <summary>
    /// The entry unique identifier (file or folder).
    /// </summary>
    public T EntryId { get; set; }

    /// <summary>
    /// The entry type (file or folder).
    /// </summary>
    public FileEntryType EntryType { get; set; }

    /// <summary>
    /// The order value.
    /// </summary>
    [Range(1, int.MaxValue)]
    [JsonConverter(typeof(OrderRequestDtoConverter))]
    public int Order { get; set; }
}

/// <summary>
/// The collection of items to be ordered.
/// </summary>
public class OrdersRequestDto<T>
{
    /// <summary>
    /// The list of items with their ordering information.
    /// </summary>
    public List<OrdersItemRequestDto<T>> Items { get; set; }
}

/// <summary>
/// The JSON converter for handling order values in different formats.
/// </summary>
public class OrderRequestDtoConverter : System.Text.Json.Serialization.JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var order))
        {
            return order;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var orderString = reader.GetString();
            if (!string.IsNullOrEmpty(orderString))
            {
                var path = orderString.Split('.');
                if (int.TryParse(path.Last(), out var pathOrder))
                {
                    return pathOrder;
                }
            }
        }

        throw new ArgumentException("order");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// The request parameters for ordering a file.
/// </summary>
public class OrderFileRequestDto<T>
{
    /// <summary>
    /// The file unique identifier.
    /// </summary>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The file order information.
    /// </summary>
    [FromBody]
    public OrderRequestDto Order { get; set; }
}

/// <summary>
/// The request parameters for ordering a folder.
/// </summary>
public class OrderFolderRequestDto<T>
{
    /// <summary>
    /// The folder unique identifier.
    /// </summary>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The folder order information.
    /// </summary>
    [FromBody]
    public OrderRequestDto Order { get; set; }
}