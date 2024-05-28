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

namespace ASC.Files.Core.Core.History.Interpreters;

public class RoomTagsInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        return ValueTask.FromResult<HistoryData>(new TagData(description[1].Split(',')));
    }
    
    private record TagData(string[] Tags) : HistoryData;
}

public class RoomCreateInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        return ValueTask.FromResult<HistoryData>(new EntryData(int.Parse(target), description[0]));
    }
}

public class RoomRenamedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        return ValueTask.FromResult<HistoryData>(new RenameEntryData(int.Parse(target), description[1], description[0]));
    }
}

public class RoomLogoChangedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        return ValueTask.FromResult<HistoryData>(null);
    }
}

public abstract class RoomUserAccessBaseInterpreter: ActionInterpreter
{
    protected override async ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var additionalDescription = GetAdditionalDescription(description);
        
        var userId = additionalDescription.UserIds.FirstOrDefault();
        var userManager = serviceProvider.GetRequiredService<UserManager>();

        var user = await userManager.GetUsersAsync(userId);

        if (user == null || user.Id == Constants.LostUser.Id || user.Id == ASC.Core.Configuration.Constants.Guest.ID)
        {
            return new UserHistoryData
            {
                User = new EmployeeDto { DisplayName = description[0] },
                Access = GetAccess(description)
            };
        }

        var employeeDtoHelper = serviceProvider.GetRequiredService<EmployeeDtoHelper>();
        
        return new UserHistoryData
        {
            User = await employeeDtoHelper.GetAsync(user),
            Access = GetAccess(description)
        };
    }

    protected abstract string GetAccess(List<string> description);
}

public class RoomUserAccessInterpreter : RoomUserAccessBaseInterpreter
{
    protected override string GetAccess(List<string> description) => description[1];
}

public class RoomUserRemovedInterpreter : RoomUserAccessBaseInterpreter
{
    protected override string GetAccess(List<string> description) => null;
}

public class RoomGroupAccessInterpreter: ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var groupId = Guid.Parse(description[2]);

        return ValueTask.FromResult<HistoryData>(
            new GroupHistoryData
            {
                Group = new GroupSummaryDto { Id = groupId, Name = description[0] }, 
                Access = description[1]
            });
    }
}

public class RoomRemovedGroupInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        var groupId = Guid.Parse(description[1]);

        return ValueTask.FromResult<HistoryData>(
            new GroupHistoryData
            {
                Group = new GroupSummaryDto { Id = groupId, Name = description[1] }
            });
    }
}

public class RoomExternalLinkCreatedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        return ValueTask.FromResult<HistoryData>(new LinkData(description[0], description[1]));
    }
}

public class RoomExternalLinkDeletedInterpreter : ActionInterpreter
{
    protected override ValueTask<HistoryData> GetDataAsync(IServiceProvider serviceProvider, string target, List<string> description)
    {
        return ValueTask.FromResult<HistoryData>(new LinkData(description[0], null));
    }
}

public record UserHistoryData : HistoryData
{
    public EmployeeDto User { get; set; }
    public string Access { get; set; }
}

public record GroupHistoryData : HistoryData
{
    public GroupSummaryDto Group { get; set; }
    public string Access { get; set; }
}