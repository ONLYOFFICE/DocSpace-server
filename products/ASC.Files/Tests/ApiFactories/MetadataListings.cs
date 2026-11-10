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

namespace ASC.Files.Tests.ApiFactories;

/// <summary>
/// Lookups by name over the generated metadata models of the SDK.
/// </summary>
public static class MetadataModelExtensions
{
    public static MetadataFieldDto Field(this MetadataTemplateDto template, string name)
    {
        return template.Fields.Single(f => f.Name == name);
    }

    public static EntryFieldDto Field(this EntryTemplateDto template, string name)
    {
        return template.Fields.Single(f => f.Name == name);
    }

    public static Guid Option(this MetadataFieldDto field, string value)
    {
        return field.Options.Single(o => o.Value == value).Id;
    }

    /// <summary>
    /// The fields holding a value on the entry.
    /// </summary>
    public static IEnumerable<EntryFieldDto> SetFields(this EntryTemplateDto template)
    {
        return template.Fields.Where(f => f.Value != null);
    }
}

/// <summary>
/// The listings and searches with the metadata filter, through the typed SDK calls. <c>FolderContentDto.Folders</c>
/// and <c>Files</c> are declared as the base <c>FileEntryBaseDto</c>, but the <c>entryKind</c> discriminator makes
/// the SDK build each entry as its concrete model, so the <c>id</c>, <c>assignedMetadataTemplates</c> and
/// <c>metadataMatches</c> are read from <c>FileEntryDto</c>.
/// </summary>
public static class MetadataListings
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Newtonsoft.Json.JsonSerializerSettings _filterSettings = new()
    {
        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
    };

    /// <summary>
    /// The <c>metadataFilters</c> query parameter: the SDK takes it as a JSON string the caller builds.
    /// </summary>
    public static string? SerializeFilters(IEnumerable<MetadataFilterConditionRequest>? filters)
    {
        return filters == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(filters.ToList(), _filterSettings);
    }

    public static async Task<RoomsContentResponse> GetRoomsWithMetadataAsync(
        this RoomsApi api,
        int? metadataTemplateId = null,
        IEnumerable<MetadataFilterConditionRequest>? metadataFilters = null,
        string? filterValue = null,
        SearchArea? searchArea = null,
        List<RoomType>? type = null,
        bool? includeHiddenMetadata = null,
        string? rawMetadataFilters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await api.GetRoomsFolderAsync(
            type: type,
            searchArea: searchArea,
            filterValue: filterValue,
            metadataTemplateId: metadataTemplateId,
            metadataFilters: rawMetadataFilters ?? SerializeFilters(metadataFilters),
            includeHiddenMetadata: includeHiddenMetadata,
            cancellationToken: cancellationToken);

        return RoomsContentResponse.From(response.Response);
    }

    /// <summary>
    /// The folder content. <paramref name="withSubFolders"/> left unset means the endpoint's own default of
    /// <c>true</c>: the search goes through the whole subtree.
    /// </summary>
    public static async Task<FolderContentResponse> GetFolderContentWithMetadataAsync(
        this FoldersApi api,
        int folderId,
        int? metadataTemplateId = null,
        IEnumerable<MetadataFilterConditionRequest>? metadataFilters = null,
        string? filterValue = null,
        bool? withSubFolders = null,
        string? extension = null,
        bool? includeHiddenMetadata = null,
        string? rawMetadataFilters = null,
        CancellationToken cancellationToken = default)
    {
        var response = await api.GetFolderByFolderIdAsync(
            folderId,
            withSubFolders: withSubFolders,
            extension: extension,
            filterValue: filterValue,
            metadataTemplateId: metadataTemplateId,
            metadataFilters: rawMetadataFilters ?? SerializeFilters(metadataFilters),
            includeHiddenMetadata: includeHiddenMetadata,
            cancellationToken: cancellationToken);

        return FolderContentResponse.From(response.Response);
    }

    /// <summary>
    /// A section requested by its alias. Only the aliases the SDK has a method for: <c>@share</c> and
    /// <c>@templates</c> are not in the OpenAPI document and are requested over raw HTTP by the tests that need them.
    /// </summary>
    public static async Task<FolderContentResponse> GetSectionWithMetadataAsync(
        this FoldersApi api,
        string section,
        int? metadataTemplateId = null,
        IEnumerable<MetadataFilterConditionRequest>? metadataFilters = null,
        CancellationToken cancellationToken = default)
    {
        var filters = SerializeFilters(metadataFilters);

        var response = section switch
        {
            "@my" => await api.GetMyFolderAsync(metadataTemplateId: metadataTemplateId, metadataFilters: filters, cancellationToken: cancellationToken),
            "@trash" => await api.GetTrashFolderAsync(metadataTemplateId: metadataTemplateId, metadataFilters: filters, cancellationToken: cancellationToken),
            "@recent" => await api.GetRecentFolderAsync(metadataTemplateId: metadataTemplateId, metadataFilters: filters, cancellationToken: cancellationToken),
            "@favorites" => await api.GetFavoritesFolderAsync(metadataTemplateId: metadataTemplateId, metadataFilters: filters, cancellationToken: cancellationToken),
            "@forms" => await api.GetFormsFolderAsync(metadataTemplateId: metadataTemplateId, metadataFilters: filters, cancellationToken: cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, "The SDK has no method for this section alias.")
        };

        return FolderContentResponse.From(response.Response);
    }

    public static async Task<FolderContentResponse> SearchFolderWithMetadataAsync(this FoldersApi api, int folderId, FolderMetadataSearch body, CancellationToken cancellationToken = default)
    {
        var response = await api.SearchFolderAsync(folderId, body, cancellationToken);

        return FolderContentResponse.From(response.Response);
    }

    public static async Task<RoomsContentResponse> SearchRoomsWithMetadataAsync(this RoomsApi api, RoomsMetadataSearchRequestDto body, CancellationToken cancellationToken = default)
    {
        var response = await api.SearchRoomsAsync(body, cancellationToken);

        return RoomsContentResponse.From(response.Response);
    }

    /// <summary>
    /// Reads the <c>response</c> of the API envelope, for a listing requested over raw HTTP.
    /// </summary>
    public static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        return Read<T>(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private static T Read<T>(string rawContent)
    {
        var wrapper = JsonSerializer.Deserialize<RawApiResponse<T>>(rawContent, _jsonOptions);

        if (wrapper is null || wrapper.Response is null)
        {
            throw new InvalidOperationException("Empty response body.");
        }

        return wrapper.Response;
    }
}

public class RoomsContentResponse
{
    public List<RoomEntryResponse> Folders { get; init; } = [];
    public int Total { get; init; }
    public int Count { get; init; }

    public static RoomsContentResponse From(FolderContentDto content)
    {
        return new RoomsContentResponse
        {
            Folders = RoomEntryResponse.From(content.Folders),
            Total = content.Total,
            Count = content.Count
        };
    }

    public List<int> RoomIds()
    {
        return Folders.Select(f => f.Id).ToList();
    }
}

public class FolderContentResponse
{
    public List<RoomEntryResponse> Folders { get; init; } = [];
    public List<RoomEntryResponse> Files { get; init; } = [];
    public int Total { get; init; }
    public int Count { get; init; }

    public static FolderContentResponse From(FolderContentDto content)
    {
        return new FolderContentResponse
        {
            Folders = RoomEntryResponse.From(content.Folders),
            Files = RoomEntryResponse.From(content.Files),
            Total = content.Total,
            Count = content.Count
        };
    }

    public List<int> FolderIds()
    {
        return Folders.Select(f => f.Id).ToList();
    }

    public List<int> FileIds()
    {
        return Files.Select(f => f.Id).ToList();
    }
}

/// <summary>
/// An entry of a listing: the metadata part of its <c>FileEntryDto</c>. Also the shape of a listing requested over
/// raw HTTP through <see cref="MetadataListings.ReadAsync{T}"/>.
/// </summary>
public class RoomEntryResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public List<int>? AssignedMetadataTemplates { get; init; }
    public List<MetadataMatchResponse>? MetadataMatches { get; init; }

    public static List<RoomEntryResponse> From(List<FileEntryBaseDto>? entries)
    {
        return (entries ?? []).OfType<FileEntryDto>()
            .Select(e => new RoomEntryResponse
            {
                Id = e.Id,
                Title = e.Title,
                AssignedMetadataTemplates = e.AssignedMetadataTemplates,
                MetadataMatches = e.MetadataMatches?.Select(m => new MetadataMatchResponse
                {
                    TemplateId = m.TemplateId,
                    FieldId = m.FieldId,
                    FieldName = m.FieldName ?? "",
                    Value = m.Value ?? ""
                }).ToList()
            })
            .ToList();
    }
}

/// <summary>
/// Where the text search met the metadata of an entry: the field and the whole stored value.
/// </summary>
public class MetadataMatchResponse
{
    public int? TemplateId { get; init; }
    public int? FieldId { get; init; }
    public string FieldName { get; init; } = "";
    public string Value { get; init; } = "";
}
