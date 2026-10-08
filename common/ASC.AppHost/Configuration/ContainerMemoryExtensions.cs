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
/// Memory sizing for the containers the AppHost starts. Without it nothing bounds them: the JVMs
/// size their heaps from the whole Docker VM (25 % each for Identity), Server GC spreads one heap
/// per host core over every .NET service, and the idle graph measured 8.7 GiB on 2026-10-08 -
/// more than the 8 GB Docker Desktop allows on a Mac by default. The limits below are cgroup caps
/// (`--memory`), which also make .NET and the JVM derive their heap budgets from the cap instead
/// of the VM. Each value is roughly 2-3x what the container needs when idle, so it bounds growth
/// without getting in the way of normal development work.
/// </summary>
public static class ContainerMemoryExtensions
{
    /// <summary>A .NET service on Workstation GC idles at 150-300 MiB; this leaves room for uploads and conversions.</summary>
    public const string DotNetServiceLimit = "768m";

    /// <summary>One Identity JVM, 492 MiB measured idle with <see cref="IdentityJavaOptions"/>.</summary>
    public const string IdentityLimit = "768m";

    /// <summary>OpenSearch: 512 MiB heap plus Lucene off-heap and the ingest-attachment (Tika) parsers.</summary>
    public const string OpensearchLimit = "1536m";

    /// <summary>MySQL with performance_schema off idles at ~210 MiB.</summary>
    public const string MySqlLimit = "768m";

    /// <summary>
    /// Document Server: ~920 MiB anonymous memory measured with the bundled broker and database, ~720 without
    /// them (AddEditors moves both to the AppHost containers); the rest of its 1.5 GiB was the page cache of
    /// web-apps, sdkjs and fonts, which the cap lets the kernel reclaim.
    /// </summary>
    public const string EditorsLimit = "1280m";

    /// <summary>
    /// Measured on identity-authorization: 706 -> 492 MiB idle, health UP. Serial GC and a small
    /// thread stack fit a dev instance that serves one developer; the default ergonomics would give
    /// each JVM a 2 GB max heap on an 8 GB Docker VM.
    /// </summary>
    public const string IdentityJavaOptions = "-Xmx384m -XX:MaxMetaspaceSize=160m -XX:+UseSerialGC -Xss512k";

    /// <summary>
    /// Measured: 1722 -> 1011 MiB idle against the image default of 1g. Safe only together with the
    /// smaller indexer bulk portion (<see cref="IndexerMaxContentLength"/>): the content pipeline
    /// parses every attached file inside this heap, so a 100 MB bulk of base64 documents would trip
    /// the circuit breaker. Production keeps its own 4g in buildtools/install/docker/opensearch.yml.
    /// </summary>
    public const string OpensearchJavaOptions = "-Xms512m -Xmx512m";

    /// <summary>
    /// `elastic:MaxContentLength` for the services: the amount of base64 document data BaseIndexer
    /// packs into one bulk request before sending it. The default is 100 MB; 16 MB keeps a bulk to
    /// one or two files (`elastic:MaxFileSize` is 10 MB), which the 512 MiB OpenSearch heap parses
    /// comfortably.
    /// </summary>
    public const long IndexerMaxContentLength = 16 * 1024 * 1024L;

    /// <summary>
    /// Server GC was measured at 299 MiB idle for ASC.People against 214 MiB on Workstation GC, and
    /// 202 MiB with the conserve-memory hint; on a single developer machine the per-core heaps of
    /// Server GC buy nothing. Only the Docker-mode containers are affected - the csproj defaults stay.
    /// </summary>
    public static IResourceBuilder<T> WithWorkstationGc<T>(this IResourceBuilder<T> builder) where T : IResourceWithEnvironment
    {
        return builder
            .WithEnvironment("DOTNET_gcServer", "0")
            .WithEnvironment("DOTNET_GCConserveMemory", "5");
    }

    public static IResourceBuilder<T> WithMemoryLimit<T>(this IResourceBuilder<T> builder, string limit) where T : ContainerResource
    {
        return builder.WithContainerRuntimeArgs("--memory", limit);
    }
}
