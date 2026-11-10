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
// source code, which remains licensed under the GNU Affero General Public
// License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only


namespace ASC.Data.Backup.Tests.Tests.Restore;

/// <summary>
/// The bodies the restore guard has to refuse and the ones it has to let through, as the JSON the guard
/// reads. A null value stands for a request without a body.
/// </summary>
public static class RestoreGuardData
{
    private const string GuidText = "11111111-2222-3333-4444-555555555555";
    private const string NotAGuid = "not-a-guid";

    /// <summary>Bodies whose <c>backupId</c> is, or could be read as, a GUID or a path.</summary>
    public static IReadOnlyDictionary<string, string?> RefusedBackupId { get; } = new Dictionary<string, string?>
    {
        ["an all-zero GUID"] = Json(new { backupId = "00000000-0000-0000-0000-000000000000" }),
        ["a GUID"] = Json(new { backupId = GuidText }),
        ["an upper-case GUID"] = Json(new { backupId = GuidText.ToUpperInvariant() }),
        ["a GUID without hyphens"] = Json(new { backupId = GuidText.Replace("-", "") }),
        ["a GUID in braces"] = Json(new { backupId = "{" + GuidText + "}" }),
        ["a GUID in parentheses"] = Json(new { backupId = "(" + GuidText + ")" }),
        ["a GUID in the hexadecimal form"] = Json(new { backupId = "{0x11111111,0x2222,0x3333,{0x44,0x44,0x55,0x55,0x55,0x55,0x55,0x55}}" }),
        ["a GUID with spaces around it"] = Json(new { backupId = "  " + GuidText + "  " }),
        ["32 hexadecimal letters"] = Json(new { backupId = "abcdefabcdefabcdefabcdefabcdefab" }),
        ["a GUID in another field"] = Json(new { backupId = NotAGuid, storageType = GuidText }),
        ["a GUID in a storageParams value"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { key = "folderId", value = GuidText } } }),
        ["a numeric string backupId"] = Json(new { backupId = "123" }),
        ["a backupId that looks like a path"] = Json(new { backupId = "a/b" }),
        ["a backupId with a dot"] = Json(new { backupId = "a.b" }),
        ["a backupId that is too long to rule out a GUID"] = Json(new { backupId = new string('a', 21) }),
        ["a non-empty object backupId"] = Json(new { backupId = new { a = 1 } }),
        ["a non-empty array backupId"] = Json(new { backupId = new[] { "a" } })
    };

    /// <summary>Bodies refused for their storage parameters, extra fields, <c>dump</c> or overall shape.</summary>
    public static IReadOnlyDictionary<string, string?> RefusedShape { get; } = new Dictionary<string, string?>
    {
        ["a filePath storage parameter"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { key = "filePath", value = "x" } } }),
        ["a FILEPATH storage parameter"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { key = "FILEPATH", value = "x" } } }),
        ["a file_path storage parameter"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { key = "file_path", value = "x" } } }),
        ["a path in a value"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { key = "folderId", value = "/tmp/path" } } }),
        ["a storage parameter key other than folderId"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { key = "module", value = "s3" } } }),
        ["a storageParams item with an unexpected field"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { key = "folderId", extra = 1 } } }),
        ["a storageParams item that is a string"] = Json(new { backupId = NotAGuid, storageParams = new[] { "x" } }),
        ["non-empty storageParams as an object"] = Json(new { backupId = NotAGuid, storageParams = new { folderId = "1" } }),
        ["a top-level filePath"] = Json(new { backupId = NotAGuid, filePath = "x" }),
        ["a field outside the DTO"] = Json(new { backupId = NotAGuid, serverBaseUri = "https://x" }),
        ["dump true"] = Json(new { backupId = NotAGuid, dump = true }),
        ["dump as the string true"] = Json(new { backupId = NotAGuid, dump = "true" }),
        ["dump as the string 1"] = Json(new { backupId = NotAGuid, dump = "1" }),
        ["dump as a non-zero number"] = Json(new { backupId = NotAGuid, dump = 1 }),
        ["a body that is an array"] = "[]",
        ["a body that is a string"] = Json("x")
    };

    /// <summary>Bodies that cannot reach the restore queue.</summary>
    public static IReadOnlyDictionary<string, string?> Allowed { get; } = new Dictionary<string, string?>
    {
        ["no body"] = null,
        ["a null body"] = "null",
        ["an empty body"] = "{}",
        ["a backupId that is not a GUID"] = Json(new { backupId = NotAGuid }),
        ["an empty backupId"] = Json(new { backupId = "" }),
        ["a null backupId"] = Json(new { backupId = (string?)null }),
        ["a numeric backupId"] = Json(new { backupId = 123 }),
        ["a boolean backupId"] = Json(new { backupId = true }),
        ["an empty object backupId"] = Json(new { backupId = new { } }),
        ["an empty array backupId"] = Json(new { backupId = Array.Empty<string>() }),
        ["a storageType of any kind"] = Json(new { backupId = NotAGuid, storageType = 99 }),
        ["a string storageType"] = Json(new { backupId = NotAGuid, storageType = "abc" }),
        ["a notify of the wrong type"] = Json(new { backupId = NotAGuid, notify = "x" }),
        ["dump false"] = Json(new { backupId = NotAGuid, dump = false }),
        ["dump as a string that is not true"] = Json(new { backupId = NotAGuid, dump = "x" }),
        ["empty storageParams"] = Json(new { backupId = NotAGuid, storageParams = Array.Empty<string>() }),
        ["null storageParams"] = Json(new { backupId = NotAGuid, storageParams = (string?)null }),
        ["empty object storageParams"] = Json(new { backupId = NotAGuid, storageParams = new { } }),
        ["string storageParams"] = Json(new { backupId = NotAGuid, storageParams = "x" }),
        ["a folderId storage parameter"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { key = "folderId", value = "1" } } }),
        ["a null storageParams item"] = Json(new { backupId = NotAGuid, storageParams = new string?[] { null } }),
        ["a storageParams item without a key"] = Json(new { backupId = NotAGuid, storageParams = new[] { new { value = "x" } } })
    };

    /// <summary>Typed DTOs that must be refused once serialized the way the SDK sends them.</summary>
    public static IReadOnlyDictionary<string, Func<StartBackupRestoreRequestDto>> RefusedTyped { get; } = new Dictionary<string, Func<StartBackupRestoreRequestDto>>
    {
        ["a GUID backupId"] = () => new StartBackupRestoreRequestDto(backupId: GuidText),
        ["an all-zero GUID backupId"] = () => new StartBackupRestoreRequestDto(backupId: "00000000-0000-0000-0000-000000000000"),
        ["dump true"] = () => new StartBackupRestoreRequestDto(backupId: NotAGuid, dump: true),
        ["a filePath storage parameter"] = () => new StartBackupRestoreRequestDto(backupId: NotAGuid, storageParams: [new ItemKeyValuePairObjectObject("filePath", "1")]),
        ["a GUID in a storageParams value"] = () => new StartBackupRestoreRequestDto(backupId: NotAGuid, storageParams: [new ItemKeyValuePairObjectObject("folderId", GuidText)]),
        ["a storage parameter key other than folderId"] = () => new StartBackupRestoreRequestDto(backupId: NotAGuid, storageParams: [new ItemKeyValuePairObjectObject("module", "s3")])
    };

    /// <summary>Typed DTOs that must be let through.</summary>
    public static IReadOnlyDictionary<string, Func<StartBackupRestoreRequestDto>> AllowedTyped { get; } = new Dictionary<string, Func<StartBackupRestoreRequestDto>>
    {
        ["a backupId that is not a GUID"] = () => new StartBackupRestoreRequestDto(backupId: NotAGuid),
        ["an empty backupId"] = () => new StartBackupRestoreRequestDto(backupId: ""),
        ["a folderId storage parameter"] = () => new StartBackupRestoreRequestDto(backupId: NotAGuid, storageParams: [new ItemKeyValuePairObjectObject("folderId", "1")]),
        ["a storageType and notify"] = () => new StartBackupRestoreRequestDto(backupId: NotAGuid, storageType: BackupStorageType.DataStore, notify: true)
    };

    /// <summary>The refused cases that are hand-written JSON, for tests that need all of them at once.</summary>
    public static IEnumerable<string?> AllRefused => RefusedBackupId.Values.Concat(RefusedShape.Values);

    private static string Json(object value)
    {
        return JsonSerializer.Serialize(value);
    }
}
