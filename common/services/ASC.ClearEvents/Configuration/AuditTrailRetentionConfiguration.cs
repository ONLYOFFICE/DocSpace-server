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

namespace ASC.ClearEvents.Configuration;

/// <summary>
/// The installation's retention of audit events, from the <see cref="SectionName"/> section. <see cref="Read"/> takes
/// every value through <see cref="ConfigurationValueExtensions"/>: a missing key gives the default, an unreadable one
/// the default and a warning, one out of bounds the nearest bound and a warning, so no value stops the host or leaves
/// the cleanup off without a word in the log.
/// </summary>
public class AuditTrailRetentionConfiguration
{
    public const string SectionName = "clearEvents:auditTrail";

    /// <summary>
    /// The shortest life time a pass accepts: the longest window the audit trail can be set to show
    /// (<see cref="TenantAuditSettings.MaxLifeTime"/>), so that no setting deletes events a portal still reports.
    /// </summary>
    public const int MinLifeTimeDays = TenantAuditSettings.MaxLifeTime;

    /// <summary>
    /// The longest life time a pass accepts, a century. Far beyond any retention policy, and far enough from the
    /// smallest <see cref="DateTime"/> that subtracting it from today cannot fail.
    /// </summary>
    public const int MaxLifeTimeDays = 100 * 365;

    /// <summary>
    /// The longest grace period a pass accepts, a year: long enough for any late renewal, short enough that the grace
    /// period stays a grace period rather than the paid life time of every portal that ever paid.
    /// </summary>
    public const int MaxPaidGracePeriodDays = 365;

    /// <summary>The largest batch a pass accepts: its ids travel to the database as one IN list.</summary>
    public const int MaxBatchSize = 10_000;

    /// <summary>The largest page a pass accepts: the rows of one page are held in memory together.</summary>
    public const int MaxReadPageSize = 100_000;

    /// <summary>The shortest period between passes: a pass walks every portal with old events.</summary>
    public static readonly TimeSpan MinPeriod = TimeSpan.FromHours(1);

    /// <summary>The longest period between passes: the longest delay <see cref="Task.Delay(TimeSpan)"/> accepts.</summary>
    public static readonly TimeSpan MaxPeriod = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    public bool Enabled { get; init; }

    public int PaidLifeTimeDays { get; init; } = 3 * 365;

    public int FreeLifeTimeDays { get; init; } = 365;

    /// <summary>
    /// How long a portal keeps the paid life time after its subscription ended - after the payment delay, which
    /// counts as paid on its own. A customer who renews within it loses none of the events past the free life time.
    /// 0 turns it off; at most <see cref="MaxPaidGracePeriodDays"/>.
    /// </summary>
    public int PaidGracePeriodDays { get; init; } = 7;

    public TimeSpan Period { get; init; } = TimeSpan.FromDays(1);

    /// <summary>How many events a pass deletes at once; a pause follows every batch.</summary>
    public int BatchSize { get; init; } = 1000;

    /// <summary>
    /// How many events a pass reads at once: their keys, dates and history flags, from the (tenant_id, date) index.
    /// Larger than <see cref="BatchSize"/>, because most of what a pass reads is history it keeps and walks past
    /// again on every pass: a page with nothing to delete is followed by one pause, so a tenant with years of
    /// retained history is walked in this many fewer pauses. The deletes still go a batch at a time.
    /// </summary>
    public int ReadPageSize { get; init; } = 10_000;

    /// <summary>
    /// Whether a pass may run with these settings. <see cref="Read"/> always returns usable settings; this guards the
    /// ones built in code. A life time of a few days would delete almost the whole audit trail of every portal, a free
    /// life time above the paid one would keep the events of free portals longer, and values past the upper bounds
    /// would fail every pass or run them back to back.
    /// </summary>
    public bool IsUsable =>
        FreeLifeTimeDays >= MinLifeTimeDays &&
        PaidLifeTimeDays >= FreeLifeTimeDays &&
        PaidLifeTimeDays <= MaxLifeTimeDays &&
        PaidGracePeriodDays is >= 0 and <= MaxPaidGracePeriodDays &&
        BatchSize is > 0 and <= MaxBatchSize &&
        ReadPageSize is > 0 and <= MaxReadPageSize &&
        Period >= MinPeriod && Period <= MaxPeriod;

    /// <summary>
    /// Reads the <see cref="SectionName"/> section, once, when the service is created. Every value goes through
    /// <see cref="ConfigurationValueExtensions"/>, so the settings that come back are always usable:
    /// <list type="bullet">
    /// <item><c>enabled</c>: <c>true</c>/<c>false</c>, <c>1</c>/<c>0</c>, <c>yes</c>/<c>no</c>, <c>on</c>/<c>off</c>;
    /// anything else is off, with a warning.</item>
    /// <item><c>paidLifeTimeDays</c> (1095) and <c>freeLifeTimeDays</c> (365): days, from <see cref="MinLifeTimeDays"/>
    /// to <see cref="MaxLifeTimeDays"/>. A free life time above the paid one gets a warning, and the paid one is raised
    /// to it: the cleanup deletes, so of two conflicting values it keeps the longer.</item>
    /// <item><c>paidGracePeriodDays</c> (7): days, from 0 (off) to <see cref="MaxPaidGracePeriodDays"/>.</item>
    /// <item><c>period</c> (<c>"1.00:00:00"</c>, one day): the time between passes, written <c>d.hh:mm:ss</c>, from
    /// <see cref="MinPeriod"/> to <see cref="MaxPeriod"/>. <c>"24:00:00"</c> reads as 24 days and <c>"7"</c> as 7 days:
    /// both are kept as read, with a warning.</item>
    /// <item><c>batchSize</c> (1000): events deleted at once, from 1 to <see cref="MaxBatchSize"/>.</item>
    /// <item><c>readPageSize</c> (10000): events read at once, from 1 to <see cref="MaxReadPageSize"/>.</item>
    /// </list>
    /// </summary>
    public static AuditTrailRetentionConfiguration Read(IConfiguration configuration, ILogger logger)
    {
        var defaults = new AuditTrailRetentionConfiguration();

        var paidLifeTimeDays = configuration.ReadInt(Key(nameof(PaidLifeTimeDays)), defaults.PaidLifeTimeDays, MinLifeTimeDays, MaxLifeTimeDays, logger);
        var freeLifeTimeDays = configuration.ReadInt(Key(nameof(FreeLifeTimeDays)), defaults.FreeLifeTimeDays, MinLifeTimeDays, MaxLifeTimeDays, logger);

        if (freeLifeTimeDays > paidLifeTimeDays)
        {
            logger.WarningFreeLifeTimeAbovePaid(SectionName, freeLifeTimeDays, paidLifeTimeDays);

            paidLifeTimeDays = freeLifeTimeDays;
        }

        return new AuditTrailRetentionConfiguration
        {
            Enabled = configuration.ReadBool(Key(nameof(Enabled)), defaults.Enabled, logger),
            PaidLifeTimeDays = paidLifeTimeDays,
            FreeLifeTimeDays = freeLifeTimeDays,
            PaidGracePeriodDays = configuration.ReadInt(Key(nameof(PaidGracePeriodDays)), defaults.PaidGracePeriodDays, 0, MaxPaidGracePeriodDays, logger),
            Period = configuration.ReadInterval(Key(nameof(Period)), defaults.Period, MinPeriod, MaxPeriod, logger),
            BatchSize = configuration.ReadInt(Key(nameof(BatchSize)), defaults.BatchSize, 1, MaxBatchSize, logger),
            ReadPageSize = configuration.ReadInt(Key(nameof(ReadPageSize)), defaults.ReadPageSize, 1, MaxReadPageSize, logger)
        };
    }

    /// <summary>
    /// Whether the cleanup has to be registered, decided at service registration, where there is no logger yet:
    /// <c>enabled</c> is set to anything but an off value (<c>false</c>, <c>0</c>, <c>no</c>, <c>off</c>). An
    /// unreadable value registers it too, so that the service, which reads the section with a logger, says why the
    /// cleanup stays off instead of leaving no trace at all.
    /// </summary>
    public static bool IsRequested(IConfiguration configuration)
    {
        var key = Key(nameof(Enabled));

        // The spellings are the helper's: an off value reads as false, an unreadable one falls to the default of
        // true, without a warning here (the service writes it, with a logger).
        return !string.IsNullOrWhiteSpace(configuration[key]) && configuration.ReadBool(key, true, NullLogger.Instance);
    }

    private static string Key(string name)
    {
        return $"{SectionName}:{name}";
    }
}
