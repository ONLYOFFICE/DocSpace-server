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

namespace ASC.Web.Api.Billing.Tests.ApiFactories;

/// <summary>
/// The spending limits of the accounting service, kept the way it keeps them: numbered across all customers, each on the
/// account of the customer it was created for. The routes by id name no portal, so the limits live here rather than
/// with a portal, and the stack's own check that a limit belongs to the portal asking - its account number against
/// the portal's balance - meets limits of other portals as it would in production.
/// </summary>
internal sealed class ServiceLimitStore
{
    private const string LimitsPath = "/serviceLimit";

    private static readonly Regex _byCustomer = new(@"^/serviceLimit/by-customer/\{portalId\}/by-group/(?<group>[^/]+)/(?<kind>total|global|participants)$",
        RegexOptions.Compiled);

    private readonly ConcurrentDictionary<int, JsonObject> _limits = new();
    private int _lastId = 500;

    /// <summary>
    /// The answer to a limit request, or null when <paramref name="call"/> is not one; <paramref name="portalOf"/> finds
    /// what is known about a portal by its payment key.
    /// </summary>
    public PaymentServiceAnswer? TryAnswer(PaymentServiceCall call, Func<string, PortalPayments?> portalOf)
    {
        if (call.Service != PaymentService.Accounting || !call.Route.StartsWith(LimitsPath, StringComparison.Ordinal))
        {
            return null;
        }

        var portal = call.PortalId is null ? null : portalOf(call.PortalId);

        if (call.Method == "POST" && call.Route == LimitsPath)
        {
            return portal?.AccountNumberIfCustomer is { } accountNumber ? Create(call, accountNumber) : CustomerNotFound(call.PortalId);
        }

        if (call.Method == "PUT" && call.Route == LimitsPath)
        {
            return Update(call);
        }

        var byCustomer = _byCustomer.Match(call.Route);
        if (byCustomer.Success)
        {
            if (portal?.AccountNumberIfCustomer is not { } accountNumber)
            {
                return CustomerNotFound(call.PortalId);
            }

            return ByCustomer(accountNumber, byCustomer.Groups["group"].Value, byCustomer.Groups["kind"].Value);
        }

        if (int.TryParse(call.Route[(LimitsPath.Length + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return call.Method switch
            {
                "GET" => _limits.TryGetValue(id, out var limit) ? PaymentServiceAnswer.Json(limit.ToJsonString()) : NotFound(),
                "DELETE" => _limits.TryRemove(id, out _) ? PaymentServiceAnswer.Json(HttpStatusCode.NoContent, string.Empty) : NotFound(),
                _ => null
            };
        }

        return null;
    }

    /// <summary>Whether the limit <paramref name="id"/> exists, on whatever account.</summary>
    public bool Contains(int id)
    {
        return _limits.ContainsKey(id);
    }

    /// <summary>
    /// Every limit on the account <paramref name="accountNumber"/> as accounting holds it, in the order of their ids: two
    /// snapshots are equal only when no limit of the account was created, changed or deleted in between.
    /// </summary>
    public string Snapshot(int accountNumber)
    {
        return new JsonArray([.. _limits.Values
            .Where(limit => (int)limit["customerAccountNumber"]! == accountNumber)
            .OrderBy(limit => (int)limit["id"]!)
            .Select(limit => limit.DeepClone())]).ToJsonString();
    }

    /// <summary>Puts a limit on the account <paramref name="accountNumber"/> as if another portal had created it.</summary>
    public int Add(int accountNumber, string scope, decimal amountValue, string period = "Day", string? participant = null)
    {
        var id = Interlocked.Increment(ref _lastId);

        _limits[id] = Limit(id, accountNumber, "ai-services", scope, participant, amountValue, null, period);

        return id;
    }

    private PaymentServiceAnswer Create(PaymentServiceCall call, int accountNumber)
    {
        var body = JsonNode.Parse(call.Body!)!.AsObject();
        var id = Interlocked.Increment(ref _lastId);

        var limit = Limit(
            id,
            accountNumber,
            (string)body["serviceGroupName"]!,
            body["scope"]!.ToString(),
            (string?)body["customerParticipantName"],
            (decimal?)body["amountValue"],
            (int?)body["quantityValue"],
            body["period"]?.ToString() ?? "Day");

        _limits[id] = limit;

        return PaymentServiceAnswer.Json(HttpStatusCode.Created, limit.ToJsonString());
    }

    private PaymentServiceAnswer Update(PaymentServiceCall call)
    {
        var body = JsonNode.Parse(call.Body!)!.AsObject();
        var id = (int)body["id"]!;

        if (!_limits.TryGetValue(id, out var limit))
        {
            return NotFound();
        }

        var updated = (JsonObject)limit.DeepClone();

        foreach (var field in new[] { "amountValue", "quantityValue", "enabled" })
        {
            if (body[field] is { } value)
            {
                updated[field] = value.DeepClone();
            }
        }

        _limits[id] = updated;

        return PaymentServiceAnswer.Json(updated.ToJsonString());
    }

    private PaymentServiceAnswer ByCustomer(int accountNumber, string group, string kind)
    {
        var limits = _limits.Values
            .Where(limit => (int)limit["customerAccountNumber"]! == accountNumber
                            && string.Equals((string?)limit["serviceGroupName"], group, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (kind == "participants")
        {
            var custom = limits.Where(limit => (string?)limit["scope"] == "Custom").Select(limit => limit.DeepClone()).ToArray();

            return PaymentServiceAnswer.Json(new JsonObject
            {
                ["collection"] = new JsonArray(custom),
                ["totalQuantity"] = custom.Length
            }.ToJsonString());
        }

        var scope = kind == "total" ? "Total" : "Global";
        var found = limits.FirstOrDefault(limit => (string?)limit["scope"] == scope);

        return found is null ? NotFound() : PaymentServiceAnswer.Json(found.ToJsonString());
    }

    private static JsonObject Limit(int id, int accountNumber, string group, string scope, string? participant, decimal? amountValue,
        int? quantityValue, string period)
    {
        return new JsonObject
        {
            ["id"] = id,
            ["serviceGroupName"] = group,
            ["customerAccountNumber"] = accountNumber,
            ["scope"] = scope,
            ["participant"] = participant,
            ["amountValue"] = amountValue,
            ["currency"] = "USD",
            ["quantityValue"] = quantityValue,
            ["period"] = period,
            ["enabled"] = true
        };
    }

    private static PaymentServiceAnswer NotFound()
    {
        return PaymentServiceAnswer.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}""");
    }

    private static PaymentServiceAnswer CustomerNotFound(string? portalId)
    {
        return PaymentServiceAnswer.Json(HttpStatusCode.BadRequest,
            $$"""{"title":"Bad request","status":400,"detail":"Customer '{{portalId}}' not found"}""");
    }
}
