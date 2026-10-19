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


namespace ASC.Web.Api.Tests.Tests._09_Apps;

/// <summary>
/// Shared inputs of the Apps suites.
/// </summary>
public static class AppsTestData
{
    /// <summary>
    /// Every kind of JSON value a settings document can be (name, raw JSON). <c>null</c> is not here: it
    /// resets the document and has its own tests.
    /// </summary>
    public static TheoryData<string, string> JsonValues => new()
    {
        { "simple object", """{"a":1}""" },
        { "object with several fields", """{"a":1,"b":"two","c":true,"d":[1,2]}""" },
        { "nested object", """{"a":{"b":{"c":{"d":"deep"}}}}""" },
        { "array", """[1,"two",false]""" },
        { "array of objects", """[{"id":1},{"id":2,"tags":["x"]}]""" },
        { "nested arrays", """[[1,[2,[3]]],[]]""" },
        { "string", "\"plain string\"" },
        { "number", "42" },
        { "negative number", "-7" },
        { "fractional number", "3.14159" },
        { "true", "true" },
        { "false", "false" },
        { "zero", "0" },
        { "empty string", "\"\"" },
        { "empty array", "[]" },
        { "empty object", "{}" },
        { "Unicode", """{"ru":"Привет, мир","zh":"你好","emoji":"😀🚀","rtl":"مرحبا"}""" },
        { "special characters", """{"html":"<script>alert(1)</script>","quotes":"\"double\" 'single' \\ back\\slash","control":"line1\nline2\ttab","sql":"' OR 1=1 --"}""" },
        { "keys with spaces and symbols", """{"a key":1,"k/e\\y":2,"<>":3,"":4}""" },
        { "nested null", """{"a":null,"list":[null,1],"o":{"n":null}}""" },
    };
}
