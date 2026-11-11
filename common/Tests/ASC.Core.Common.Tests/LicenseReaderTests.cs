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
/// database dependencies; the callback records which license it was given.
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
    public async Task DocsTrialToDBAsync_LicenseFileOnDisk_WritesNothing()
    {
        // A license put in force from a file is never replaced by the editing service's trial. The reader has no
        // database here, so reaching it would throw.
        await File.WriteAllTextAsync(_licensePath, LicenseJson(ActiveCustomer), TestContext.Current.CancellationToken);

        var written = await CreateReader().DocsTrialToDBAsync(new DocsTrialLicense(new DateTime(2099, 1, 1)));

        written.Should().BeFalse();
    }

    private LicenseReader CreateReader()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["license:file:path"] = _licensePath })
            .Build();

        return new LicenseReader(
            tenantManager: null!,
            tariffService: null!,
            coreSettings: null!,
            licenseReaderConfig: new LicenseReaderConfig(configuration),
            logger: NullLogger<LicenseReader>.Instance);
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
