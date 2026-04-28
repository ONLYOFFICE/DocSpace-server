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
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbProfile?> GetProfileAsync(int tenantId, int id)
    {
        return Queries.GetProfileAsync(this, tenantId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbProfile> GetAllProfilesAsync(int tenantId)
    {
        return Queries.GetAllProfilesAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null, null, null, null, null, null, null])]
    public Task<int> UpdateProfileAsync(
        int tenantId,
        int id,
        string name,
        string providerType,
        string baseUrl,
        string? key,
        string modelId,
        bool? reasoning,
        Capabilities? capabilities)
    {
        return Queries.UpdateProfileAsync(this, tenantId, id, name, providerType, baseUrl, key, modelId, reasoning, capabilities);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> DeleteProfileAsync(int tenantId, int id)
    {
        return Queries.DeleteProfileAsync(this, tenantId, id);
    }
}

static file class Queries
{
    public static readonly Func<AiIntegrationContext, int, int, Task<DbProfile?>> GetProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int id) =>
                ctx.Profiles.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));

    public static readonly Func<AiIntegrationContext, int, IAsyncEnumerable<DbProfile>> GetAllProfilesAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId) =>
                ctx.Profiles
                    .Where(x => x.TenantId == tenantId)
                    .OrderBy(x => x.Id)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, int, string, string, string, string?, string, bool?, Capabilities?, Task<int>> UpdateProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int id, string name, string providerType, string baseUrl, string? key, string modelId, bool? reasoning, Capabilities? capabilities) =>
                ctx.Profiles
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteUpdate(x =>
                        x.SetProperty(y => y.Name, name)
                            .SetProperty(y => y.ProviderType, providerType)
                            .SetProperty(y => y.BaseUrl, baseUrl)
                            .SetProperty(y => y.Key, key)
                            .SetProperty(y => y.ModelId, modelId)
                            .SetProperty(y => y.Reasoning, reasoning)
                            .SetProperty(y => y.Capabilities, capabilities)));

    public static readonly Func<AiIntegrationContext, int, int, Task<int>> DeleteProfileAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int id) =>
                ctx.Profiles
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteDelete());
}
