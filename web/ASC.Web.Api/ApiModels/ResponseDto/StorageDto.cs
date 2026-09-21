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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// One third-party storage provider the portal data can be kept in, with the keys it expects.
/// </summary>
/// <example>
/// {
///   "id": "s3",
///   "title": "Amazon AWS S3",
///   "properties": [ { "name": "acesskey", "value": "AKIAIOSFODNN7EXAMPLE", "title": "Access key" } ],
///   "current": true,
///   "isSet": true
/// }
/// </example>
public class StorageDto
{
    /// <summary>
    /// The provider's key, which is what `PUT api/2.0/settings/storage` and its CDN and backup counterparts take
    /// as the storage to switch to. The built-in local storage has no entry of its own: a listing in which
    /// nothing is `current` means the data sits locally.
    /// </summary>
    /// <example>s3</example>
    public required string Id { get; set; }

    /// <summary>
    /// The provider name in the portal language, falling back to `id` when this build ships no wording for it.
    /// </summary>
    /// <example>Amazon AWS S3</example>
    public required string Title { get; set; }

    /// <summary>
    /// The settings the provider expects, each with its key, its localised label and the value the server
    /// currently holds. For the entry marked `current` the values come from the portal's saved storage settings
    /// and for the others from the installation configuration, so a setting nobody has configured comes back with
    /// an empty value rather than being left out.
    /// </summary>
    /// <example>[{"name": "acesskey", "value": "AKIAIOSFODNN7EXAMPLE", "title": "Access key"}]</example>
    public List<AuthKey> Properties { get; set; }

    /// <summary>
    /// Whether the portal is using this provider right now. At most one entry of a listing has it set.
    /// </summary>
    /// <example>true</example>
    public required bool Current { get; set; }

    /// <summary>
    /// Whether the provider's keys are already filled in on the server, so it could be switched to without
    /// sending credentials. It says nothing about whether the credentials still work.
    /// </summary>
    /// <example>true</example>
    public required bool IsSet { get; set; }

    public static async Task<StorageDto> StorageWrapperInit<T>(DataStoreConsumer consumer, BaseStorageSettings<T> current) where T : class, ISettings<T>, new()
    {
        var result = new StorageDto
        {
            Id = consumer.Name,
            Title = ConsumerExtension.GetResourceString(consumer.Name) ?? consumer.Name,
            Current = consumer.Name == current.Module,
            IsSet = await consumer.GetIsSetAsync()
        };

        var props = result.Current
            ? current.Props
            : await current.Switch(consumer).AdditionalKeys
                .ToAsyncEnumerable()
                .ToDictionaryAsync((s, _) => ValueTask.FromResult(s), async (a, _) => await consumer.GetAsync(a));

        result.Properties = props.Select(
            r => new AuthKey
            {
                Name = r.Key,
                Value = r.Value,
                Title = ConsumerExtension.GetResourceString(consumer.Name + r.Key) ?? r.Key
            }).ToList();

        return result;
    }
}