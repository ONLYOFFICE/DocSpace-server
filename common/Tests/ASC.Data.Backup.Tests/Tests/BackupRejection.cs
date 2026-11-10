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


namespace ASC.Data.Backup.Tests.Tests;

/// <summary>
/// What a request answered, for the tests that send a body the server has to refuse: the status and the
/// text of the body, from either a typed SDK call (an <see cref="ApiException"/>) or a raw request. A
/// request that was accepted reports status 200.
/// </summary>
/// <param name="Status">The HTTP status.</param>
/// <param name="Body">The text of the answer.</param>
public sealed record BackupRejection(int Status, string Body)
{
    /// <summary>The body with its braces doubled, to be put in the "because" text of an assertion without being read as a format string.</summary>
    public string Explain => Body.Replace("{", "{{").Replace("}", "}}");

    /// <summary>Runs a typed SDK call and reports its answer; a non-2xx answer is the status of the exception.</summary>
    public static async Task<BackupRejection> FromCallAsync(Func<Task> call)
    {
        try
        {
            await call();

            return new BackupRejection(200, "");
        }
        catch (ApiException e)
        {
            return new BackupRejection(e.ErrorCode, e.ErrorContent?.ToString() ?? "");
        }
    }

    /// <summary>Reads the answer of a raw request and disposes it.</summary>
    public static async Task<BackupRejection> FromResponseAsync(HttpResponseMessage response)
    {
        using (response)
        {
            return new BackupRejection((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
    }

    /// <summary>
    /// The names under <c>response.errors</c> of a validation answer, e.g. <c>$.storageType</c> for a body
    /// whose field the framework could not read. Empty when the answer has no such object.
    /// </summary>
    public IReadOnlyList<string> ErrorFields()
    {
        if (!TryParse(out var document))
        {
            return [];
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("response", out var response)
                && response.ValueKind == JsonValueKind.Object
                && response.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Object)
            {
                return errors.EnumerateObject().Select(property => property.Name).ToList();
            }

            return [];
        }
    }

    /// <summary>Whether the answer explains the failure: a non-null <c>error</c>, or the validation errors of the framework.</summary>
    public bool HasError()
    {
        if (!TryParse(out var document))
        {
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            // A DocSpace error carries `error`; a body the framework refuses before the action (an
            // empty body, a value of the wrong type) carries the validation problem in `response.errors`.
            return (document.RootElement.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
                || (document.RootElement.TryGetProperty("response", out var response)
                    && response.ValueKind == JsonValueKind.Object
                    && response.TryGetProperty("errors", out var errors)
                    && errors.ValueKind == JsonValueKind.Object);
        }
    }

    private bool TryParse(out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(Body);

            return true;
        }
        catch (JsonException)
        {
            document = null!;

            return false;
        }
    }
}
