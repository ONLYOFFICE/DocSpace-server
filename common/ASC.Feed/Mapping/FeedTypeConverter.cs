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

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ASC.Feed.Mapping;

[Scope]
public class FeedMappingAction(TenantUtil tenantUtil) : IMappingAction<FeedAggregate, FeedResultItem>
{
    public void Process(FeedAggregate source, FeedResultItem destination, ResolutionContext context)
    {
        var now = tenantUtil.DateTimeFromUtc(DateTime.UtcNow);

        destination.CreatedDate = tenantUtil.DateTimeFromUtc(source.CreatedDate);
        destination.ModifiedDate = tenantUtil.DateTimeFromUtc(source.ModifiedDate);
        destination.AggregatedDate = tenantUtil.DateTimeFromUtc(source.AggregateDate);

        var feed = JsonSerializer.Deserialize<Aggregator.Feed>(source.Json);

        destination.TargetId = feed.Target;

        var compareDate = feed.IsAllDayEvent
                ? tenantUtil.DateTimeToUtc(source.CreatedDate).Date
                : destination.CreatedDate.Date;

        if (now.Date == compareDate.AddDays(-1))
        {
            destination.IsTomorrow = true;
        }
        else if (now.Date == compareDate)
        {
            destination.IsToday = true;
        }
        else if (now.Date == compareDate.AddDays(1))
        {
            destination.IsYesterday = true;
        }
    }
}

[Scope]
public class FeedTypeConverter(UserManager userManager) : ITypeConverter<FeedResultItem, FeedMin>
{
    public FeedMin Convert(FeedResultItem source, FeedMin destination, ResolutionContext context)
    {
        var feedMin = JsonConvert.DeserializeObject<FeedMin>(source.Json);
        feedMin.Author = new FeedMinUser { UserInfo = userManager.GetUsers(feedMin.AuthorId) };
        feedMin.CreatedDate = source.CreatedDate;

        if (feedMin.Comments == null)
        {
            return feedMin;
        }

        foreach (var comment in feedMin.Comments)
        {
            comment.Author = new FeedMinUser
            {
                UserInfo = userManager.GetUsers(comment.AuthorId)
            };
        }

        return feedMin;
    }
}
