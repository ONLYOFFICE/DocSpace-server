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

namespace ASC.Webhooks.Contract;

/// <summary>
/// One REST API operation that raises a webhook, as the generated API documents describe it.
/// </summary>
/// <param name="Method">The HTTP method, upper case.</param>
/// <param name="Path">The route a caller sends the request to, without the leading slash, as the API descriptions name
/// operations: <c>api/2.0/...</c> for the portal services, <c>apisystem/...</c> for the ApiSystem service.</param>
/// <param name="Summary">The operation's title.</param>
/// <param name="When">The condition under which it raises the trigger, or null when it always does.</param>
public sealed record RaisingOperation(string Method, string Path, string Summary, string When);

/// <summary>
/// Reads, from the generated OpenAPI documents of the REST API, which operations raise each webhook.
/// </summary>
/// <remarks>
/// Every API action that raises a webhook carries <c>[RaisesWebhook]</c>, and the OpenAPI filter of ASC.Api.Core
/// writes it into the operation as <c>x-docspace-webhooks</c>: a list of <c>{ trigger, when }</c>. The documents are
/// the ones <c>generate-sdk</c> emits into ASC.Api.Documentation; regenerate them before the contract when an
/// attribute changed, or the contract lists the operations as they were.
/// </remarks>
public static class RaisingOperations
{
    private const string ExtensionName = "x-docspace-webhooks";

    private static readonly string[] _methods = ["get", "post", "put", "patch", "delete"];

    /// <summary>
    /// The route prefix of a document whose paths leave it out. The ApiSystem service publishes its paths relative to
    /// the `apisystem/` mount point the router puts it under, while the documents of the portal services carry the
    /// whole route (`/api/2.0/...`); an operation is listed under the route a caller sends it to.
    /// </summary>
    private static readonly Dictionary<string, string> _routePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apisystem_common.json"] = "apisystem/"
    };

    /// <summary>
    /// The directory of the generated API documents, below the repository root.
    /// </summary>
    public static string GetDocumentsDirectory(string repositoryRoot)
    {
        return Path.Combine(repositoryRoot, "common", "Tools", "ASC.Api.Documentation", "ASC.Api.Documentation", "json");
    }

    /// <summary>
    /// The operations of every document in <paramref name="documentsDirectory"/>, by trigger name, each list ordered
    /// by path and method so that the contract does not change with the order of the files.
    /// </summary>
    public static Dictionary<string, List<RaisingOperation>> Load(string documentsDirectory)
    {
        var result = new Dictionary<string, List<RaisingOperation>>();

        if (!Directory.Exists(documentsDirectory))
        {
            return result;
        }

        foreach (var file in Directory.EnumerateFiles(documentsDirectory, "*.json").Order(StringComparer.Ordinal))
        {
            if (JsonNode.Parse(File.ReadAllText(file))?["paths"] is not JsonObject paths)
            {
                continue;
            }

            var routePrefix = _routePrefixes.GetValueOrDefault(Path.GetFileName(file), string.Empty);

            foreach (var (path, item) in paths)
            {
                foreach (var method in _methods)
                {
                    if (item?[method] is not JsonObject operation || operation[ExtensionName] is not JsonArray entries)
                    {
                        continue;
                    }

                    foreach (var entry in entries)
                    {
                        var trigger = entry?["trigger"]?.GetValue<string>();
                        if (trigger is null)
                        {
                            continue;
                        }

                        if (!result.TryGetValue(trigger, out var list))
                        {
                            result[trigger] = list = [];
                        }

                        list.Add(new RaisingOperation(
                            method.ToUpperInvariant(),
                            routePrefix + path.TrimStart('/'),
                            operation["summary"]?.GetValue<string>(),
                            entry["when"]?.GetValue<string>()));
                    }
                }
            }
        }

        foreach (var list in result.Values)
        {
            list.Sort((a, b) =>
            {
                var byPath = string.CompareOrdinal(a.Path, b.Path);

                return byPath != 0 ? byPath : Array.IndexOf(_methods, a.Method.ToLowerInvariant()) - Array.IndexOf(_methods, b.Method.ToLowerInvariant());
            });
        }

        return result;
    }
}
