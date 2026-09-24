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
/// The raw HTTP client for the metadata endpoints and for the rooms listing with the metadata filter.
/// The generated SDK has no metadata API and its rooms method does not expose the metadata query parameters.
/// </summary>
public class MetadataApiClient(HttpClient client)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #region Templates and fields

    public async Task<MetadataTemplateResponse> CreateTemplateAsync(string name, IEnumerable<MetadataFieldPayload> fields, CancellationToken cancellationToken, bool visible = true)
    {
        using var response = await CreateTemplateResponseAsync(name, fields, cancellationToken, visible);

        return await ReadAsync<MetadataTemplateResponse>(response, cancellationToken);
    }

    /// <summary>
    /// Creates a template. Returns the raw response so the error cases can be asserted on the status code.
    /// </summary>
    public Task<HttpResponseMessage> CreateTemplateResponseAsync(string name, IEnumerable<MetadataFieldPayload> fields, CancellationToken cancellationToken, bool visible = true)
    {
        var body = new { name, visible, fields = fields.ToList() };

        return PostAsync("api/2.0/files/metadata/templates", body, cancellationToken);
    }

    public async Task<List<MetadataTemplateResponse>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync("api/2.0/files/metadata/templates", cancellationToken);

        return await ReadAsync<List<MetadataTemplateResponse>>(response, cancellationToken);
    }

    /// <summary>
    /// Requests a template by its id. Returns the raw response so the error cases can be asserted on the status code.
    /// </summary>
    public Task<HttpResponseMessage> GetTemplateResponseAsync(int templateId, CancellationToken cancellationToken)
    {
        return client.GetAsync($"api/2.0/files/metadata/templates/{templateId}", cancellationToken);
    }

    public async Task<MetadataTemplateResponse> UpdateTemplateAsync(int templateId, object body, CancellationToken cancellationToken)
    {
        using var response = await PutAsync($"api/2.0/files/metadata/templates/{templateId}", body, cancellationToken);

        return await ReadAsync<MetadataTemplateResponse>(response, cancellationToken);
    }

    public async Task<MetadataTemplateResponse> GetTemplateAsync(int templateId, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"api/2.0/files/metadata/templates/{templateId}", cancellationToken);

        return await ReadAsync<MetadataTemplateResponse>(response, cancellationToken);
    }

    /// <summary>
    /// Updates a field with an arbitrary body. Returns the raw response so the error cases can be asserted on the status code.
    /// </summary>
    public Task<HttpResponseMessage> UpdateFieldResponseAsync(int templateId, int fieldId, object body, CancellationToken cancellationToken)
    {
        return PutAsync($"api/2.0/files/metadata/templates/{templateId}/fields/{fieldId}", body, cancellationToken);
    }

    public async Task<MetadataFieldResponse> UpdateFieldAsync(int templateId, int fieldId, object body, CancellationToken cancellationToken)
    {
        using var response = await UpdateFieldResponseAsync(templateId, fieldId, body, cancellationToken);

        return await ReadAsync<MetadataFieldResponse>(response, cancellationToken);
    }

    #endregion

    #region Assignment and values

    /// <summary>
    /// Assigns the templates to the folder. <paramref name="conflictResolveType"/> is the <c>MetadataConflictResolveType</c>
    /// value the cascade propagates with: Skip = 0 keeps the values the sub-entries already hold, Overwrite = 1 replaces them.
    /// </summary>
    public Task AssignFolderTemplatesAsync(int folderId, IEnumerable<int> templateIds, bool cascade, CancellationToken cancellationToken, int conflictResolveType = 0)
    {
        return AssignTemplatesAsync("folder", folderId, templateIds, cascade, conflictResolveType, cancellationToken);
    }

    public Task AssignFileTemplatesAsync(int fileId, IEnumerable<int> templateIds, CancellationToken cancellationToken)
    {
        return AssignTemplatesAsync("file", fileId, templateIds, cascade: false, conflictResolveType: 0, cancellationToken);
    }

    public async Task<MetadataOperationResponse?> GetCascadeProgressAsync(int folderId, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"api/2.0/files/metadata/folder/{folderId}/templates/progress", cancellationToken);

        response.EnsureSuccessStatusCode();

        // a folder that never cascaded is answered with a completed operation without an id; the wrapper is still read
        // without the non-null check so a regression to a null body shows up as a null here instead of an exception
        var wrapper = await response.Content.ReadFromJsonAsync<MetadataApiResponse<MetadataOperationResponse>>(_jsonOptions, cancellationToken);

        return wrapper?.Response;
    }

    public async Task UnassignFolderTemplateAsync(int folderId, int templateId, CancellationToken cancellationToken)
    {
        using var response = await client.DeleteAsync($"api/2.0/files/metadata/folder/{folderId}/templates/{templateId}", cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task UnassignFileTemplateAsync(int fileId, int templateId, CancellationToken cancellationToken)
    {
        using var response = await client.DeleteAsync($"api/2.0/files/metadata/file/{fileId}/templates/{templateId}", cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public Task SetFolderValuesAsync(int folderId, IEnumerable<MetadataValuePayload> values, CancellationToken cancellationToken)
    {
        return SetValuesAsync("folder", folderId, values, cancellationToken);
    }

    public Task SetFileValuesAsync(int fileId, IEnumerable<MetadataValuePayload> values, CancellationToken cancellationToken)
    {
        return SetValuesAsync("file", fileId, values, cancellationToken);
    }

    /// <summary>
    /// Assigns the templates to the folder and returns the operation the endpoint answers with.
    /// </summary>
    public async Task<MetadataOperationResponse> AssignFolderTemplatesWithStatusAsync(int folderId, IEnumerable<int> templateIds, bool cascade, CancellationToken cancellationToken, int conflictResolveType = 0)
    {
        var body = new { templateIds = templateIds.ToList(), cascade, conflictResolveType };

        using var response = await PutAsync($"api/2.0/files/metadata/folder/{folderId}/templates", body, cancellationToken);

        return await ReadAsync<MetadataOperationResponse>(response, cancellationToken);
    }

    /// <summary>
    /// Sets the values on the folder and returns the entry metadata the endpoint answers with.
    /// </summary>
    public async Task<EntryMetadataSetResponse> SetFolderValuesWithResultAsync(int folderId, IEnumerable<MetadataValuePayload> values, CancellationToken cancellationToken)
    {
        using var response = await PutAsync($"api/2.0/files/metadata/folder/{folderId}/values", new { values = values.ToList() }, cancellationToken);

        return await ReadAsync<EntryMetadataSetResponse>(response, cancellationToken);
    }

    private async Task AssignTemplatesAsync(string entryKind, int entryId, IEnumerable<int> templateIds, bool cascade, int conflictResolveType, CancellationToken cancellationToken)
    {
        var body = new { templateIds = templateIds.ToList(), cascade, conflictResolveType };

        using var response = await PutAsync($"api/2.0/files/metadata/{entryKind}/{entryId}/templates", body, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task SetValuesAsync(string entryKind, int entryId, IEnumerable<MetadataValuePayload> values, CancellationToken cancellationToken)
    {
        using var response = await SetValuesResponseAsync(entryKind, entryId, values, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Sets the values of the folder. Returns the raw response so the error cases can be asserted on the status code.
    /// </summary>
    public Task<HttpResponseMessage> SetFolderValuesResponseAsync(int folderId, IEnumerable<MetadataValuePayload> values, CancellationToken cancellationToken)
    {
        return SetValuesResponseAsync("folder", folderId, values, cancellationToken);
    }

    private Task<HttpResponseMessage> SetValuesResponseAsync(string entryKind, int entryId, IEnumerable<MetadataValuePayload> values, CancellationToken cancellationToken)
    {
        var body = new { values = values.ToList() };

        return PutAsync($"api/2.0/files/metadata/{entryKind}/{entryId}/values", body, cancellationToken);
    }

    /// <summary>
    /// Sets the custom fields of the folder by name. Returns the raw response so the error cases can be asserted on the status code.
    /// </summary>
    public Task<HttpResponseMessage> SetFolderCustomFieldsResponseAsync(int folderId, IEnumerable<CustomFieldPayload> fields, CancellationToken cancellationToken)
    {
        return PutAsync($"api/2.0/files/metadata/folder/{folderId}/customFields", new { fields = fields.ToList() }, cancellationToken);
    }

    public Task<HttpResponseMessage> SetFileCustomFieldsResponseAsync(int fileId, IEnumerable<CustomFieldPayload> fields, CancellationToken cancellationToken)
    {
        return PutAsync($"api/2.0/files/metadata/file/{fileId}/customFields", new { fields = fields.ToList() }, cancellationToken);
    }

    public async Task<List<CustomFieldValueResponse>> SetFolderCustomFieldsAsync(int folderId, IEnumerable<CustomFieldPayload> fields, CancellationToken cancellationToken)
    {
        using var response = await SetFolderCustomFieldsResponseAsync(folderId, fields, cancellationToken);

        return await ReadAsync<List<CustomFieldValueResponse>>(response, cancellationToken);
    }

    public async Task<List<CustomFieldValueResponse>> SetFileCustomFieldsAsync(int fileId, IEnumerable<CustomFieldPayload> fields, CancellationToken cancellationToken)
    {
        using var response = await SetFileCustomFieldsResponseAsync(fileId, fields, cancellationToken);

        return await ReadAsync<List<CustomFieldValueResponse>>(response, cancellationToken);
    }

    public Task<List<CustomFieldValueResponse>> SetFolderCustomFieldAsync(int folderId, string name, string? value, CancellationToken cancellationToken)
    {
        return SetFolderCustomFieldsAsync(folderId, [new CustomFieldPayload(name, value)], cancellationToken);
    }

    public Task<List<CustomFieldValueResponse>> SetFileCustomFieldAsync(int fileId, string name, string? value, CancellationToken cancellationToken)
    {
        return SetFileCustomFieldsAsync(fileId, [new CustomFieldPayload(name, value)], cancellationToken);
    }

    /// <summary>
    /// The templates assigned to the folder with their values; the custom fields come from <see cref="GetFolderCustomFieldsAsync"/>.
    /// </summary>
    public async Task<List<EntryTemplateResponse>> GetFolderMetadataAsync(int folderId, CancellationToken cancellationToken)
    {
        return (await GetEntryMetadataAsync("folder", folderId, cancellationToken)).Templates;
    }

    public async Task<List<EntryTemplateResponse>> GetFileMetadataAsync(int fileId, CancellationToken cancellationToken)
    {
        return (await GetEntryMetadataAsync("file", fileId, cancellationToken)).Templates;
    }

    public async Task<List<CustomFieldValueResponse>> GetFolderCustomFieldsAsync(int folderId, CancellationToken cancellationToken)
    {
        return (await GetEntryMetadataAsync("folder", folderId, cancellationToken)).CustomFields;
    }

    public async Task<List<CustomFieldValueResponse>> GetFileCustomFieldsAsync(int fileId, CancellationToken cancellationToken)
    {
        return (await GetEntryMetadataAsync("file", fileId, cancellationToken)).CustomFields;
    }

    private async Task<EntryMetadataSetResponse> GetEntryMetadataAsync(string entryKind, int entryId, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"api/2.0/files/metadata/{entryKind}/{entryId}", cancellationToken);

        return await ReadAsync<EntryMetadataSetResponse>(response, cancellationToken);
    }

    #endregion

    #region Rooms listing

    /// <summary>
    /// Requests the rooms listing. Returns the raw response so the error cases can be asserted on the status code.
    /// </summary>
    public async Task<HttpResponseMessage> GetRoomsResponseAsync(
        int? metadataTemplateId = null,
        IEnumerable<object>? metadataFilters = null,
        string? filterValue = null,
        int? searchArea = null,
        int? roomType = null,
        string? rawMetadataFilters = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();

        if (metadataTemplateId.HasValue)
        {
            query.Add($"metadataTemplateId={metadataTemplateId.Value}");
        }

        var filtersJson = rawMetadataFilters ?? (metadataFilters == null ? null : JsonSerializer.Serialize(metadataFilters, _jsonOptions));

        if (filtersJson != null)
        {
            query.Add($"metadataFilters={Uri.EscapeDataString(filtersJson)}");
        }

        if (!string.IsNullOrEmpty(filterValue))
        {
            query.Add($"filterValue={Uri.EscapeDataString(filterValue)}");
        }

        if (searchArea.HasValue)
        {
            query.Add($"searchArea={searchArea.Value}");
        }

        if (roomType.HasValue)
        {
            query.Add($"type={roomType.Value}");
        }

        var path = "api/2.0/files/rooms" + (query.Count > 0 ? "?" + string.Join('&', query) : "");

        return await client.GetAsync(path, cancellationToken);
    }

    public async Task<RoomsContentResponse> GetRoomsAsync(
        int? metadataTemplateId = null,
        IEnumerable<object>? metadataFilters = null,
        string? filterValue = null,
        int? searchArea = null,
        int? roomType = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await GetRoomsResponseAsync(metadataTemplateId, metadataFilters, filterValue, searchArea, roomType, cancellationToken: cancellationToken);

        return await ReadAsync<RoomsContentResponse>(response, cancellationToken);
    }

    #endregion

    #region Folder listing

    /// <summary>
    /// Requests the folder content. <paramref name="withSubFolders"/> is left unset by default, which means the
    /// endpoint applies its own default of <c>true</c> — the search goes through the whole subtree.
    /// </summary>
    public async Task<FolderContentResponse> GetFolderContentAsync(
        int folderId,
        int? metadataTemplateId = null,
        IEnumerable<object>? metadataFilters = null,
        string? filterValue = null,
        bool? withSubFolders = null,
        CancellationToken cancellationToken = default,
        string? extension = null)
    {
        using var response = await GetFolderContentResponseAsync(folderId, metadataTemplateId, metadataFilters, filterValue, withSubFolders, extension, cancellationToken);

        return await ReadAsync<FolderContentResponse>(response, cancellationToken);
    }

    /// <summary>
    /// Requests the folder content and returns the raw response so the error cases can be asserted on the status code.
    /// </summary>
    public async Task<HttpResponseMessage> GetFolderContentResponseAsync(
        int folderId,
        int? metadataTemplateId = null,
        IEnumerable<object>? metadataFilters = null,
        string? filterValue = null,
        bool? withSubFolders = null,
        string? extension = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(extension))
        {
            query.Add($"extension={Uri.EscapeDataString(extension)}");
        }

        if (metadataTemplateId.HasValue)
        {
            query.Add($"metadataTemplateId={metadataTemplateId.Value}");
        }

        if (metadataFilters != null)
        {
            query.Add($"metadataFilters={Uri.EscapeDataString(JsonSerializer.Serialize(metadataFilters, _jsonOptions))}");
        }

        if (!string.IsNullOrEmpty(filterValue))
        {
            query.Add($"filterValue={Uri.EscapeDataString(filterValue)}");
        }

        if (withSubFolders.HasValue)
        {
            query.Add($"withSubFolders={(withSubFolders.Value ? "true" : "false")}");
        }

        var path = $"api/2.0/files/{folderId}" + (query.Count > 0 ? "?" + string.Join('&', query) : "");

        return await client.GetAsync(path, cancellationToken);
    }

    /// <summary>
    /// Requests a section by its alias (<c>@recent</c>, <c>@favorites</c>) with the metadata filter in the query.
    /// Raw HTTP on purpose: the generated SDK of the pinned version has no metadata parameters on the section endpoints.
    /// </summary>
    public async Task<HttpResponseMessage> GetSectionContentResponseAsync(
        string section,
        int? metadataTemplateId = null,
        IEnumerable<object>? metadataFilters = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();

        if (metadataTemplateId.HasValue)
        {
            query.Add($"metadataTemplateId={metadataTemplateId.Value}");
        }

        if (metadataFilters != null)
        {
            query.Add($"metadataFilters={Uri.EscapeDataString(JsonSerializer.Serialize(metadataFilters, _jsonOptions))}");
        }

        var path = $"api/2.0/files/{section}" + (query.Count > 0 ? "?" + string.Join('&', query) : "");

        return await client.GetAsync(path, cancellationToken);
    }

    public async Task<FolderContentResponse> GetSectionContentAsync(
        string section,
        int? metadataTemplateId = null,
        IEnumerable<object>? metadataFilters = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await GetSectionContentResponseAsync(section, metadataTemplateId, metadataFilters, cancellationToken);

        return await ReadAsync<FolderContentResponse>(response, cancellationToken);
    }

    #endregion

    #region Typed search

    /// <summary>
    /// The typed search of a folder. Returns the raw response so the error cases can be asserted on the status code.
    /// </summary>
    public Task<HttpResponseMessage> SearchFolderResponseAsync(int folderId, object body, CancellationToken cancellationToken)
    {
        return PostAsync($"api/2.0/files/{folderId}/search", body, cancellationToken);
    }

    public async Task<FolderContentResponse> SearchFolderAsync(int folderId, object body, CancellationToken cancellationToken)
    {
        using var response = await SearchFolderResponseAsync(folderId, body, cancellationToken);

        return await ReadAsync<FolderContentResponse>(response, cancellationToken);
    }

    public Task<HttpResponseMessage> SearchRoomsResponseAsync(object body, CancellationToken cancellationToken)
    {
        return PostAsync("api/2.0/files/rooms/search", body, cancellationToken);
    }

    public async Task<RoomsContentResponse> SearchRoomsAsync(object body, CancellationToken cancellationToken)
    {
        using var response = await SearchRoomsResponseAsync(body, cancellationToken);

        return await ReadAsync<RoomsContentResponse>(response, cancellationToken);
    }

    #endregion

    private async Task<HttpResponseMessage> PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(body, options: _jsonOptions);

        return await client.PostAsync(path, content, cancellationToken);
    }

    private async Task<HttpResponseMessage> PutAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(body, options: _jsonOptions);

        return await client.PutAsync(path, content, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<MetadataApiResponse<T>>(_jsonOptions, cancellationToken);

        if (wrapper is null || wrapper.Response is null)
        {
            throw new InvalidOperationException($"Empty response body for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}.");
        }

        return wrapper.Response;
    }
}

public class MetadataApiResponse<T>
{
    public T? Response { get; init; }
    public int Status { get; init; }
    public int StatusCode { get; init; }
}

public class MetadataFieldPayload
{
    public string Name { get; init; } = "";

    /// <summary>
    /// The <c>MetadataFieldType</c> value: String = 0, Date = 1, Number = 2, SingleChoice = 3, MultiChoice = 4.
    /// </summary>
    public int Type { get; init; }

    public List<MetadataFieldOptionPayload>? Options { get; init; }
}

public class MetadataFieldOptionPayload
{
    public string Value { get; init; } = "";
}

public class MetadataValuePayload
{
    public int FieldId { get; init; }
    public string? StringValue { get; init; }
    public long? NumberValue { get; init; }
    public DateTime? DateValue { get; init; }
    public List<Guid>? OptionIds { get; init; }
}

/// <summary>
/// A template assigned to an entry: every field of the template, each carrying its value on the entry (null when unset).
/// </summary>
public class EntryTemplateResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public bool Visible { get; init; }
    public List<EntryFieldResponse> Fields { get; init; } = [];

    public EntryFieldResponse Field(string name)
    {
        return Fields.Single(f => f.Name == name);
    }

    /// <summary>
    /// The fields holding a value on the entry.
    /// </summary>
    public IEnumerable<EntryFieldResponse> SetFields => Fields.Where(f => f.Value != null);
}

public class EntryFieldResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int Type { get; init; }
    public List<MetadataFieldOptionResponse> Options { get; init; } = [];
    public int Order { get; init; }
    public MetadataValueResponse? Value { get; init; }
}

/// <summary>
/// The whole metadata answer of an entry: the assigned templates and the custom fields holding a value.
/// </summary>
public class EntryMetadataSetResponse
{
    public List<EntryTemplateResponse> Templates { get; init; } = [];
    public List<CustomFieldValueResponse> CustomFields { get; init; } = [];
}

public record CustomFieldPayload(string Name, string? Value);

public class CustomFieldValueResponse
{
    public string Name { get; init; } = "";
    public string? Value { get; init; }
}

public class MetadataValueResponse
{
    public string? StringValue { get; init; }
    public long? NumberValue { get; init; }
    public DateTimeOffset? DateValue { get; init; }
    public List<Guid>? OptionIds { get; init; }
}

public class MetadataOperationResponse
{
    public string? Id { get; init; }
    public double Progress { get; init; }
    public bool IsCompleted { get; init; }
    public string? Error { get; init; }
}

public class MetadataTemplateResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public bool Visible { get; init; }
    public List<MetadataFieldResponse> Fields { get; init; } = [];

    public MetadataFieldResponse Field(string name)
    {
        return Fields.Single(f => f.Name == name);
    }
}

public class MetadataFieldResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int Type { get; init; }
    public List<MetadataFieldOptionResponse> Options { get; init; } = [];

    public Guid Option(string value)
    {
        return Options.Single(o => o.Value == value).Id;
    }
}

public class MetadataFieldOptionResponse
{
    public Guid Id { get; init; }
    public string Value { get; init; } = "";
}

public class RoomsContentResponse
{
    public List<RoomEntryResponse> Folders { get; init; } = [];
    public int Total { get; init; }
    public int Count { get; init; }

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

    public List<int> FolderIds()
    {
        return Folders.Select(f => f.Id).ToList();
    }

    public List<int> FileIds()
    {
        return Files.Select(f => f.Id).ToList();
    }
}

public class RoomEntryResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public List<int>? AssignedMetadataTemplates { get; init; }
}
