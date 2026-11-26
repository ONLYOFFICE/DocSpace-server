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
/// Nothing goes around the restore guard: the SDK restore call and the raw restore route appear in the
/// suite's sources only in <c>BackupTestBase</c>, each inside the delegate handed to <see cref="RestoreGuard"/>:
/// the raw route and one SDK call behind the guard that lets nothing queue, one SDK call behind the guard that
/// lets only a restore of the test's own portal through, and one behind the guard of the server-wide restore. The sources are scanned as text with their comments blanked out, so a
/// comment that names the endpoint is not taken for a call. It needs no portal and sends nothing.
/// </summary>
public class RestoreGuardCoverageTests
{
    private const string HelperFile = "BackupTestBase.cs";
    private const string ThisFile = "RestoreGuardCoverageTests.cs";

    // Built from parts so that this file does not match its own search.
    private static readonly Regex _sdkCall = new(@"\.Start" + "BackupRestore" + @"\w*\(", RegexOptions.CultureInvariant);
    private static readonly Regex _rawRoute = new("backup/start" + "restore", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex _blockComment = new(@"/\*.*?\*/", RegexOptions.Singleline);
    private static readonly Regex _lineComment = new(@"(^|[^:])//.*$", RegexOptions.Multiline);

    [Fact]
    public void RestoreSdkCall_IsMadeOnlyByTheGuardedHelper()
    {
        // Arrange
        var sources = ReadSuiteSources();

        // Act
        var offenders = sources
            .Where(source => source.Name is not (HelperFile or ThisFile))
            .SelectMany(source => LinesMatching(source, _sdkCall))
            .ToList();

        // Assert
        offenders.Should().BeEmpty("the restore SDK call must go through the guard in BackupTestBase");
    }

    [Fact]
    public void RestoreRoute_IsReachedByRawRequestOnlyByTheGuardedHelper()
    {
        // Arrange
        var sources = ReadSuiteSources();

        // Act
        var offenders = sources
            .Where(source => source.Name is not (HelperFile or ThisFile))
            .SelectMany(source => LinesMatching(source, _rawRoute))
            .ToList();

        // Assert
        offenders.Should().BeEmpty("the restore route must be reached through the guard in BackupTestBase");
    }

    [Fact]
    public void GuardedHelper_MakesThreeSdkCallsAndOneRawCall_EachInsideItsGuard()
    {
        // Arrange
        var helper = ReadSuiteSources().Single(source => source.Name == HelperFile).Code;

        // Act
        var sdkCalls = _sdkCall.Matches(helper).Select(match => match.Index).ToList();
        var routes = _rawRoute.Matches(helper).Select(match => match.Index).ToList();

        // Assert: one SDK call and the raw route behind the guard that lets nothing queue, one SDK call behind
        // the guard that lets only a restore of the test's own portal through, and one behind the guard of the
        // server-wide restore, which needs its environment variable
        sdkCalls.Should().HaveCount(3);
        routes.Should().HaveCount(1);

        GuardsOf(helper, sdkCalls).Should().BeEquivalentTo([CannotQueueGuard, OwnPortalGuard, ServerWideGuard]);
        GuardsOf(helper, routes).Should().BeEquivalentTo([CannotQueueGuard]);
    }

    private const string CannotQueueGuard = "RestoreGuard.SendAsync(";
    private const string OwnPortalGuard = "RestoreGuard.SendOwnPortalRestoreAsync(";
    private const string ServerWideGuard = "RestoreGuard.SendServerWideRestoreAsync(";

    // The guard each call sits in: the nearest guard call before it, within the delegate passed to it.
    private static List<string> GuardsOf(string helper, IEnumerable<int> calls)
    {
        var guards = new List<string>();

        foreach (var index in calls)
        {
            var nearest = new[] { CannotQueueGuard, OwnPortalGuard, ServerWideGuard }
                .Select(guard => (guard, at: helper.LastIndexOf(guard, index, StringComparison.Ordinal)))
                .MaxBy(found => found.at);

            nearest.at.Should().BeGreaterThanOrEqualTo(0, "every restore call has to go through a guard");
            (index - nearest.at).Should().BeLessThan(400, "the call has to sit inside the delegate passed to the guard");

            guards.Add(nearest.guard);
        }

        return guards;
    }

    // Every .cs file of the suite outside bin and obj, with its comments blanked out.
    private static List<SuiteSource> ReadSuiteSources()
    {
        var root = AppContext.BaseDirectory;

        while (root is not null && !File.Exists(Path.Combine(root, "ASC.Data.Backup.Tests.csproj")))
        {
            root = Path.GetDirectoryName(root);
        }

        root.Should().NotBeNull("the sources of the suite are needed to check that nothing goes around the guard");

        return Directory.EnumerateFiles(root!, "*.cs", SearchOption.AllDirectories)
            .Select(file => (file, relative: Path.GetRelativePath(root!, file)))
            .Where(entry => entry.relative.Split(Path.DirectorySeparatorChar)[0] is not ("bin" or "obj"))
            .Select(entry => new SuiteSource(Path.GetFileName(entry.file), entry.relative, WithoutComments(File.ReadAllText(entry.file))))
            .ToList();
    }

    // Blanks comments out but keeps the line numbers.
    private static string WithoutComments(string source)
    {
        var blanked = _blockComment.Replace(source, block => Regex.Replace(block.Value, "[^\n]", " "));

        return _lineComment.Replace(blanked, "$1");
    }

    private static IEnumerable<string> LinesMatching(SuiteSource source, Regex pattern)
    {
        return source.Code.Split('\n')
            .Select((line, index) => (line, at: $"{source.RelativePath}:{index + 1}"))
            .Where(entry => pattern.IsMatch(entry.line))
            .Select(entry => entry.at);
    }

    private sealed record SuiteSource(string Name, string RelativePath, string Code);
}
