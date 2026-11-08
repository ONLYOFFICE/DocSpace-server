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
/// <remarks>
/// It answers as <c>AccountingClient</c> documents the service: a missing limit is 404 on a read and 400
/// "Service limit 'N' not found" on a change or a deletion; a second limit of the same scope, or for the same user, is
/// 409; the list of the participants holds the limit in force for each of them, the switched-off ones left out; the
/// reads that are asked for it carry the spending of the current period, all of it still available.
/// </remarks>
internal sealed class ServiceLimitStore
{
    private const string LimitsPath = "/serviceLimit";
    private const string ParticipantsPath = "/serviceLimit/participants";
    private const string Custom = "Custom";

    private static readonly Regex _byCustomer = new(
        @"^/serviceLimit/by-customer/\{portalId\}/by-group/(?<group>[^/]+)/(?<kind>total|global|participants|participants/disable)$",
        RegexOptions.Compiled);

    private readonly ConcurrentDictionary<int, JsonObject> _limits = new();
    private readonly ConcurrentDictionary<int, bool> _goneBeforeNextChange = new();

    // A change reads what is there and writes it back; one at a time, so two requests never write over each other.
    private readonly Lock _writeLock = new();
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
        var accountNumber = portal?.AccountNumberIfCustomer;

        if (call.Method == "POST" && call.Route == LimitsPath)
        {
            return accountNumber is { } account ? Create(call, account) : CustomerNotFound(call.PortalId);
        }

        if (call.Method == "PUT" && call.Route == LimitsPath)
        {
            return Update(call);
        }

        if (call.Method == "PUT" && call.Route == ParticipantsPath)
        {
            return accountNumber is { } account ? SetParticipants(call, account) : CustomerNotFound(call.PortalId);
        }

        var byCustomer = _byCustomer.Match(call.Route);
        if (byCustomer.Success)
        {
            if (accountNumber is not { } account)
            {
                return CustomerNotFound(call.PortalId);
            }

            var group = byCustomer.Groups["group"].Value;

            return (call.Method, byCustomer.Groups["kind"].Value) switch
            {
                ("PUT", "participants/disable") => DisableParticipants(account, group),
                ("GET", var kind and not "participants/disable") => ByCustomer(account, group, kind, HttpUtility.ParseQueryString(call.Query)),
                _ => null
            };
        }

        if (int.TryParse(call.Route[(LimitsPath.Length + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return call.Method switch
            {
                "GET" => _limits.TryGetValue(id, out var limit) ? PaymentServiceAnswer.Json(limit.ToJsonString()) : NotFound(),
                "DELETE" => Delete(id),
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

    /// <summary>
    /// The limit <paramref name="id"/> is deleted by somebody else just before the next request that changes or deletes
    /// it arrives, as when two administrators act at once: that request finds no limit.
    /// </summary>
    public void GoneBeforeNextChange(int id)
    {
        _goneBeforeNextChange[id] = true;
    }

    private PaymentServiceAnswer Create(PaymentServiceCall call, int accountNumber)
    {
        var body = JsonNode.Parse(call.Body!)!.AsObject();
        var group = (string)body["serviceGroupName"]!;
        var scope = body["scope"]!.ToString();
        var participant = (string?)body["customerParticipantName"];

        lock (_writeLock)
        {
            // One limit of the portal, one for every user and one per user: a second one of the same kind is refused.
            var existing = OfAccount(accountNumber, group).FirstOrDefault(limit => (string?)limit["scope"] == scope
                && (scope != Custom || (string?)limit["participant"] == participant));

            if (existing is not null)
            {
                return PaymentServiceAnswer.Json(HttpStatusCode.Conflict,
                    $$"""{"title":"Conflict","status":409,"detail":"Service limit with the scope '{{scope}}' already exists"}""");
            }

            var id = Interlocked.Increment(ref _lastId);
            var limit = Limit(id, accountNumber, group, scope, participant, (decimal?)body["amountValue"], (int?)body["quantityValue"],
                body["period"]?.ToString() ?? "Day");

            _limits[id] = limit;

            return PaymentServiceAnswer.Json(HttpStatusCode.Created, limit.ToJsonString());
        }
    }

    private PaymentServiceAnswer Update(PaymentServiceCall call)
    {
        var body = JsonNode.Parse(call.Body!)!.AsObject();
        var id = (int)body["id"]!;

        lock (_writeLock)
        {
            if (!TakeForChange(id, out var limit))
            {
                return LimitNotFound(id);
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
    }

    private PaymentServiceAnswer Delete(int id)
    {
        lock (_writeLock)
        {
            return TakeForChange(id, out _) && _limits.TryRemove(id, out _)
                ? PaymentServiceAnswer.Json(HttpStatusCode.NoContent, string.Empty)
                : LimitNotFound(id);
        }
    }

    // The personal limit of each user is replaced as a whole - a threshold left out is removed - and switched on.
    private PaymentServiceAnswer SetParticipants(PaymentServiceCall call, int accountNumber)
    {
        var body = JsonNode.Parse(call.Body!)!.AsObject();
        var group = (string)body["serviceGroupName"]!;
        var participants = body["customerParticipantNames"]!.AsArray().Select(name => (string)name!).Distinct().ToList();
        var period = body["period"]?.ToString() ?? "Day";

        lock (_writeLock)
        {
            foreach (var participant in participants)
            {
                var existing = OfAccount(accountNumber, group)
                    .FirstOrDefault(limit => (string?)limit["scope"] == Custom && (string?)limit["participant"] == participant);

                var id = existing is null ? Interlocked.Increment(ref _lastId) : (int)existing["id"]!;

                _limits[id] = Limit(id, accountNumber, group, Custom, participant, (decimal?)body["amountValue"], (int?)body["quantityValue"], period);
            }
        }

        return PaymentServiceAnswer.Json(participants.Count.ToString(CultureInfo.InvariantCulture));
    }

    // Only the personal limits in force are switched off, so a repeated call switches off nothing more.
    private PaymentServiceAnswer DisableParticipants(int accountNumber, string group)
    {
        var count = 0;

        lock (_writeLock)
        {
            foreach (var limit in OfAccount(accountNumber, group).Where(limit => (string?)limit["scope"] == Custom && (bool)limit["enabled"]!))
            {
                var disabled = (JsonObject)limit.DeepClone();
                disabled["enabled"] = false;

                _limits[(int)limit["id"]!] = disabled;
                count++;
            }
        }

        return PaymentServiceAnswer.Json(count.ToString(CultureInfo.InvariantCulture));
    }

    // The limit of the portal comes with its spending when it is asked for; the list of the participants comes with it
    // unless it is asked not to, as accounting attaches it by default; the limit for every user never does.
    private PaymentServiceAnswer ByCustomer(int accountNumber, string group, string kind, NameValueCollection query)
    {
        var limits = OfAccount(accountNumber, group).ToList();

        if (kind == "participants")
        {
            return Participants(limits, query);
        }

        var scope = kind == "total" ? "Total" : "Global";
        var found = limits.FirstOrDefault(limit => (string?)limit["scope"] == scope);

        return found is null
            ? NotFound()
            : PaymentServiceAnswer.Json(WithUsage(found, kind == "total" && query["includeUsage"] == "true").ToJsonString());
    }

    // The limit in force for each participant, as AccountingClient documents the list: the personal one when it is
    // switched on, or else the limit for every user, its ID repeated for each participant it applies to. A switched-off
    // limit is not listed. Accounting knows the participants that have used the services; here, the ones that have ever
    // had a personal limit. Newest limits first unless asked otherwise, then paged by offset and limit.
    private static PaymentServiceAnswer Participants(List<JsonObject> limits, NameValueCollection query)
    {
        var personal = limits.Where(limit => (string?)limit["scope"] == Custom).ToList();
        var forEveryUser = limits.FirstOrDefault(limit => (string?)limit["scope"] == "Global" && (bool)limit["enabled"]!);

        var inForce = new List<JsonObject>();

        foreach (var participant in personal.Select(limit => (string)limit["participant"]!).Distinct())
        {
            var own = personal.FirstOrDefault(limit => (string?)limit["participant"] == participant && (bool)limit["enabled"]!);

            if (own is not null)
            {
                inForce.Add(own);
            }
            else if (forEveryUser is not null)
            {
                var shared = (JsonObject)forEveryUser.DeepClone();
                shared["participant"] = participant;
                inForce.Add(shared);
            }
        }

        // A limit is created with the next id, so the id is the order of creation.
        var ascending = query["orderType"] == "Ascending";
        IEnumerable<JsonObject> ordered = query["orderBy"] == "Participant"
            ? ascending
                ? inForce.OrderBy(limit => (string)limit["participant"]!, StringComparer.Ordinal)
                : inForce.OrderByDescending(limit => (string)limit["participant"]!, StringComparer.Ordinal)
            : ascending
                ? inForce.OrderBy(limit => (int)limit["id"]!).ThenBy(limit => (string)limit["participant"]!, StringComparer.Ordinal)
                : inForce.OrderByDescending(limit => (int)limit["id"]!).ThenBy(limit => (string)limit["participant"]!, StringComparer.Ordinal);

        var offset = int.TryParse(query["offset"], NumberStyles.None, CultureInfo.InvariantCulture, out var from) ? from : 0;
        var page = ordered.Skip(offset);

        if (int.TryParse(query["limit"], NumberStyles.None, CultureInfo.InvariantCulture, out var size))
        {
            page = page.Take(size);
        }

        var withUsage = query["includeUsage"] != "false";

        return PaymentServiceAnswer.Json(new JsonObject
        {
            ["collection"] = new JsonArray([.. page.Select(limit => WithUsage(limit, withUsage))]),
            ["totalQuantity"] = inForce.Count
        }.ToJsonString());
    }

    private IEnumerable<JsonObject> OfAccount(int accountNumber, string group)
    {
        return _limits.Values.Where(limit => (int)limit["customerAccountNumber"]! == accountNumber
            && string.Equals((string?)limit["serviceGroupName"], group, StringComparison.OrdinalIgnoreCase));
    }

    // Whether the limit can be changed: false when it does not exist, or when somebody else has just deleted it.
    private bool TakeForChange(int id, out JsonObject limit)
    {
        if (_goneBeforeNextChange.TryRemove(id, out _))
        {
            _limits.TryRemove(id, out _);
        }

        return _limits.TryGetValue(id, out limit!);
    }

    // Nothing has been spent in the current period yet, so all of each threshold is still available.
    private static JsonObject WithUsage(JsonObject limit, bool include)
    {
        var copy = (JsonObject)limit.DeepClone();

        if (!include)
        {
            return copy;
        }

        var today = DateTime.UtcNow.Date;
        var (start, nextStart) = limit["period"]!.ToString() switch
        {
            "Day" => (today, today.AddDays(1)),
            "Week" => (today.AddDays(-(((int)today.DayOfWeek + 6) % 7)), today.AddDays(7 - (((int)today.DayOfWeek + 6) % 7))),
            "Month" => (new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)),
            _ => (today, (DateTime?)null)
        };

        copy["usage"] = new JsonObject
        {
            ["periodStart"] = start.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            ["periodEnd"] = nextStart?.AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            ["amountConsumed"] = 0,
            ["amountAvailable"] = limit["amountValue"]?.DeepClone(),
            ["quantityConsumed"] = 0,
            ["quantityAvailable"] = limit["quantityValue"]?.DeepClone()
        };

        return copy;
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
            ["currency"] = amountValue is null ? null : "USD",
            ["quantityValue"] = quantityValue,
            ["period"] = period,
            ["enabled"] = true
        };
    }

    private static PaymentServiceAnswer NotFound()
    {
        return PaymentServiceAnswer.Json(HttpStatusCode.NotFound, """{"title":"Resource not found","status":404}""");
    }

    private static PaymentServiceAnswer LimitNotFound(int id)
    {
        return PaymentServiceAnswer.Json(HttpStatusCode.BadRequest,
            $$"""{"title":"Bad request","status":400,"detail":"Service limit '{{id}}' not found"}""");
    }

    private static PaymentServiceAnswer CustomerNotFound(string? portalId)
    {
        return PaymentServiceAnswer.Json(HttpStatusCode.BadRequest,
            $$"""{"title":"Bad request","status":400,"detail":"Customer '{{portalId}}' not found"}""");
    }
}
