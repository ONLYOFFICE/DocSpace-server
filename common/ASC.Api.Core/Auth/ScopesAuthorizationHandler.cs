// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Api.Core.Auth;

public class ScopesAuthorizationHandler : AuthorizationHandler<ScopesRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopesRequirement requirement)
    {
        if (context.User.Identity is { IsAuthenticated: false })
        {
            return Task.CompletedTask;
        }

        if (context.HasSucceeded)
        {
            return Task.CompletedTask;
        }

        if (requirement == null || string.IsNullOrWhiteSpace(requirement.Scopes))
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

        var userScopes = context.User.Claims.Where(c => string.Equals(c.Type, "scope", StringComparison.OrdinalIgnoreCase))
                                                             .Select(c => c.Value)
                                                             .ToList();

        if (expectedRequirements.Any(x => userScopes.Contains(x)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}