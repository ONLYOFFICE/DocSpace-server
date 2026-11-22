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

namespace ASC.Web.Api.Tests.Tests._10_TestHost;

/// <summary>
/// What keeps a test run apart from the runs of other checkouts and suites: its own ports (HostPorts) and its own
/// storage and logs (AppPaths). No portal is involved; the class lives here only because the AppHost project, which no
/// unit-test project references, is where both are. Run on its own, it still waits for the Aspire host that the
/// assembly fixture of this suite starts for every test - which is also what has switched this process to free ports.
/// </summary>
[Trait("Category", "TestHost")]
public sealed class TestRunIsolationTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("asc-checkouts-");

    public void Dispose()
    {
        _root.Delete(recursive: true);
    }

    [Fact]
    public void MainCheckout_KeepsTheLogFolder_AndStoresUnderAFolderOfItsOwn()
    {
        // Arrange: <base>/buildtools and the main checkout <base>/server
        var basePath = CreateBase();
        var appHost = CreateAppHost(Path.Combine(basePath, "server"));

        // Act
        var name = ASC.AppHost.Configuration.AppPaths.GetCheckoutName(appHost);

        // Assert
        name.Should().BeEmpty();
        ASC.AppHost.Configuration.AppPaths.GetTestLogsDirectory(appHost).Should().Be(Path.Combine(basePath, "Logs", "test"));
        ASC.AppHost.Configuration.AppPaths.GetTestStorageRoot(appHost)
            .Should().Be(Path.Combine(basePath, "Data", "test", "_main", ASC.AppHost.Configuration.AppPaths.GetSuiteName()));
    }

    [Fact]
    public void Worktree_IsNamedByItsFolder()
    {
        // Arrange: a worktree under the main checkout, which finds <base>/buildtools by walking up
        var basePath = CreateBase();
        var appHost = CreateAppHost(Path.Combine(basePath, "server", ".claude", "worktrees", "feature-a"));

        // Act
        var name = ASC.AppHost.Configuration.AppPaths.GetCheckoutName(appHost);

        // Assert
        name.Should().Be("feature-a");
        ASC.AppHost.Configuration.AppPaths.GetTestLogsDirectory(appHost).Should().Be(Path.Combine(basePath, "Logs", "test", "feature-a"));
        ASC.AppHost.Configuration.AppPaths.GetTestStorageRoot(appHost)
            .Should().Be(Path.Combine(basePath, "Data", "test", "feature-a", ASC.AppHost.Configuration.AppPaths.GetSuiteName()));
    }

    [Fact]
    public void StorageRoots_OfTheMainCheckoutAndItsWorktrees_HoldNoneOfTheOthers()
    {
        // Arrange: the main checkout and worktrees that share its base path, one of them named like the main
        // checkout's temp folder and one like the main checkout's own storage folder
        var basePath = CreateBase();
        var worktrees = Path.Combine(basePath, "server", ".claude", "worktrees");
        string[] appHosts =
        [
            CreateAppHost(Path.Combine(basePath, "server")),
            CreateAppHost(Path.Combine(worktrees, "feature-a")),
            CreateAppHost(Path.Combine(worktrees, "temp")),
            CreateAppHost(Path.Combine(worktrees, "_main"))
        ];

        // Act
        var roots = appHosts.Select(ASC.AppHost.Configuration.AppPaths.GetTestStorageRoot).ToList();

        // Assert: the cleanup of one run deletes its root recursively, so no root may lie inside another
        AssertNoneInsideAnother(roots);
    }

    [Fact]
    public void StorageRoots_OfWorktreesWithABuildtoolsLink_HoldNoneOfTheOthers()
    {
        // Arrange: .claude/worktrees holds a buildtools of its own (the link a worktree run used to need), so it is
        // the base path of every worktree under it - one of them named like the main checkout
        var worktrees = Path.Combine(CreateBase(), "server", ".claude", "worktrees");
        Directory.CreateDirectory(Path.Combine(worktrees, "buildtools"));
        string[] appHosts =
        [
            CreateAppHost(Path.Combine(worktrees, "feature-a")),
            CreateAppHost(Path.Combine(worktrees, "feature-b")),
            CreateAppHost(Path.Combine(worktrees, "server"))
        ];

        // Act
        var names = appHosts.Select(ASC.AppHost.Configuration.AppPaths.GetCheckoutName).ToList();
        var roots = appHosts.Select(ASC.AppHost.Configuration.AppPaths.GetTestStorageRoot).ToList();

        // Assert
        names.Take(2).Should().Equal("feature-a", "feature-b");
        AssertNoneInsideAnother(roots);
    }

    [Fact]
    public void CheckoutName_ThatLosesCharacters_GetsAHashOfItsPath()
    {
        // Arrange: names the folder alphabet [a-z0-9_.-] cannot hold as they are, and one it can
        var worktrees = Path.Combine(CreateBase(), "server", ".claude", "worktrees");
        string[] appHosts =
        [
            CreateAppHost(Path.Combine(worktrees, "_Fix Ports")),
            CreateAppHost(Path.Combine(worktrees, "fix-ports")),
            CreateAppHost(Path.Combine(worktrees, "исправление")),
            CreateAppHost(Path.Combine(worktrees, "порты"))
        ];

        // Act
        var names = appHosts.Select(ASC.AppHost.Configuration.AppPaths.GetCheckoutName).ToList();

        // Assert: a name the alphabet holds is kept as it is; any other never meets another one, nor turns into the
        // main checkout's empty name
        names[0].Should().MatchRegex("^fix-ports-[0-9a-f]{8}$");
        names[1].Should().Be("fix-ports");
        names[2].Should().MatchRegex("^[0-9a-f]{8}$");
        names[3].Should().MatchRegex("^[0-9a-f]{8}$");
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void HostPorts_InATestRun_AreFreeOnesKeptAndReservedForTheRun()
    {
        // Act
        var webApi = ASC.AppHost.Configuration.HostPorts.Of(ASC.AppHost.Configuration.Constants.WebApiPort);
        var router = ASC.AppHost.Configuration.HostPorts.Of(ASC.AppHost.Configuration.Constants.AppHostPort);

        // Assert: not the fixed ports of a development stack, from the range below the ephemeral ones, one port per
        // listener, the same port on every read, so the AppHost and the fixture agree on it, and reserved
        ASC.AppHost.Configuration.HostPorts.FreePorts.Should().BeTrue();
        webApi.Should().NotBe(ASC.AppHost.Configuration.Constants.WebApiPort)
            .And.BeInRange(ASC.AppHost.Configuration.HostPorts.FirstFreePort, ASC.AppHost.Configuration.HostPorts.LastFreePort);
        router.Should().NotBe(ASC.AppHost.Configuration.Constants.AppHostPort).And.NotBe(webApi);
        ASC.AppHost.Configuration.HostPorts.Of(ASC.AppHost.Configuration.Constants.WebApiPort).Should().Be(webApi);
        File.Exists(ASC.AppHost.Configuration.HostPorts.GetReservationPath(webApi)).Should().BeTrue();
    }

    [Fact]
    public void HostPorts_APortAnotherRunHolds_IsNotReserved()
    {
        // Arrange: another run's reservation of a port - its lock file held open, as that process holds it - on a port
        // neither this run nor a run next to it has reserved
        var (port, held) = HoldReservationOfAnUnreservedPort();

        bool whileHeld;

        using (held)
        {
            // Act
            whileHeld = ASC.AppHost.Configuration.HostPorts.TryReserve(port);
        }

        var afterRelease = ASC.AppHost.Configuration.HostPorts.TryReserve(port);

        // Assert: skipped while the other run lives, reserved once it is gone
        whileHeld.Should().BeFalse();
        afterRelease.Should().BeTrue();
    }

    private static (int Port, FileStream Held) HoldReservationOfAnUnreservedPort()
    {
        var reserved = ASC.AppHost.Configuration.HostPorts.Assigned.Values.ToHashSet();

        for (var port = ASC.AppHost.Configuration.HostPorts.LastFreePort; port >= ASC.AppHost.Configuration.HostPorts.FirstFreePort; port--)
        {
            var path = ASC.AppHost.Configuration.HostPorts.GetReservationPath(port);

            if (reserved.Contains(port))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            try
            {
                return (port, new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, System.IO.FileShare.None, 1, FileOptions.DeleteOnClose));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Another run on this machine holds it, or has just let it go: try the next one.
            }
        }

        throw new InvalidOperationException("Every port of the range is reserved.");
    }

    private static void AssertNoneInsideAnother(List<string> roots)
    {
        for (var i = 0; i < roots.Count; i++)
        {
            for (var j = 0; j < roots.Count; j++)
            {
                if (i != j)
                {
                    (roots[j] + Path.DirectorySeparatorChar).Should().NotStartWith(roots[i] + Path.DirectorySeparatorChar,
                        "{0} would be deleted with {1}", roots[j], roots[i]);
                }
            }
        }
    }

    private string CreateBase()
    {
        var basePath = Path.Combine(_root.FullName, "DocSpace");
        Directory.CreateDirectory(Path.Combine(basePath, "buildtools"));

        return basePath;
    }

    private static string CreateAppHost(string checkout)
    {
        var appHost = Path.Combine(checkout, "common", "ASC.AppHost");
        Directory.CreateDirectory(appHost);

        return appHost;
    }
}
