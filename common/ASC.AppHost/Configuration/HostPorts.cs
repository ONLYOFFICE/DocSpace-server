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
/// A development run keeps the fixed ports. A test run takes free ones instead (<see cref="UseFreePorts"/>), so two
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

    // Per user on Unix, where the temp folder is shared and a folder another user created cannot be written; the temp
    // folder of a Windows user is that user's already.
    private static readonly string _reservationDirectory = Path.Combine(Path.GetTempPath(),
        OperatingSystem.IsWindows() ? "docspace-test-ports" : $"docspace-test-ports-{Environment.UserName}");

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
    /// starts anything, and by the AppHost for its test profiles. A second call changes nothing.
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
    /// <exception cref="InvalidOperationException">The reservation folder cannot be written, which no port would change.</exception>
    public static bool TryReserve(int port)
    {
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(_reservationDirectory);

                _reservations.Add(new FileStream(GetReservationPath(port), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                    bufferSize: 1, OperatingSystem.IsWindows() ? FileOptions.DeleteOnClose : FileOptions.None));

                return true;
            }
            catch (IOException exception) when (exception is not (DirectoryNotFoundException or PathTooLongException or DriveNotFoundException))
            {
                // Locked by its holder: a sharing violation on Windows, a failed lock on Unix.
                return false;
            }
            catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
            {
                // Deleted by its holder on close a moment ago: Windows refuses the open while the delete is pending.
                return false;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"The port reservations cannot be written to {_reservationDirectory}.", exception);
            }
        }
    }

    /// <summary>The lock file a run holds open while <paramref name="port"/> is reserved for it.</summary>
    public static string GetReservationPath(int port)
    {
        return Path.Combine(_reservationDirectory, port.ToString(CultureInfo.InvariantCulture) + ".lock");
    }

    private static int PickFreePort()
    {
        const int count = LastFreePort - FirstFreePort + 1;
        var start = Random.Shared.Next(count);

        // Ends by count: every port of the range is tried once.
        for (var i = 0; i < count; i++)
        {
            var port = FirstFreePort + (start + i) % count;

            if (!_taken.Contains(port) && IsFree(port) && TryReserve(port))
            {
                return port;
            }
        }

        throw new InvalidOperationException(
            $"No free port between {FirstFreePort} and {LastFreePort}: each one was in use, or its reservation in {_reservationDirectory} could not be taken.");
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
