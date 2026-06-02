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
    [PreCompileQuery]
    public Task<DbAssignment?> GetAssignmentAsync(int tenantId, ActionType actionType)
    {
        return AssignmentQueriesContainer.GetAssignmentAsync(this, tenantId, actionType);
    }

    [PreCompileQuery]
    public Task<DbAssignment?> GetAssignmentByEntryAsync(int tenantId, ActionType actionType, int entryId)
    {
        return AssignmentQueriesContainer.GetAssignmentByEntryAsync(this, tenantId, actionType, entryId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAssignment> GetAllAssignmentsAsync(int tenantId)
    {
        return AssignmentQueriesContainer.GetAllAssignmentsAsync(this, tenantId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAssignment> GetAllAssignmentsByEntryAsync(int tenantId, int entryId)
    {
        return AssignmentQueriesContainer.GetAllAssignmentsByEntryAsync(this, tenantId, entryId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAssignment> GetAssignmentsByTypesAsync(int tenantId, IEnumerable<ActionType> actionTypes)
    {
        return AssignmentQueriesContainer.GetAssignmentsByTypesAsync(this, tenantId, actionTypes);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAssignment> GetAssignmentsByTypesAndEntryAsync(int tenantId, int entryId, IEnumerable<ActionType> actionTypes)
    {
        return AssignmentQueriesContainer.GetAssignmentsByTypesAndEntryAsync(this, tenantId, entryId, actionTypes);
    }

    [PreCompileQuery]
    public Task<int> UpdateAssignmentProfileAsync(int tenantId, ActionType actionType, Guid profileId)
    {
        return AssignmentQueriesContainer.UpdateAssignmentProfileAsync(this, tenantId, actionType, profileId);
    }

    [PreCompileQuery]
    public Task<int> UpdateAssignmentProfileByEntryAsync(int tenantId, ActionType actionType, int entryId, Guid profileId)
    {
        return AssignmentQueriesContainer.UpdateAssignmentProfileByEntryAsync(this, tenantId, actionType, entryId, profileId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAssignmentAsync(int tenantId, ActionType actionType)
    {
        return AssignmentQueriesContainer.DeleteAssignmentAsync(this, tenantId, actionType);
    }

    [PreCompileQuery]
    public Task<int> DeleteAssignmentByEntryAsync(int tenantId, ActionType actionType, int entryId)
    {
        return AssignmentQueriesContainer.DeleteAssignmentByEntryAsync(this, tenantId, actionType, entryId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAssignmentsByTypesAsync(int tenantId, IEnumerable<ActionType> actionTypes)
    {
        return AssignmentQueriesContainer.DeleteAssignmentsByTypesAsync(this, tenantId, actionTypes);
    }

    [PreCompileQuery]
    public Task<int> DeleteAssignmentsByTypesAndEntryAsync(int tenantId, int entryId, IEnumerable<ActionType> actionTypes)
    {
        return AssignmentQueriesContainer.DeleteAssignmentsByTypesAndEntryAsync(this, tenantId, entryId, actionTypes);
    }
}

static file class AssignmentQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, ActionType, Task<DbAssignment?>> GetAssignmentAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, ActionType actionType) =>
                ctx.Assignments.FirstOrDefault(x => x.TenantId == tenantId && x.ActionType == actionType && x.EntryId == null));

    public static readonly Func<AiIntegrationContext, int, ActionType, int, Task<DbAssignment?>> GetAssignmentByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, ActionType actionType, int entryId) =>
                ctx.Assignments.FirstOrDefault(x => x.TenantId == tenantId && x.ActionType == actionType && x.EntryId == entryId));

    // Returns every assignment in the scope, including the global Default slot
    // (ActionType.Default == 0). Default is a valid persisted value, so it is included by design.
    public static readonly Func<AiIntegrationContext, int, IAsyncEnumerable<DbAssignment>> GetAllAssignmentsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.EntryId == null)
                    .OrderBy(x => x.ActionType)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, int, IAsyncEnumerable<DbAssignment>> GetAllAssignmentsByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int entryId) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.EntryId == entryId)
                    .OrderBy(x => x.ActionType)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, IEnumerable<ActionType>, IAsyncEnumerable<DbAssignment>> GetAssignmentsByTypesAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<ActionType> actionTypes) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.EntryId == null && actionTypes.Contains(x.ActionType)));

    public static readonly Func<AiIntegrationContext, int, int, IEnumerable<ActionType>, IAsyncEnumerable<DbAssignment>> GetAssignmentsByTypesAndEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int entryId, IEnumerable<ActionType> actionTypes) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.EntryId == entryId && actionTypes.Contains(x.ActionType)));

    public static readonly Func<AiIntegrationContext, int, ActionType, Guid, Task<int>> UpdateAssignmentProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, ActionType actionType, Guid profileId) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.ActionType == actionType && x.EntryId == null)
                    .ExecuteUpdate(x => x.SetProperty(y => y.ProfileId, profileId)));

    public static readonly Func<AiIntegrationContext, int, ActionType, int, Guid, Task<int>> UpdateAssignmentProfileByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, ActionType actionType, int entryId, Guid profileId) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.ActionType == actionType && x.EntryId == entryId)
                    .ExecuteUpdate(x => x.SetProperty(y => y.ProfileId, profileId)));

    public static readonly Func<AiIntegrationContext, int, ActionType, Task<int>> DeleteAssignmentAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, ActionType actionType) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.ActionType == actionType && x.EntryId == null)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, ActionType, int, Task<int>> DeleteAssignmentByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, ActionType actionType, int entryId) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.ActionType == actionType && x.EntryId == entryId)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, IEnumerable<ActionType>, Task<int>> DeleteAssignmentsByTypesAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<ActionType> actionTypes) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.EntryId == null && actionTypes.Contains(x.ActionType))
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, int, IEnumerable<ActionType>, Task<int>> DeleteAssignmentsByTypesAndEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int entryId, IEnumerable<ActionType> actionTypes) =>
                ctx.Assignments
                    .Where(x => x.TenantId == tenantId && x.EntryId == entryId && actionTypes.Contains(x.ActionType))
                    .ExecuteDelete());
}
