// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Integration.Database;

public partial class AiIntegrationContext
{
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<DbAssignment?> GetAssignmentAsync(int tenantId, string actionType)
    {
        return AssignmentQueriesContainer.GetAssignmentAsync(this, tenantId, actionType);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbAssignment> GetAllAssignmentsAsync(int tenantId)
    {
        return AssignmentQueriesContainer.GetAllAssignmentsAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbAssignment> GetAssignmentsByTypesAsync(int tenantId, IEnumerable<string> actionTypes)
    {
        return AssignmentQueriesContainer.GetAssignmentsByTypesAsync(this, tenantId, actionTypes);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultInt])]
    public Task<int> UpdateAssignmentProfileAsync(int tenantId, string actionType, int profileId)
    {
        return AssignmentQueriesContainer.UpdateAssignmentProfileAsync(this, tenantId, actionType, profileId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteAssignmentAsync(int tenantId, string actionType)
    {
        return AssignmentQueriesContainer.DeleteAssignmentAsync(this, tenantId, actionType);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteAssignmentsByTypesAsync(int tenantId, IEnumerable<string> actionTypes)
    {
        return AssignmentQueriesContainer.DeleteAssignmentsByTypesAsync(this, tenantId, actionTypes);
    }
}

static file class AssignmentQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, string, Task<DbAssignment?>> GetAssignmentAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string actionType) =>
                ctx.Assignments.FirstOrDefault(x => x.TenantId == tenantId && x.ActionType == actionType));

    public static readonly Func<AiIntegrationContext, int, IAsyncEnumerable<DbAssignment>> GetAllAssignmentsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId)
                    .OrderBy(x => x.ActionType)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, IEnumerable<string>, IAsyncEnumerable<DbAssignment>> GetAssignmentsByTypesAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<string> actionTypes) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && actionTypes.Contains(x.ActionType)));

    public static readonly Func<AiIntegrationContext, int, string, int, Task<int>> UpdateAssignmentProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string actionType, int profileId) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.ActionType == actionType)
                    .ExecuteUpdate(x => x.SetProperty(y => y.ProfileId, profileId)));

    public static readonly Func<AiIntegrationContext, int, string, Task<int>> DeleteAssignmentAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string actionType) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.ActionType == actionType)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, IEnumerable<string>, Task<int>> DeleteAssignmentsByTypesAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<string> actionTypes) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && actionTypes.Contains(x.ActionType))
                    .ExecuteDelete());
}
