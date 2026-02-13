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

namespace ASC.AI.Models.ResponseDto;

/// <summary>
/// The AI provider information.
/// </summary>
public class AiProviderDto
{
    /// <summary>
    /// The AI provider identifier.
    /// </summary>
    /// <example>1</example>
    public int Id { get; init; }

    /// <summary>
    /// The AI provider title.
    /// </summary>
    /// <example>OpenAI</example>
    public required string Title { get; init; }

    /// <summary>
    /// The AI provider type.
    /// </summary>
    /// <example>0</example>
    public ProviderType Type { get; init; }

    /// <summary>
    /// The AI provider URL.
    /// </summary>
    /// <example>https://api.openai.com/v1</example>
    public string? Url { get; init; }

    /// <summary>
    /// The creation date and time.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public required ApiDateTime CreatedOn { get; init; }

    /// <summary>
    /// The last modification date and time.
    /// </summary>
    /// <example>2025-06-15T12:45:00.0000000Z</example>
    public required ApiDateTime ModifiedOn { get; init; }

    /// <summary>
    /// Indicates whether the provider API key needs to be reset.
    /// </summary>
    /// <example>false</example>
    public bool NeedReset { get; init; }

    /// <summary>
    /// Indicates whether this is the default provider.
    /// </summary>
    /// <example>true</example>
    public bool IsDefault { get; init; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None,
    PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class ProviderMapper(ApiDateTimeHelper helper)
{
    public partial AiProviderDto MapToDto(AiProvider provider);

    private ApiDateTime MapDateTime(DateTime dateTime)
    {
        return helper.Get(dateTime);
    }
}