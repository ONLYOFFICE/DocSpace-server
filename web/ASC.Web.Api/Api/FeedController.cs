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

using Constants = ASC.Feed.Constants;

namespace ASC.Web.Api.Controllers;

/// <summary>
/// Feed API.
/// </summary>
/// <name>feed</name>
/// <visible>false</visible>
[Scope]
[DefaultRoute]
[ApiController]
public class FeedController(FeedReadedDataProvider feedReadDataProvider,
        ApiContext apiContext,
        ICache newFeedsCountCache,
        FeedAggregateDataProvider feedAggregateDataProvider,
        TenantUtil tenantUtil,
        SecurityContext securityContext,
        IMapper mapper,
        IDaoFactory daoFactory,
        FileSecurity fileSecurity)
    : ControllerBase
{
    private string Key => $"newfeedscount/{securityContext.CurrentAccount.ID}";

    /// <summary>
    /// Opens feeds for reading.
    /// </summary>
    /// <short>
    /// Read feeds
    /// </short>
    /// <path>api/2.0/feed/read</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns></returns>
    [HttpPut("read")]
    public async Task Read()
    {
        await feedReadDataProvider.SetTimeReadedAsync();
    }

    /// <summary>
    /// Returns a list of feeds that are filtered by the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Get feeds
    /// </short>
    /// <param type="System.String, System" name="id">Entity ID</param>
    /// <param type="System.String, System" name="product">Product which feeds you want to read</param>
    /// <param type="System.String, System" name="module">Feeds of the module that will be searched for by entity ID</param>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="from">Time from which the feeds should be displayed</param>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="to">Time until which the feeds should be displayed</param>
    /// <param type="System.Nullable{System.Guid}, System" name="author">Author whose feeds you want to read</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="onlyNew">Displays only fresh feeds</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withRelated">Includes the associated feeds related to the entity with the specified ID</param>
    /// <param type="ASC.Api.Core.ApiDateTime, ASC.Api.Core" name="timeReaded">Time when the feeds were read</param>
    /// <returns type="System.Object, System">List of filtered feeds with the dates when they were read</returns>
    /// <path>api/2.0/feed/filter</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("filter")]
    public async Task<object> GetFeedAsync(
        string id,
        string product,
        string module,
        ApiDateTime from,
        ApiDateTime to,
        Guid? author,
        bool? onlyNew,
        bool? withRelated,
        ApiDateTime timeReaded)
    {
        FileEntry entry = null;
        
        if (!string.IsNullOrEmpty(id))
        {
            if (int.TryParse(id, out var intId))
            {
                entry = await CheckAccessAsync(intId, module);
            }
            else
            {
                entry = await CheckAccessAsync(id, module);
            }
        }
        
        var filter = new FeedApiFilter
        {
            Id = id,
            Product = product,
            Module = module,
            Offset = Convert.ToInt32(apiContext.StartIndex),
            Max = Convert.ToInt32(apiContext.Count) - 1,
            Author = author ?? Guid.Empty,
            SearchKeys = apiContext.FilterValues,
            OnlyNew = onlyNew.HasValue && onlyNew.Value,
            History = withRelated.HasValue && withRelated.Value
        };

        if (from != null && to != null)
        {
            var f = tenantUtil.DateTimeFromUtc(from.UtcTime);
            filter.From = new DateTime(f.Year, f.Month, f.Day, 0, 0, 0);

            var t = tenantUtil.DateTimeFromUtc(to.UtcTime);
            filter.To = new DateTime(t.Year, t.Month, t.Day, 23, 59, 59);
        }
        else
        {
            filter.From = from != null ? from.UtcTime : DateTime.MinValue;
            filter.To = to != null ? to.UtcTime : DateTime.MaxValue;
        }

        var lastTimeRead = DateTime.UtcNow;
        var readDate = lastTimeRead;

        if (string.IsNullOrEmpty(id))
        {
            lastTimeRead = await feedReadDataProvider.GetTimeReadedAsync();
            readDate = timeReaded != null ? timeReaded.UtcTime : lastTimeRead;
        }

        if (filter.OnlyNew)
        {
            filter.From = lastTimeRead;
            filter.Max = 100;
        }
        else if (timeReaded == null)
        {
            await feedReadDataProvider.SetTimeReadedAsync();
            newFeedsCountCache.Remove(Key);
        }

        if (entry is { FileEntryType: FileEntryType.File })
        {
            filter.From = tenantUtil.DateTimeToUtc(entry.CreateOn);
            filter.To = tenantUtil.DateTimeToUtc(entry.ModifiedOn);
        }

        var feeds = (await feedAggregateDataProvider
            .GetFeedsAsync(filter))
            .GroupBy(n => n.GroupId, mapper.Map<FeedResultItem, FeedDto>, (_, group) => 
            { 
                var firstFeed = group.First(); 
                firstFeed.GroupedFeeds = group.Skip(1); 
                return firstFeed; 
            })
            .OrderByDescending(f => f.ModifiedDate)
            .ToList();

        return new { feeds, readedDate = readDate };

        async Task<FileEntry> CheckAccessAsync<T>(T entryId, string fModule)
        {
            FileEntry<T> fileEntry = null;

            switch (fModule)
            {
                case Constants.RoomsModule:
                case Constants.FoldersModule:
                    {
                        fileEntry = await daoFactory.GetFolderDao<T>().GetFolderAsync(entryId);
                        break;
                    }
                case Constants.FilesModule:
                    {
                        fileEntry = await daoFactory.GetFileDao<T>().GetFileAsync(entryId);
                        break;
                    }
            }

            if (fileEntry == null)
            {
                throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
            }

            if (!await fileSecurity.CanReadAsync(fileEntry))
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
            }

            return fileEntry;
        }
    }

    /// <summary>
    /// Returns an integer representing the number of fresh feeds.
    /// </summary>
    /// <short>
    /// Count fresh feeds
    /// </short>
    /// <returns type="System.Object, System">Number of fresh feeds</returns>
    /// <path>api/2.0/feed/newfeedscount</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("newfeedscount")]
    public async Task<object> GetFreshNewsCountAsync()
    {
        var cacheKey = Key;
        var resultfromCache = newFeedsCountCache.Get<string>(cacheKey);

        if (!int.TryParse(resultfromCache, out var result))
        {
            var lastTimeReaded = await feedReadDataProvider.GetTimeReadedAsync();
            result = await feedAggregateDataProvider.GetNewFeedsCountAsync(lastTimeReaded);
            newFeedsCountCache.Insert(cacheKey, result.ToString(), DateTime.UtcNow.AddMinutes(3));
        }

        return result;
    }
}