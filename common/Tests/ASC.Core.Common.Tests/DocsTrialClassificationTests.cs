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
/// How the answer of the editing service to the license command is read as the state of its own trial, the state it
/// runs in while it has no license file.
/// </summary>
/// <remarks>
/// The answers are the ones an editing service 10.0.1 gave: a fresh trial, the same trial after a restart (the end
/// moves by seconds), and a license file it could not accept (no <c>trial</c> flag, result <c>ExpiredTrial</c>). They
/// are read through <see cref="DocumentService.ReadCommandResponse"/>, the reader the command request uses.
/// </remarks>
public sealed class DocsTrialClassificationTests
{
    private const string FreshTrial = """
        {"error":0,"license":{"start_date":"2026-11-01T00:00:00.000Z","end_date":"2026-12-11T16:19:17.013Z","timelimited":false,"trial":true,"developer":true,"branding":false,"customization":true,"advanced_api":true,"connections":20,"connections_view":20,"users_count":0,"users_view_count":0,"users_expire":1,"customer_id":"","alias":"","multitenancy":false,"grace_days":30},"server":{"workersCount":999999,"resultType":3,"packageType":2,"buildDate":"2026-10-23T00:00:00.000Z","buildVersion":"10.0.1","buildNumber":13},"quota":{"users":[],"users_view":[]}}
        """;

    private const string RestartedTrial = """
        {"error":0,"license":{"start_date":"2026-11-01T00:00:00.000Z","end_date":"2026-12-11T16:19:48.185Z","timelimited":false,"trial":true,"developer":true,"branding":false,"customization":true,"advanced_api":true,"connections":20,"connections_view":20,"users_count":0,"users_view_count":0,"users_expire":1,"customer_id":"","alias":"","multitenancy":false,"grace_days":30},"server":{"workersCount":999999,"resultType":3,"packageType":2,"buildDate":"2026-10-23T00:00:00.000Z","buildVersion":"10.0.1","buildNumber":13},"quota":{"users":[],"users_view":[]}}
        """;

    private const string UnacceptedLicenseFile = """
        {"error":0,"license":{"customer_id":"x","end_date":"2099-01-01"},"server":{"workersCount":1,"resultType":6,"packageType":2,"buildDate":"2026-10-23T00:00:00.000Z","buildVersion":"10.0.1","buildNumber":13},"quota":{"users":[],"users_view":[]}}
        """;

    private const string ExpiredTrial = """
        {"error":0,"license":{"start_date":"2026-10-01T00:00:00.000Z","end_date":"2026-10-31T10:00:00.000Z","timelimited":false,"trial":true,"branding":false,"customization":true,"advanced_api":true,"customer_id":""},"server":{"workersCount":1,"resultType":6,"packageType":1,"buildDate":"2026-10-23T00:00:00.000Z","buildVersion":"10.0.1","buildNumber":13},"quota":{"users":[],"users_view":[]}}
        """;

    private const string PaidLicense = """
        {"error":0,"license":{"start_date":"2026-01-01T00:00:00.000Z","end_date":"2027-01-01T00:00:00.000Z","timelimited":false,"trial":false,"branding":true,"customization":true,"advanced_api":true,"customer_id":"customer"},"server":{"workersCount":4,"resultType":3,"packageType":1,"buildDate":"2026-10-23T00:00:00.000Z","buildVersion":"10.0.1","buildNumber":13},"quota":{"users":[],"users_view":[]}}
        """;

    private static readonly DateTime _now = new(2026, 11, 11, 16, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void ClassifyTrial_FreshTrial_IsActiveUntilTheDayOfItsEnd()
    {
        var check = Classify(FreshTrial);

        check.State.Should().Be(DocsTrialState.Active);
        check.Trial.Should().Be(new DocsTrialLicense(new DateTime(2026, 12, 11, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void ClassifyTrial_SameTrialAfterRestart_GivesTheSameTrial()
    {
        // The service moves the end by seconds when it restarts; a trial kept to the second would make the background
        // refresh rewrite the tariff on every pass.
        Classify(RestartedTrial).Should().Be(Classify(FreshTrial));
    }

    [Fact]
    public void ClassifyTrial_LicenseFileTheServiceRefuses_IsNotATrial()
    {
        // The service answers ExpiredTrial here as well, but without the trial flag: a refused file is no trial.
        Classify(UnacceptedLicenseFile).Should().Be(DocsTrialCheck.NotTrial);
    }

    [Fact]
    public void ClassifyTrial_ExpiredTrial_EndsBeforeToday()
    {
        var check = Classify(ExpiredTrial);

        check.State.Should().Be(DocsTrialState.Expired);
        check.Trial!.DueDate.Should().Be(new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ClassifyTrial_ExpiredTrialEndingInTheFuture_EndsYesterday()
    {
        // The service's verdict wins over its date: an expired trial must leave the portal unpaid today.
        var check = Classify(ExpiredTrial.Replace("2026-10-31T10:00:00.000Z", "2026-12-31T10:00:00.000Z"));

        check.State.Should().Be(DocsTrialState.Expired);
        check.Trial!.DueDate.Should().Be(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("2026-12-11T00:30:00")]
    [InlineData("2026-12-11T23:30:00")]
    public void ClassifyTrial_EndWithoutZone_IsReadAsUtc(string end)
    {
        // A date without a zone read as local time moves to the day before east of UTC (00:30) and to the day after
        // west of it (23:30); on a machine whose time zone is UTC neither case can fail.
        var check = Classify(FreshTrial.Replace("2026-12-11T16:19:17.013Z", end));

        check.State.Should().Be(DocsTrialState.Active);
        check.Trial!.DueDate.Should().Be(new DateTime(2026, 12, 11, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ClassifyTrial_PaidLicense_IsNotATrial()
    {
        Classify(PaidLicense).Should().Be(DocsTrialCheck.NotTrial);
    }

    [Theory]
    [InlineData("<html><body>502 Bad Gateway</body></html>")]
    [InlineData("""{"error":6}""")]
    [InlineData("""{"error":0,"license":{"trial":true,"end_date":null},"server":{"resultType":3}}""")]
    [InlineData("""{"error":0,"license":{"trial":true},"server":{"resultType":3}}""")]
    [InlineData("""{"error":0,"license":{"trial":true,"end_date":"2026-12-11T00:00:00Z"}}""")]
    [InlineData("""{"error":0,"license":{"trial":true,"end_date":"2026-12-11T00:00:00Z"},"server":{"resultType":1}}""")]
    public void ClassifyTrial_AnswerThatSaysNothingAboutATrial_IsUnknown(string answer)
    {
        // Unknown never reads as an expired trial: the caller keeps what it had.
        Classify(answer).Should().Be(DocsTrialCheck.Unknown);
    }

    private static DocsTrialCheck Classify(string answer)
    {
        return DocumentServiceLicense.ClassifyTrial(DocumentService.ReadCommandResponse(answer), _now);
    }
}
