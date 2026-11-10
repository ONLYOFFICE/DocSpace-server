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

namespace ASC.Data.Stress.Core;

/// <summary>
/// Memory of the machine and of the DocSpace service processes at one moment. Locally the services run
/// as separate processes of the Aspire AppHost, so besides the machine-wide numbers of the original script
/// (/proc/meminfo) each watched service gets its own working set and private bytes.
/// </summary>
public sealed record MemorySample(long? FreeMb, long? AvailableMb, IReadOnlyList<ProcessMemory> Processes);

/// <summary>Sum over every process with that name (a service restarted by Aspire shows up under the same name).</summary>
public sealed record ProcessMemory(string Name, int Count, long WorkingSetMb, long PrivateMb);

public static class MemorySampler
{
    private const long Mb = 1024 * 1024;

    public static MemorySample Take(IReadOnlyList<string> processNames)
    {
        var (free, available) = GetMachineMemory();

        return new MemorySample(free, available, processNames.Select(GetProcessMemory).ToList());
    }

    private static ProcessMemory GetProcessMemory(string name)
    {
        var processes = Process.GetProcessesByName(name);
        long workingSet = 0, privateBytes = 0;

        foreach (var process in processes)
        {
            using (process)
            {
                try
                {
                    workingSet += process.WorkingSet64;
                    privateBytes += process.PrivateMemorySize64;
                }
                catch (InvalidOperationException)
                {
                    // the process exited between the lookup and the read
                }
            }
        }

        return new ProcessMemory(name, processes.Length, workingSet / Mb, privateBytes / Mb);
    }

    private static (long? Free, long? Available) GetMachineMemory()
    {
        if (OperatingSystem.IsWindows())
        {
            var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };

            // Windows has no MemFree of its own: AvailPhys already counts the standby list, as MemAvailable does
            return GlobalMemoryStatusEx(ref status) ? (null, (long)(status.AvailPhys / Mb)) : (null, null);
        }

        if (OperatingSystem.IsLinux() && File.Exists("/proc/meminfo"))
        {
            long? free = null, available = null;

            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (line.StartsWith("MemFree:", StringComparison.Ordinal))
                {
                    free = ParseKb(line) / 1024;
                }
                else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                {
                    available = ParseKb(line) / 1024;
                }
            }

            return (free, available);
        }

        return (null, null);
    }

    private static long ParseKb(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length > 1 && long.TryParse(parts[1], out var kb) ? kb : 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
