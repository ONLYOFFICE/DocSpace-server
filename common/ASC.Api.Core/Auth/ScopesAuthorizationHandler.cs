// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.Api.Core.Auth;

public class ScopesAuthorizationHandler : AuthorizationHandler<ScopesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopesRequirement requirement)
    {
        if (!context.User.Identity.IsAuthenticated)
        {
            return Task.CompletedTask;
        }

        if (context.HasSucceeded)
        {
            return Task.CompletedTask;
        }

        if (context.User == null || requirement == null || string.IsNullOrWhiteSpace(requirement.Scopes))
        {
            return Task.CompletedTask;
        }

        var requirementScopes = requirement.Scopes.Split(",", StringSplitOptions.RemoveEmptyEntries);

        if (requirementScopes.Length == 0)
        {
            return Task.CompletedTask;
        }

        var expectedRequirements = requirementScopes.ToList();

        if (expectedRequirements.Count == 0)
        {
            return Task.CompletedTask;
        }

        var userScopeClaims = context.User.Claims.Where(c => string.Equals(c.Type, "scope", StringComparison.OrdinalIgnoreCase));

        foreach (var claim in userScopeClaims)
        {
            var match = expectedRequirements
                .Where(r => string.Equals(r, claim.Value, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(AuthConstants.Claim_ScopeRootWrite.Value, claim.Value, StringComparison.OrdinalIgnoreCase));

            if (match.Any())
            {
                context.Succeed(requirement);

                break;
            }
        }

        return Task.CompletedTask;
    }
}

