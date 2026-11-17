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

namespace ASC.Files.Core.ExternalDatabase;

/// <summary>
/// The connections the built-in forms database takes on its server. Every portal has its own pools there, so on a server
/// shared by many portals the size of a pool decides how many connections the active portals hold together, and the
/// limit of a portal's account keeps one portal from taking them all. Read once, when the service starts.
/// </summary>
[Singleton]
public class BuiltinFormsDbOptions
{
    private const int DefaultMaxPoolSize = 5;
    private const int MinDefaultRwConnectionLimit = 50;

    // Files, Files.Worker and AI each hold two pools of a portal: one for the submissions, one for the analysis.
    private const int PoolsOfOneReplicaSet = 6;

    public BuiltinFormsDbOptions(IConfiguration configuration, ILogger<BuiltinFormsDbOptions> logger)
    {
        // Connections of one pool of a portal in one process: 1 to 20, e.g. 3.
        MaxPoolSize = configuration.ReadInt("files:builtinFormsDb:maxPoolSize", DefaultMaxPoolSize, 1, 20, logger);

        // Connections the account of one portal may hold on the server, over all processes and replicas: from what one
        // process's two pools need (a lower limit would refuse the portal's own pools) to 1000, e.g. 60 for two replicas
        // of Files, Files.Worker and AI with pools of 5. By default one replica of each, and at least 50.
        var defaultLimit = Math.Max(MinDefaultRwConnectionLimit, PoolsOfOneReplicaSet * MaxPoolSize);
        RwConnectionLimit = configuration.ReadInt("files:builtinFormsDb:rwConnectionLimit", defaultLimit, 2 * MaxPoolSize, 1000, logger);
    }

    public int MaxPoolSize { get; }

    public int RwConnectionLimit { get; }
}
