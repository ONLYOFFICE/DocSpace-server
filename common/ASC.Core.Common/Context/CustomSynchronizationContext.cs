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

using Microsoft.AspNetCore.Builder;

namespace ASC.Core;

public class CustomSynchronizationContext
{
    public IPrincipal CurrentPrincipal { get; set; }

    private static readonly AsyncLocal<CustomSynchronizationContext> _context = new();
    public static CustomSynchronizationContext CurrentContext => _context.Value;

    /// <summary>
    /// Always starts a fresh context for the current logical flow.
    /// </summary>
    /// <remarks>
    /// The value is an <see cref="AsyncLocal{T}"/> reference, so it is inherited by every downstream flow.
    /// A context is created in the application root (see RunWithTasksAsync), which means hosted services,
    /// event bus consumers and queued jobs all inherit the very same instance. Reusing it would turn
    /// <see cref="CurrentPrincipal"/> into a process-wide mutable field shared by concurrent handlers,
    /// so each entry point must get its own instance instead.
    /// </remarks>
    public static void CreateContext()
    {
        _context.Value = new CustomSynchronizationContext { CurrentPrincipal = CurrentContext?.CurrentPrincipal };
    }
}


public class SynchronizationContextMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        CustomSynchronizationContext.CreateContext();

        await next.Invoke(context);
    }
}

public static class SynchronizationContextMiddlewareExtensions
{
    public static IApplicationBuilder UseSynchronizationContextMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SynchronizationContextMiddleware>();
    }
}