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

    public async Task<MetadataTemplateResponse> CreateTemplateAsync(string name, IEnumerable<MetadataFieldPayload> fields, CancellationToken cancellationToken)
    {
        var body = new { name, visible = true, fields = fields.ToList() };

        using var response = await PostAsync("api/2.0/files/metadata/templates", body, cancellationToken);

        return await ReadAsync<MetadataTemplateResponse>(response, cancellationToken);
    }

    #endregion

    #region Assignment and values

    public async Task AssignFolderTemplatesAsync(int folderId, IEnumerable<int> templateIds, bool cascade, CancellationToken cancellationToken)
    {
        var body = new { templateIds = templateIds.ToList(), cascade };

        using var response = await PutAsync($"api/2.0/files/metadata/folder/{folderId}/templates", body, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task SetFolderValuesAsync(int folderId, IEnumerable<MetadataValuePayload> values, CancellationToken cancellationToken)
    {
        var body = new { values = values.ToList() };

        using var response = await PutAsync($"api/2.0/files/metadata/folder/{folderId}/values", body, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task AddFolderCustomFieldAsync(int folderId, string name, string value, CancellationToken cancellationToken)
    {
        var body = new { name, value };

        using var response = await PostAsync($"api/2.0/files/metadata/folder/{folderId}/customfield", body, cancellationToken);

        response.EnsureSuccessStatusCode();
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

public class MetadataTemplateResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
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

public class RoomEntryResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public List<int>? AssignedMetadataTemplates { get; init; }
}
