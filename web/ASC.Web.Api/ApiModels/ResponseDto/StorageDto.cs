// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Api.ApiModel.ResponseDto;

public class StorageDto
{
    [SwaggerSchemaCustomString("ID")]
    public string Id { get; set; }

    [SwaggerSchemaCustomString("Title")]
    public string Title { get; set; }

    [SwaggerSchemaCustom<List<AuthKey>>("List of authentication keys")]
    public List<AuthKey> Properties { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if this is the current storage or not")]
    public bool Current { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if this storage can be set or not")]
    public bool IsSet { get; set; }

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
            : await current.Switch(consumer).AdditionalKeys.ToAsyncEnumerable().ToDictionaryAwaitAsync(ValueTask.FromResult, async a => await consumer.GetAsync(a));

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