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

namespace ASC.AppHost.Configuration;

/// <summary>
/// The host port each listener of this AppHost run binds: a service, the router, a container port published on the
/// host, a stub in the test process. A port is named by its fixed default (<see cref="Constants"/>), and
/// <see cref="Of"/> gives the port this run uses for it.
/// </summary>
/// <remarks>
/// A development run keeps the fixed ports, and so does a test run in the docker mode. Any other test run takes free ones instead (<see cref="UseFreePorts"/>), so two
/// runs - from two checkouts on different branches, or a test run next to a development stack - do not compete for
/// the same ports. The test fixture boots the AppHost inside the test process, so the AppHost and the fixture read
/// the same table: the router address the fixture polls, the port a stub of the test listens on and the address the
/// services are told are one value.
///
/// The free ports are drawn at random from <see cref="FirstFreePort"/>..<see cref="LastFreePort"/>, below the
/// ephemeral ranges of Windows (49152+) and Linux (32768+), from which the system hands out the local ports of
/// outgoing connections. A port is picked when the stack is built and bound by its listener only when that one starts,
/// so a run reserves each port it picks for as long as the process lives (<see cref="TryReserve"/>): another test run
/// skips it in that window. A process that is not a test run can still take it, and a probe bind before the
/// reservation (<see cref="IsFree"/>) only tells which ports are in use right then.
/// </remarks>
public static class HostPorts
{
    /// <summary>The lowest port a test run takes.</summary>
    public const int FirstFreePort = 20000;

    /// <summary>The highest port a test run takes.</summary>
    public const int LastFreePort = 32000;

    private static readonly Lock _lock = new();
    private static readonly Dictionary<int, int> _assigned = [];
    private static readonly HashSet<int> _taken = [];

    // The reservations of this process: each lock file stays open, exclusive, until the process ends - also when it is
    // killed - and the system then releases the lock, so a reservation never outlives the process. On Windows the file
    // goes with it (DeleteOnClose). On Unix it stays, unlocked, for the next run to lock again: deleting a locked file
    // there would let a run lock the old file while another creates and locks a new one under the same name.
    private static readonly List<FileStream> _reservations = [];

    // In a folder of the user's own: the temp folder of a Windows user is that user's already. On Unix the temp folder
    // is shared, and a folder there with a name known in advance could be created first by another user, who would
    // then decide which ports this one may take; ~/.local/share is the user's alone. Not XDG_RUNTIME_DIR: it is set in
    // a login session and missing under cron, sudo -u or docker exec, so two runs of one user could reserve in two
    // folders and not see each other. A home folder that is missing or cannot be written (HOME=/nonexistent of a
    // service account) falls back to the shared temp folder, by user name. Settled on first use (ReservationDirectory).
    private static string? _reservationDirectory;

    // How many free ports in a row may fail their reservation before the run stops looking. A port another run holds
    // is a port it listens on, or soon will: such ports mostly fail the probe bind and never get here, so a long run of
    // failed reservations means the folder itself refuses them, which every next port would repeat. A single lock file
    // that cannot be opened (left read-only) counts as one failure and costs the run only that port.
    private const int MaxReservationFailuresInARow = 64;

    // The Win32 errors of an open the holder of the file refuses: ERROR_SHARING_VIOLATION, ERROR_LOCK_VIOLATION.
    private const int ErrorSharingViolation = 32;
    private const int ErrorLockViolation = 33;

    /// <summary>The folder of the lock files, created on first use.</summary>
    /// <exception cref="InvalidOperationException">No candidate folder can be created.</exception>
    public static string ReservationDirectory
    {
        get
        {
            lock (_lock)
            {
                return _reservationDirectory ??= CreateReservationDirectory();
            }
        }
    }

    // null until the first decision: UseFreePorts, or a read of a port, which settles on the fixed ones.
    private static bool? _freePorts;

    /// <summary>Whether this run takes free ports rather than the fixed ones.</summary>
    public static bool FreePorts
    {
        get
        {
            lock (_lock)
            {
                return _freePorts == true;
            }
        }
    }

    /// <summary>
    /// Makes this process take free ports. Called before the first port is read: by the test fixture before it
    /// starts anything, and by the AppHost for its test profiles - neither in the docker mode (Docker=true), which keeps
    /// the fixed ports. A second call changes nothing.
    /// </summary>
    /// <exception cref="InvalidOperationException">A port was read before, so part of the run already uses the fixed ones.</exception>
    public static void UseFreePorts()
    {
        lock (_lock)
        {
            if (_freePorts == false)
            {
                throw new InvalidOperationException(
                    $"{nameof(HostPorts)}.{nameof(UseFreePorts)} was called after a port had been read: part of the run already uses the fixed ports.");
            }

            _freePorts = true;
        }
    }

    /// <summary>
    /// The port this run uses for <paramref name="defaultPort"/>: the default itself, or under
    /// <see cref="UseFreePorts"/> a free port picked on the first call and returned on every later one.
    /// </summary>
    public static int Of(int defaultPort)
    {
        lock (_lock)
        {
            _freePorts ??= false;

            if (_freePorts == false)
            {
                return defaultPort;
            }

            if (!_assigned.TryGetValue(defaultPort, out var port))
            {
                port = PickFreePort();
                _assigned[defaultPort] = port;
                _taken.Add(port);
            }

            return port;
        }
    }

    /// <summary>The ports of this run, default to the port used, for the run's log.</summary>
    public static IReadOnlyDictionary<int, int> Assigned
    {
        get
        {
            lock (_lock)
            {
                return new Dictionary<int, int>(_assigned);
            }
        }
    }

    /// <summary>
    /// Reserves <paramref name="port"/> for this process among the test runs of the machine: <c>false</c> when the
    /// port is held - by another process, or by a reservation of this one. A port reserved here stays reserved until
    /// the process ends.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The folder cannot be created, or the lock file cannot be opened for a reason the open tells apart from a holder:
    /// on Windows a read-only file or an I/O failure other than a sharing or lock violation (a full disk), on Unix a
    /// file the user may not open. What reads as held instead: on Windows a sharing or lock violation and a refused open
    /// of a file that is not read-only - a delete its holder left pending, or a folder that lets no file be created; on
    /// Unix every other I/O failure. The caller that picks the ports stops on a folder that refuses them all by count.
    /// </exception>
    public static bool TryReserve(int port)
    {
        return TryReserve(port, out _);
    }

    private static bool TryReserve(int port, out Exception? refusal)
    {
        lock (_lock)
        {
            var path = GetReservationPath(port);

            try
            {
                _reservations.Add(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                    bufferSize: 1, OperatingSystem.IsWindows() ? FileOptions.DeleteOnClose : FileOptions.None));

                refusal = null;

                return true;
            }
            catch (IOException exception) when (IsHeld(exception))
            {
                // Locked by its holder.
                refusal = exception;

                return false;
            }
            catch (UnauthorizedAccessException exception) when (OperatingSystem.IsWindows() && !IsReadOnly(path))
            {
                // Deleted by its holder on close while another handle (an indexer, an antivirus) still has it open: Windows
                // refuses the open while the delete is pending, and the file still shows. Or the folder lets no file be
                // created. Either way not this port's file to fix; a folder refusing every port ends the pick by count.
                refusal = exception;

                return false;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"The port reservation {path} cannot be written.", exception);
            }
        }
    }

    /// <summary>The lock file a run holds open while <paramref name="port"/> is reserved for it.</summary>
    public static string GetReservationPath(int port)
    {
        return Path.Combine(ReservationDirectory, port.ToString(CultureInfo.InvariantCulture) + ".lock");
    }

    private static int PickFreePort()
    {
        const int count = LastFreePort - FirstFreePort + 1;
        var start = Random.Shared.Next(count);
        var failuresInARow = 0;

        Exception? lastFailure = null;
        var directory = ReservationDirectory;

        // Ends by count: every port of the range is tried once; or earlier, when the folder refuses the reservations.
        for (var i = 0; i < count; i++)
        {
            var port = FirstFreePort + (start + i) % count;

            if (_taken.Contains(port) || !IsFree(port))
            {
                continue;
            }

            try
            {
                if (TryReserve(port, out var refusal))
                {
                    return port;
                }

                lastFailure = refusal;
            }
            catch (InvalidOperationException exception)
            {
                // This port's lock file only: the next port has a file of its own.
                lastFailure = exception;
            }

            if (++failuresInARow >= MaxReservationFailuresInARow)
            {
                throw new InvalidOperationException(
                    $"{MaxReservationFailuresInARow} free ports in a row could not be reserved in {directory}: the folder refuses the lock files.",
                    lastFailure);
            }
        }

        throw new InvalidOperationException(
            $"No free port between {FirstFreePort} and {LastFreePort}: each one was in use, or its reservation in {directory} could not be taken.");
    }

    /// <summary>
    /// Whether the open failed because another handle holds the file: a sharing or lock violation on Windows. On Unix
    /// the failed lock (EWOULDBLOCK) has no portable code that tells it from the other failures of the open, so every
    /// <see cref="IOException"/> but a missing folder counts as held, and <see cref="MaxReservationFailuresInARow"/>
    /// ends the search when the folder refuses them all.
    /// </summary>
    private static bool IsHeld(IOException exception)
    {
        if (OperatingSystem.IsWindows())
        {
            return (exception.HResult & 0xFFFF) is ErrorSharingViolation or ErrorLockViolation;
        }

        return exception is not (DirectoryNotFoundException or PathTooLongException or DriveNotFoundException);
    }

    private static bool IsReadOnly(string path)
    {
        try
        {
            return File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>The first candidate folder that can be created: see <see cref="_reservationDirectory"/>.</summary>
    private static string CreateReservationDirectory()
    {
        var candidates = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            candidates.Add(Path.Combine(Path.GetTempPath(), "docspace-test-ports"));
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (!string.IsNullOrEmpty(home))
            {
                candidates.Add(Path.Combine(home, ".local", "share", "docspace-test-ports"));
            }

            candidates.Add(Path.Combine(Path.GetTempPath(), $"docspace-test-ports-{Environment.UserName}"));
        }

        Exception? failure = null;

        foreach (var candidate in candidates)
        {
            try
            {
                Directory.CreateDirectory(candidate);

                return candidate;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                failure = exception;
            }
        }

        throw new InvalidOperationException($"No port reservation folder can be created: {string.Join(", ", candidates)}.", failure);
    }

    /// <summary>
    /// Whether nothing listens on the port, on IPv4 and on IPv6. Exclusive, so on Windows a listener on one address
    /// of the port (127.0.0.1, ::1) counts too; a port the system reserves (Hyper-V) fails the bind as well.
    /// </summary>
    private static bool IsFree(int port)
    {
        return CanBind(IPAddress.Any, port) != false && (!Socket.OSSupportsIPv6 || CanBind(IPAddress.IPv6Any, port) != false);
    }

    /// <summary>
    /// <c>true</c> when the port binds, <c>false</c> when it is in use or reserved, <c>null</c> when the address
    /// family cannot be bound at all here (IPv6 switched off), which says nothing about the port.
    /// </summary>
    /// <exception cref="SocketException">Any other failure of an IPv4 bind, which would fail every port the same way.</exception>
    private static bool? CanBind(IPAddress address, int port)
    {
        using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            if (OperatingSystem.IsWindows())
            {
                socket.ExclusiveAddressUse = true;
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socket.DualMode = false;
            }

            socket.Bind(new IPEndPoint(address, port));

            return true;
        }
        catch (SocketException exception) when (exception.SocketErrorCode is SocketError.AddressAlreadyInUse or SocketError.AccessDenied)
        {
            // In use or reserved: the caller tries the next port.
            return false;
        }
        catch (SocketException exception) when (address.AddressFamily == AddressFamily.InterNetworkV6
                                                 && exception.SocketErrorCode is SocketError.AddressNotAvailable or SocketError.AddressFamilyNotSupported)
        {
            // No IPv6 here: only the IPv4 probe decides.
            return null;
        }
    }
}
