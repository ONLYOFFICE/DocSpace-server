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


namespace ASC.Data.Backup.Tests.Tests.Restore;

/// <summary>
/// The safety device in front of <c>POST /api/2.0/backup/startrestore</c>, which replaces the data of
/// the portal - or, with <c>dump</c>, of the whole server database of the run, because the integration
/// test stack is a standalone installation. A body whose <c>backupId</c> is a GUID is looked up in the
/// history and queued, and so is one whose <c>storageParams</c> carry a <c>filePath</c>: even an
/// all-zero GUID was seen to be accepted with 200 and to queue a restore. Every request this suite
/// sends is therefore built so that it cannot reach the queue, and the guard throws before anything is
/// sent when a body is not provably of that kind.
/// </summary>
/// <remarks>
/// It is an allow-list over the JSON of the body, not a list of known bad shapes, so a form of the GUID
/// or of the path that nobody thought of is refused as well: the <c>backupId</c> has to be a short
/// string of letters and hyphens or a value the framework itself refuses, the <c>storageParams</c> may
/// only name a <c>folderId</c>, and no field outside the DTO, no <c>dump: true</c> and no GUID anywhere
/// is let through. The same text is checked whether it was written by hand or produced by the SDK
/// serialization of a typed DTO (<c>ToJson</c>, which uses the <c>DataMember</c> names the SDK sends).
/// </remarks>
public static class RestoreGuard
{
    private static readonly Regex _guidLike = new(
        "[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Letters and hyphens, and short: too short to hold the 32 digits of any form of a GUID, and with
    // no digit, slash or dot to make a path of it. \z, not $, which would let a trailing newline in.
    private static readonly Regex _safeBackupId = new(@"\A[A-Za-z-]{0,20}\z", RegexOptions.CultureInvariant);

    private static readonly Regex _dumpAsTrue = new(
        @"\A\s*(true|1)\s*\z",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex _mentionsPath = new("path", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // The only fields startrestore reads.
    private static readonly HashSet<string> _bodyFields = new(StringComparer.Ordinal)
    {
        "backupId", "storageType", "storageParams", "notify", "dump"
    };

    // The only storageParams key a safe request may carry.
    private const string AllowedParamKey = "folderId";

    /// <summary>
    /// Throws, before anything is sent, unless the body provably cannot reach the restore queue. A null,
    /// empty or JSON <c>null</c> body is let through: it carries nothing to restore from.
    /// </summary>
    public static void AssertCannotQueue(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw Refusal("the body is not valid JSON", json);
        }

        using (document)
        {
            Check(document.RootElement, json);
        }
    }

    /// <summary>The same check over a typed DTO, serialized the way the SDK serializes it.</summary>
    public static void AssertCannotQueue(StartBackupRestoreRequestDto? dto)
    {
        AssertCannotQueue(dto?.ToJson());
    }

    /// <summary>Runs the check and only then <paramref name="send"/>; a refused body never reaches it.</summary>
    public static async Task<T> SendAsync<T>(string? json, Func<Task<T>> send)
    {
        AssertCannotQueue(json);

        return await send();
    }

    /// <summary>Runs the check over a typed DTO and only then <paramref name="send"/>.</summary>
    public static async Task<T> SendAsync<T>(StartBackupRestoreRequestDto? dto, Func<Task<T>> send)
    {
        AssertCannotQueue(dto);

        return await send();
    }

    private static void Check(JsonElement body, string json)
    {
        if (body.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (body.ValueKind != JsonValueKind.Object)
        {
            throw Refusal("the body is not an object", json);
        }

        foreach (var property in body.EnumerateObject())
        {
            if (!_bodyFields.Contains(property.Name))
            {
                throw Refusal($"the field {property.Name} is not part of the DTO", json);
            }
        }

        // The raw text catches the plain forms, the decoded strings the ones hidden behind \u escapes.
        if (_guidLike.IsMatch(json) || StringsOf(body).Any(_guidLike.IsMatch))
        {
            throw Refusal("it holds a GUID", json);
        }

        // Every property is looked at, so a duplicated name cannot hide one value behind another.
        foreach (var property in body.EnumerateObject())
        {
            switch (property.Name)
            {
                case "backupId":
                    CheckBackupId(property.Value, json);
                    break;
                case "dump":
                    CheckDump(property.Value, json);
                    break;
                case "storageParams":
                    CheckStorageParams(property.Value, json);
                    break;
            }
        }
    }

    private static void CheckBackupId(JsonElement backupId, string json)
    {
        switch (backupId.ValueKind)
        {
            case JsonValueKind.String:
                if (!_safeBackupId.IsMatch(backupId.GetString()!))
                {
                    throw Refusal("the backupId could be a GUID or a path", json);
                }

                break;
            case JsonValueKind.Null:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                break;
            case JsonValueKind.Object when !backupId.EnumerateObject().Any():
            case JsonValueKind.Array when backupId.GetArrayLength() == 0:
                break;
            default:
                throw Refusal("the backupId is not a plain value", json);
        }
    }

    private static void CheckDump(JsonElement dump, string json)
    {
        var asksForServerWide = dump.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.String => _dumpAsTrue.IsMatch(dump.GetString()!),
            JsonValueKind.Number => !dump.TryGetDouble(out var number) || number != 0,
            _ => false
        };

        if (asksForServerWide)
        {
            throw Refusal("it asks for a server-wide restore", json);
        }
    }

    private static void CheckStorageParams(JsonElement storageParams, string json)
    {
        if (_mentionsPath.IsMatch(storageParams.GetRawText()) || StringsOf(storageParams).Any(_mentionsPath.IsMatch))
        {
            throw Refusal("the storageParams mention a path", json);
        }

        switch (storageParams.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in storageParams.EnumerateArray())
                {
                    CheckStorageParamsItem(item, json);
                }

                break;
            case JsonValueKind.Object when storageParams.EnumerateObject().Any():
                throw Refusal("the storageParams are not an array", json);
        }
    }

    private static void CheckStorageParamsItem(JsonElement item, string json)
    {
        if (item.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (item.ValueKind != JsonValueKind.Object)
        {
            throw Refusal("a storageParams item is not an object", json);
        }

        foreach (var field in item.EnumerateObject())
        {
            switch (field.Name)
            {
                case "value":
                    break;
                case "key":
                    if (field.Value.ValueKind != JsonValueKind.String || field.Value.GetString() != AllowedParamKey)
                    {
                        throw Refusal($"the storageParams key {field.Value.GetRawText()} is not allowed", json);
                    }

                    break;
                default:
                    throw Refusal("a storageParams item has an unexpected field", json);
            }
        }
    }

    // Every decoded string of the subtree, property names included.
    private static IEnumerable<string> StringsOf(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                yield return element.GetString()!;
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var text in StringsOf(item))
                    {
                        yield return text;
                    }
                }

                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    yield return property.Name;

                    foreach (var text in StringsOf(property.Value))
                    {
                        yield return text;
                    }
                }

                break;
        }
    }

    private static InvalidOperationException Refusal(string why, string json)
    {
        return new InvalidOperationException($"Refusing to send a restore request ({why}): {json}");
    }
}
