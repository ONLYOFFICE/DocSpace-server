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

namespace ASC.Common.Utils;

/// <summary>
/// Tells whether the process was started by the OpenAPI document generator
/// (<c>dotnet-getdocument</c> from Microsoft.Extensions.ApiDescription.Server) rather than as a real service.
/// </summary>
/// <remarks>
/// The generator loads the service assembly and runs its <c>Main</c> only to build the DI graph, then stops the host.
/// It kills the child process after a hard-coded 2 minutes, so startup code must not talk to external
/// infrastructure (Redis, RabbitMQ, the database) in this mode: on a machine without that infrastructure
/// every connection attempt eats into the deadline and the build fails with MSB3073 / TimeoutException.
/// </remarks>
public static class OpenApiDocumentGeneration
{
    private const string GeneratorAssemblyPrefix = "GetDocument";
    private const string OverrideVariable = "ASC_OPENAPI_DOCUMENT_GENERATION";

    /// <summary>
    /// True when the current process only builds the service graph to emit OpenAPI documents.
    /// </summary>
    /// <remarks>
    /// The entry assembly name belongs to a third-party tool and may change when the package is upgraded.
    /// Setting <c>ASC_OPENAPI_DOCUMENT_GENERATION=true</c> forces the same mode, which is the escape hatch when
    /// a future generator stops being recognised (the symptom is the build failing on the 2-minute timeout again).
    /// </remarks>
    public static bool IsRunning { get; } = Resolve();

    private static bool Resolve()
    {
        if (string.Equals(Environment.GetEnvironmentVariable(OverrideVariable), "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

        return entryAssemblyName != null && entryAssemblyName.StartsWith(GeneratorAssemblyPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
