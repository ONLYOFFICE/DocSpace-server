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

#nullable enable
namespace ASC.Files.Core.ExternalDatabase;

public interface IFormsDatabaseClient
{
    bool IsEnabled();

    Task CreateTableAndUpsertAsync(string tableName, IEnumerable<DbColumnDefinition> columns,
        Dictionary<string, object> data, string keyColumn);

    Task<long> GetTableCountAsync(string tableName);

    Task<IReadOnlySet<int>> GetExistingFormIdsAsync(string tableName);

    Task<bool> TableExistsAsync(string tableName);

    Task<long> CountAsync(string tableName);

    Task<string> AggregateAsync(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        string aggregateFunction,
        string? valueColumn,
        string? groupByColumn,
        IEnumerable<QueryFilter>? filters = null,
        string? groupByDatePart = null,
        string? secondGroupByColumn = null,
        string? secondGroupByDatePart = null,
        IEnumerable<DatePartFilter>? datePartFilters = null,
        DateDiffFilter? dateDiffFilter = null,
        DateDiffAggregate? dateDiffAggregate = null,
        string? havingFilter = null,
        string? thirdGroupByColumn = null,
        string? thirdGroupByDatePart = null,
        IEnumerable<QueryFilter>? excludeFilters = null,
        IEnumerable<DatePartFilter>? excludeDatePartFilters = null,
        bool countGroupsOnly = false);

    Task<string> QueryAsync(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        IEnumerable<string>? selectColumns = null,
        IEnumerable<QueryFilter>? filters = null,
        string? orderByColumn = null,
        bool orderByDescending = false,
        string? thenByColumn = null,
        bool thenByDescending = false,
        int maxRows = 50,
        int offset = 0,
        IEnumerable<DatePartFilter>? datePartFilters = null,
        DateDiffFilter? dateDiffFilter = null);

    Task<string> SelfJoinAsync(
        string tableName,
        IReadOnlyCollection<string> allowedColumns,
        string pkColumn,
        IEnumerable<SelfJoinCondition> joinConditions,
        IEnumerable<string>? displayColumns = null,
        int limit = 100,
        IEnumerable<QueryFilter>? filters = null,
        IEnumerable<DatePartFilter>? datePartFilters = null,
        string? countDistinctColumn = null);
}
