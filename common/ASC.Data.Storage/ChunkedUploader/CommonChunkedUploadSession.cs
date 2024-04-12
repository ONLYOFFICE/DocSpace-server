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

namespace ASC.Core.ChunkedUploader;

public class CommonChunkedUploadSession(long bytesTotal) : ICloneable
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Expired { get; set; }
    public string Location { get; set; }
    public long BytesTotal { get; set; } = bytesTotal;
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    public bool UseChunks { get; set; } = true;
    public string CultureName { get; set; }
    public Dictionary<string, object> Items { get; set; } = new();

    [JsonIgnore]
    public string TempPath
    {
        get => GetItemOrDefault<string>(TempPathKey);
        set => Items[TempPathKey] = value;
    }

    [JsonIgnore]
    public string UploadId
    {
        get => GetItemOrDefault<string>(UploadIdKey);
        set => Items[UploadIdKey] = value;
    }

    [JsonIgnore]
    public string ChunksBuffer
    {
        get => GetItemOrDefault<string>(ChunksBufferKey);
        set => Items[ChunksBufferKey] = value;
    }

    private const string TempPathKey = "TempPath";
    private const string UploadIdKey = "UploadId";
    private const string ChunksBufferKey = "ChunksBuffer";

    public T GetItemOrDefault<T>(string key)
    {
        if (!Items.TryGetValue(key, out var obj) || obj == null)
        {
            return default;
        }

        switch (obj)
        {
            case T value:
                return value;
            case JsonElement element:
                {
                    T item;

                    try
                    {
                        item = element.Deserialize<T>();
                    }
                    catch (Exception)
                    {
                        return default;
                    }
                
                    Items[key] = item;
                    return item;
                }
        }

        return default;
    }

    public void TransformItems()
    {
        var newItems = new Dictionary<string, object>();

        foreach (var item in Items.Where(item => item.Value != null))
        {
            if (item.Value is JsonElement value)
            {
                switch (value.ValueKind)
                {
                    case JsonValueKind.String:
                        newItems.Add(item.Key, value.ToString());
                        break;
                    case JsonValueKind.Number:
                        newItems.Add(item.Key, Int32.Parse(value.ToString()));
                        break;
                    case JsonValueKind.Array:
                        newItems.Add(item.Key, value.EnumerateArray().Select(o => o.ToString()).ToList());
                        break;
                    case JsonValueKind.Object:
                        try
                        {
                            newItems.Add(item.Key, JsonSerializer.Deserialize<Dictionary<int, string>>(value.ToString()));
                        }
                        catch
                        {
                            newItems.Add(item.Key, value);
                        }
                        break;
                    default:
                        newItems.Add(item.Key, value);
                        break;
                }
            }
            else
            {
                newItems.Add(item.Key, item.Value);
            }
        }
        
        Items = newItems;
    }

    public virtual object Clone()
    {
        return (CommonChunkedUploadSession)MemberwiseClone();
    }
}
