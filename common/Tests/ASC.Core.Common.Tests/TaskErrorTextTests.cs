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

namespace ASC.Core.Common.Tests;

/// <summary>
/// <see cref="TaskErrorText"/> decides what the metadata cascade and the external database sync keep as the error of
/// their progress, which every reader of the folder or every editor of the room reads. A failure the stand cannot
/// cause on purpose (SQL, a path, a dependency) is covered here: its text, with table names and paths, is replaced by
/// the generic one.
/// </summary>
public class TaskErrorTextTests
{
    private const string Generic = "The operation failed.";

    public static TheoryData<Exception> Refusals => new()
    {
        new ItemNotFoundException("Folder not found."),
        new System.Security.SecurityException("Access denied."),
        new TenantQuotaException("The quota is exceeded.")
    };

    public static TheoryData<Exception> Failures => new()
    {
        new Exception("Table 'onlyoffice.files_folder' doesn't exist"),
        new InvalidOperationException("Sequence contains no elements"),
        new IOException(@"Could not find a part of the path 'C:\Data\tenant1'."),
        new NullReferenceException(),
        new HttpRequestException("Connection refused (db.internal:3306)"),
        new ArgumentException("Option 'serverr' not supported."),
        new ArgumentNullException("connectionString")
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void ForReaders_ARefusalTheTaskRaises_KeepsItsText(Exception refusal)
    {
        TaskErrorText.ForReaders(refusal, Generic).Should().BeSameAs(refusal);
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public void ForReaders_AnyOtherFailure_GetsTheGenericText(Exception failure)
    {
        var shown = TaskErrorText.ForReaders(failure, Generic);

        shown.Message.Should().Be(Generic);
        shown.Should().NotBeSameAs(failure);
    }
}
