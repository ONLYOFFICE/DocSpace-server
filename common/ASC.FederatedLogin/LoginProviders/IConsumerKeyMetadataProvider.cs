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

namespace ASC.FederatedLogin.LoginProviders;

public interface IConsumerKeyMetadataProvider
{
    AuthKeyMetadata GetKeyMetadata(string key);
}

/// <summary>
/// The UI metadata for a consumer key field.
/// </summary>
public class AuthKeyMetadata
{
    /// <summary>
    /// The display order of the field.
    /// </summary>
    public int Order { get; init; } = int.MaxValue;

    /// <summary>
    /// The key is not offered on this installation (an SQLite file path outside the server version), so it is left out
    /// of the settings form.
    /// </summary>
    public bool Hidden { get; init; }

    /// <summary>
    /// The field type: "text", "password", "select", "toggle".
    /// </summary>
    public string Type { get; init; } = "text";

    /// <summary>
    /// The list of options for "select" type fields.
    /// </summary>
    public List<string> Options { get; init; }

    /// <summary>
    /// The name of another key this field depends on for visibility.
    /// </summary>
    public string DependsOn { get; init; }

    /// <summary>
    /// One value of <see cref="DependsOn"/> key that makes this field visible, for clients that do not read
    /// <see cref="DependsOnValues"/>: set it to the first of those values.
    /// </summary>
    public string DependsOnValue { get; init; }

    /// <summary>
    /// All values of <see cref="DependsOn"/> key that make this field visible; set it on every field with
    /// <see cref="DependsOn"/>, it supersedes <see cref="DependsOnValue"/>.
    /// </summary>
    public List<string> DependsOnValues { get; init; }
}
