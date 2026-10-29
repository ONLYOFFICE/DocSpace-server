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

namespace ASC.Core.Common.Tests.Webhooks;

/// <summary>
/// What <see cref="DbWorker.ToStorableText"/> makes of free text before it reaches the utf8 (three bytes at most) text
/// columns of the webhook tables: a character outside the Basic Multilingual Plane would fail the whole statement in
/// strict mode, and a NUL fails it on PostgreSQL.
/// </summary>
[Trait("Category", "Webhooks")]
public class WebhookStorableTextTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("plain text", "plain text")]
    [InlineData("before\0after", "beforeafter")]
    [InlineData("\U0001F525 down", "\uFFFD down")]
    [InlineData("a\U0001F525\U0001F525b", "a\uFFFD\uFFFDb")]
    [InlineData("caf\u00E9 \u4E2D", "caf\u00E9 \u4E2D")]
    public void ToStorableText_KeepsWhatTheColumnHolds_AndReplacesTheRest(string? value, string? expected)
    {
        DbWorker.ToStorableText(value!).Should().Be(expected);
    }

    [Theory]
    [InlineData("a\uD83D")]
    [InlineData("\uDD25b")]
    public void ToStorableText_ReplacesALoneSurrogate(string value)
    {
        var stored = DbWorker.ToStorableText(value);

        stored.Should().Contain("\uFFFD");
        stored.Any(char.IsSurrogate).Should().BeFalse("no half of a pair may reach the column");
    }
}
