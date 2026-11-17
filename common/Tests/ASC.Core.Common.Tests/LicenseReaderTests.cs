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
/// Which license file <see cref="LicenseReader"/> reads and what it does with the staged one (uploaded, not yet
/// accepted, kept beside the license path with the <c>.tmp</c> suffix).
/// </summary>
/// <remarks>
/// The background refresh of the Files worker used to call the same refresh as the accept call, so a staged file was
/// put in force within a period without the user accepting it. It now calls
/// <see cref="LicenseReader.RefreshActiveLicenseAsync"/>, which never touches the staged file.
/// The validation callback refuses every license, so the reader stops before the database and needs none of its
/// database dependencies; the callback records which license it was given. The tests of the installation's own trial
/// read the stored quotas and tariff from stubs instead (<see cref="StubProxy{T}"/>), which fail on a write.
/// </remarks>
public sealed class LicenseReaderTests : IDisposable
{
    private const string ActiveCustomer = "active-customer";
    private const string StagedCustomer = "staged-customer";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "LicenseReaderTests", Guid.NewGuid().ToString("N"));
    private readonly string _licensePath;
    private readonly string _stagedPath;
    private readonly string _backupPath;
    private readonly List<License> _validated = [];

    public LicenseReaderTests()
    {
        Directory.CreateDirectory(_directory);
        _licensePath = Path.Combine(_directory, "license.lic");
        _stagedPath = _licensePath + ".tmp";
        _backupPath = _licensePath + ".bcp";
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    [Fact]
    public async Task RefreshActiveLicenseAsync_ActiveAndStagedFiles_ReadsActiveAndLeavesStaged()
    {
        await File.WriteAllTextAsync(_licensePath, LicenseJson(ActiveCustomer), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(_stagedPath, LicenseJson(StagedCustomer), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<BillingNotConfiguredException>(() => CreateReader().RefreshActiveLicenseAsync(RefuseAsync));

        _validated.Select(r => r.CustomerId).Should().Equal(ActiveCustomer);
        (await ReadCustomerAsync(_licensePath)).Should().Be(ActiveCustomer);
        (await ReadCustomerAsync(_stagedPath)).Should().Be(StagedCustomer);
        File.Exists(_backupPath).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshActiveLicenseAsync_OnlyStagedFile_DoesNotPutItInForce()
    {
        await File.WriteAllTextAsync(_stagedPath, LicenseJson(StagedCustomer), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<BillingNotFoundException>(() => CreateReader().RefreshActiveLicenseAsync(RefuseAsync));

        _validated.Should().BeEmpty();
        File.Exists(_licensePath).Should().BeFalse();
        (await ReadCustomerAsync(_stagedPath)).Should().Be(StagedCustomer);
    }

    [Fact]
    public async Task RefreshLicenseAsync_ActiveAndStagedFiles_ValidatesStagedAndRestoresActiveOnRefusal()
    {
        // The accept call still applies the staged file; a refused one leaves the active license in place.
        await File.WriteAllTextAsync(_licensePath, LicenseJson(ActiveCustomer), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(_stagedPath, LicenseJson(StagedCustomer), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<BillingNotConfiguredException>(() => CreateReader().RefreshLicenseAsync(RefuseAsync));

        _validated.Select(r => r.CustomerId).Should().Equal(StagedCustomer);
        (await ReadCustomerAsync(_licensePath)).Should().Be(ActiveCustomer);
        (await ReadCustomerAsync(_stagedPath)).Should().Be(StagedCustomer);
        File.Exists(_backupPath).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshLicenseAsync_ActiveAndStagedFiles_RestoresActiveOnAnyFailure()
    {
        // Not only a refused license puts the files back: an editing service that cannot be asked or the lock of the
        // license writers fail the same way, and the staged file must not stay on the license path unapplied.
        await File.WriteAllTextAsync(_licensePath, LicenseJson(ActiveCustomer), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(_stagedPath, LicenseJson(StagedCustomer), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateReader().RefreshLicenseAsync(FailAsync));

        (await ReadCustomerAsync(_licensePath)).Should().Be(ActiveCustomer);
        (await ReadCustomerAsync(_stagedPath)).Should().Be(StagedCustomer);
        File.Exists(_backupPath).Should().BeFalse();
    }

    [Fact]
    public async Task DocsTrialToDBAsync_LicenseFileOnDisk_WritesNothing()
    {
        // A license put in force from a file is never replaced by the editing service's trial. The reader has no
        // database here, so reaching it would throw.
        await File.WriteAllTextAsync(_licensePath, LicenseJson(ActiveCustomer), TestContext.Current.CancellationToken);

        var written = await CreateReader().DocsTrialToDBAsync(new DocsTrialLicense(new DateTime(2099, 1, 1)));

        written.Should().BeFalse();
    }

    [Fact]
    public void ChooseTrialDueDate_NoTrialInForce_TakesTheEditingServiceDay()
    {
        LicenseReader.ChooseTrialDueDate(null, new DateTime(2026, 12, 11, 16, 19, 17, DateTimeKind.Utc))
            .Should().Be(new DateTime(2026, 12, 11));
    }

    [Fact]
    public void ChooseTrialDueDate_TrialInForceEndsLater_IsBroughtCloser()
    {
        LicenseReader.ChooseTrialDueDate(new DateTime(2026, 12, 20, 10, 0, 0), new DateTime(2026, 12, 11))
            .Should().Be(new DateTime(2026, 12, 11));
    }

    [Fact]
    public void ChooseTrialDueDate_TrialInForceEndsEarlier_IsKept()
    {
        // An editing service reinstalled with a fresh trial must not extend the portal's trial.
        LicenseReader.ChooseTrialDueDate(new DateTime(2026, 12, 5, 10, 0, 0), new DateTime(2027, 1, 10))
            .Should().Be(new DateTime(2026, 12, 5, 10, 0, 0));
    }

    [Fact]
    public void ChooseTrialDueDate_TrialInForceEndsLaterTheSameDay_IsKept()
    {
        // A clean installation starts its own trial at the time of day it was first read; the editing service's end is
        // a whole day, and cutting the portal's trial to the start of that day would end it hours early.
        LicenseReader.ChooseTrialDueDate(new DateTime(2026, 12, 11, 10, 0, 0), new DateTime(2026, 12, 11))
            .Should().Be(new DateTime(2026, 12, 11, 10, 0, 0));
    }

    [Theory]
    [InlineData(-1, 20, false)]
    [InlineData(10, 20, true)]
    [InlineData(0, 20, true)]
    [InlineData(null, 5, true)]
    [InlineData(null, -1, false)]
    public void WillTrialBeInForce_ChosenEnd_IsInForceThroughItsDay(int? currentDays, int docsDays, bool expected)
    {
        // A trial that already ended is not revived by an editing service reinstalled with a fresh trial: the end is
        // only brought closer, so the wizard must ask for a license file again.
        var now = new DateTime(2026, 11, 17, 15, 0, 0, DateTimeKind.Utc);

        LicenseReader.WillTrialBeInForce(currentDays.HasValue ? now.AddDays(currentDays.Value) : null, now.AddDays(docsDays), now)
            .Should().Be(expected);
    }

    // The ends of the own trial below stay a day or more away from today: the code reads the clock again, and a run
    // across midnight must not move an end to the other side of today.

    [Fact]
    public async Task GetOwnTrialEndAsync_OwnTrialEnded_IsItsStoredEnd()
    {
        // The wizard decides on the end itself (DocsTrialDecision.CompletesOnOwnTrial), so an ended trial still gives its
        // end, not "no trial".
        var end = DateTime.UtcNow.Date.AddDays(-2);
        var reader = CreateReaderOnOwnTrial(end);

        (await reader.GetOwnTrialEndAsync()).Should().Be(end);
    }

    [Fact]
    public async Task GetOwnTrialEndAsync_LicenseFromAFile_IsNone()
    {
        // A license from a file is no trial of the installation, whatever its end.
        var reader = CreateReaderOnOwnTrial(DateTime.UtcNow.Date.AddDays(10), quotaName: LicenseReader.LicenseQuotaName);

        (await reader.GetOwnTrialEndAsync()).Should().BeNull();
    }

    [Fact]
    public async Task WillDocsTrialBeInForceAsync_OwnTrialEnded_IsFalse()
    {
        // What the wizard page and the wizard ask before they skip the license file: a fresh trial of the editing
        // service does not revive the installation's ended trial.
        var reader = CreateReaderOnOwnTrial(DateTime.UtcNow.Date.AddDays(-2));

        (await reader.WillDocsTrialBeInForceAsync(new DocsTrialLicense(DateTime.UtcNow.Date.AddDays(20)))).Should().BeFalse();
    }

    [Fact]
    public async Task WillDocsTrialBeInForceAsync_ReadsTheCachedTariffOnly()
    {
        // The wizard page asks this without authentication: the stored tariff is not read for it (the stub fails such a
        // read), the cached one is.
        var reader = CreateReaderOnOwnTrial(DateTime.UtcNow.Date.AddDays(10), storedTariffReadable: false);

        (await reader.WillDocsTrialBeInForceAsync(new DocsTrialLicense(DateTime.UtcNow.Date.AddDays(20)))).Should().BeTrue();
    }

    [Fact]
    public async Task DocsTrialToDBAsync_OwnTrialEnded_IsNotInForceAndWritesNothing()
    {
        // The end stays where it was, so nothing is written (the stub fails a tariff write) and the trial is not in force.
        var reader = CreateReaderOnOwnTrial(DateTime.UtcNow.Date.AddDays(-2));

        (await reader.DocsTrialToDBAsync(new DocsTrialLicense(DateTime.UtcNow.Date.AddDays(20)))).Should().BeFalse();
    }

    private LicenseReader CreateReader()
    {
        return new LicenseReader(
            tenantManager: null!,
            tariffService: null!,
            coreSettings: null!,
            licenseReaderConfig: new LicenseReaderConfig(CreateConfiguration()),
            distributedLockProvider: new InMemoryLockProvider(),
            logger: NullLogger<LicenseReader>.Instance);
    }

    /// <summary>
    /// A reader of an installation without a license file that runs its own trial ending on
    /// <paramref name="trialDueDate"/>. The stored quotas and tariff are stubs that answer only the reads; a write of
    /// the tariff fails the test.
    /// </summary>
    /// <param name="trialDueDate">The end of the own trial.</param>
    /// <param name="storedTariffReadable">
    /// Whether the stored tariff may be read; otherwise only the cached one answers and a read of the stored one fails.
    /// </param>
    /// <param name="quotaName">The name of the license quota, the own trial's by default.</param>
    private LicenseReader CreateReaderOnOwnTrial(DateTime trialDueDate, bool storedTariffReadable = true, string quotaName = LicenseReader.StartTrialQuotaName)
    {
        var quota = new TenantQuota(LicenseReader.LicenseQuotaId) { Name = quotaName, Trial = true };
        var tariff = new Tariff { Quotas = [new ASC.Core.Billing.Quota(LicenseReader.LicenseQuotaId, 1)], DueDate = trialDueDate, CustomerId = "" };

        var quotaService = StubProxy<IQuotaService>.Create(new()
        {
            [nameof(IQuotaService.GetTenantQuotasAsync)] = _ => Task.FromResult<IEnumerable<TenantQuota>>([quota])
        });
        var answers = new Dictionary<string, Func<object?[]?, object?>>
        {
            // The cached tariff of the installation, as the anonymous question must read it: no request to a payment
            // system and no refresh, either of which would read past the cache
            [nameof(ITariffService.GetTariffAsync)] = args => args is [Tenant.DefaultTenant, false, false]
                ? Task.FromResult(tariff)
                : throw new NotSupportedException($"GetTariffAsync({string.Join(", ", args ?? [])}) reads past the cache")
        };
        if (storedTariffReadable)
        {
            answers[nameof(ITariffService.GetBillingInfoAsync)] = _ => Task.FromResult(tariff);
        }

        var tariffService = StubProxy<ITariffService>.Create(answers);

        return new LicenseReader(
            tenantManager: new TenantManager(null!, quotaService, null!, tariffService, null!, null!, new InMemoryLockProvider()),
            tariffService: tariffService,
            coreSettings: null!,
            licenseReaderConfig: new LicenseReaderConfig(CreateConfiguration()),
            distributedLockProvider: new InMemoryLockProvider(),
            logger: NullLogger<LicenseReader>.Instance);
    }

    private IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["license:file:path"] = _licensePath })
            .Build();
    }

    private Task<LicenseValidationResult> FailAsync(License license)
    {
        _validated.Add(license);
        throw new InvalidOperationException("failed by the test");
    }

    private Task<LicenseValidationResult> RefuseAsync(License license)
    {
        _validated.Add(license);
        return Task.FromResult(new LicenseValidationResult(false, "refused by the test"));
    }

    private static async Task<string> ReadCustomerAsync(string path)
    {
        return License.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).CustomerId;
    }

    private static string LicenseJson(string customerId)
    {
        return $$"""{"customer_id":"{{customerId}}","signature":"signature","start_date":"2020-01-01T00:00:00Z","end_date":"2099-01-01T00:00:00Z"}""";
    }
}
