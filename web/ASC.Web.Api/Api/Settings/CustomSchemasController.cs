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

using static Dropbox.Api.Team.MobileClientPlatform;

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("customschemas")]
public class CustomSchemasController(MessageService messageService,
        ApiContext apiContext,
        TenantManager tenantManager,
        PermissionContext permissionContext,
        WebItemManager webItemManager,
        CustomNamingPeople customNamingPeople,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns all portal team templates that allow users to name their organization (or group), add members, and define their activities within the portal.
    /// </summary>
    /// <short>Get team templates</short>
    /// <path>api/2.0/settings/customschemas</path>
    /// <collection>list</collection>
    [Tags("Settings / Team templates")]
    [SwaggerResponse(200, "List of team templates with the following parameters", typeof(SchemaRequestsDto))]
    [HttpGet("")]
    public async Task<List<SchemaRequestsDto>> PeopleSchemasAsync()
    {
        return await customNamingPeople
                .GetSchemas().ToAsyncEnumerable()
                .SelectAwait(async r =>
                {
                    var names = await customNamingPeople.GetPeopleNamesAsync(r.Key);

                    return new SchemaRequestsDto
                    {
                        Id = names.Id,
                        Name = names.SchemaName,
                        UserCaption = names.UserCaption,
                        UsersCaption = names.UsersCaption,
                        GroupCaption = names.GroupCaption,
                        GroupsCaption = names.GroupsCaption,
                        UserPostCaption = names.UserPostCaption,
                        RegDateCaption = names.RegDateCaption,
                        GroupHeadCaption = names.GroupHeadCaption,
                        GuestCaption = names.GuestCaption,
                        GuestsCaption = names.GuestsCaption
                    };
                })
                .ToListAsync();
    }

    /// <summary>
    /// Saves the names from the team template with the ID specified in the request.
    /// </summary>
    /// <short>Save the naming settings</short>
    /// <path>api/2.0/settings/customschemas</path>
    [Tags("Settings / Team templates")]
    [SwaggerResponse(200, "Team template with the following parameters", typeof(SchemaRequestsDto))]
    [HttpPost("")]
    public async Task<SchemaRequestsDto> SaveNamingSettingsAsync(SchemaBaseRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await customNamingPeople.SetPeopleNamesAsync(inDto.Id);

        await tenantManager.SaveTenantAsync(await tenantManager.GetCurrentTenantAsync());

        await messageService.SendAsync(MessageAction.TeamTemplateChanged);

        var people = new IdRequestDto<string> { Id = inDto.Id };

        return await PeopleSchemaAsync(people);
    }

    /// <summary>
    /// Creates a custom team template with the parameters specified in the request.
    /// </summary>
    /// <short>Create a custom team template</short>
    /// <path>api/2.0/settings/customschemas</path>
    [Tags("Settings / Team templates")]
    [SwaggerResponse(200, "Custom team template with the following parameters", typeof(SchemaRequestsDto))]
    [SwaggerResponse(400, "Please fill in all fields")]
    [HttpPut("")]
    public async Task<SchemaRequestsDto> SaveCustomNamingSettingsAsync(SchemaRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var usrCaption = (inDto.UserCaption ?? "").Trim();
        var usrsCaption = (inDto.UsersCaption ?? "").Trim();
        var grpCaption = (inDto.GroupCaption ?? "").Trim();
        var grpsCaption = (inDto.GroupsCaption ?? "").Trim();
        var usrStatusCaption = (inDto.UserPostCaption ?? "").Trim();
        var regDateCaption = (inDto.RegDateCaption ?? "").Trim();
        var grpHeadCaption = (inDto.GroupHeadCaption ?? "").Trim();
        var guestCaption = (inDto.GuestCaption ?? "").Trim();
        var guestsCaption = (inDto.GuestsCaption ?? "").Trim();

        if (string.IsNullOrEmpty(usrCaption)
            || string.IsNullOrEmpty(usrsCaption)
            || string.IsNullOrEmpty(grpCaption)
            || string.IsNullOrEmpty(grpsCaption)
            || string.IsNullOrEmpty(usrStatusCaption)
            || string.IsNullOrEmpty(regDateCaption)
            || string.IsNullOrEmpty(grpHeadCaption)
            || string.IsNullOrEmpty(guestCaption)
            || string.IsNullOrEmpty(guestsCaption))
        {
            throw new Exception(Resource.ErrorEmptyFields);
        }

        var names = new PeopleNamesItem
        {
            Id = PeopleNamesItem.CustomID,
            UserCaption = usrCaption[..Math.Min(30, usrCaption.Length)],
            UsersCaption = usrsCaption[..Math.Min(30, usrsCaption.Length)],
            GroupCaption = grpCaption[..Math.Min(30, grpCaption.Length)],
            GroupsCaption = grpsCaption[..Math.Min(30, grpsCaption.Length)],
            UserPostCaption = usrStatusCaption[..Math.Min(30, usrStatusCaption.Length)],
            RegDateCaption = regDateCaption[..Math.Min(30, regDateCaption.Length)],
            GroupHeadCaption = grpHeadCaption[..Math.Min(30, grpHeadCaption.Length)],
            GuestCaption = guestCaption[..Math.Min(30, guestCaption.Length)],
            GuestsCaption = guestsCaption[..Math.Min(30, guestsCaption.Length)]
        };

        await customNamingPeople.SetPeopleNamesAsync(names);

        await tenantManager.SaveTenantAsync(await tenantManager.GetCurrentTenantAsync());

        await messageService.SendAsync(MessageAction.TeamTemplateChanged);

        var people = new IdRequestDto<string> { Id = PeopleNamesItem.CustomID };
        return await PeopleSchemaAsync(people);
    }

    /// <summary>
    /// Returns a team template by the ID specified in the request.
    /// </summary>
    /// <short>Get a team template by ID</short>
    /// <path>api/2.0/settings/customschemas/{id}</path>
    [Tags("Settings / Team templates")]
    [SwaggerResponse(200, "Team template with the following parameters", typeof(SchemaRequestsDto))]
    [HttpGet("{id}")]
    public async Task<SchemaRequestsDto> PeopleSchemaAsync(IdRequestDto<string> inDto)
    {
        var names = await customNamingPeople.GetPeopleNamesAsync(inDto.Id);
        var schemaItem = new SchemaRequestsDto
        {
            Id = names.Id,
            Name = names.SchemaName,
            UserCaption = names.UserCaption,
            UsersCaption = names.UsersCaption,
            GroupCaption = names.GroupCaption,
            GroupsCaption = names.GroupsCaption,
            UserPostCaption = names.UserPostCaption,
            RegDateCaption = names.RegDateCaption,
            GroupHeadCaption = names.GroupHeadCaption,
            GuestCaption = names.GuestCaption,
            GuestsCaption = names.GuestsCaption
        };
        return schemaItem;
    }
}