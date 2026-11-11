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

namespace ASC.Files.Core.Helpers;

[Scope]
public class DocumentServiceLicense(ICache cache,
    ResiliencePipelineProvider<string> resiliencePipelineProvider,
    CoreBaseSettings coreBaseSettings,
    FilesLinkUtility filesLinkUtility,
    IHttpClientFactory clientFactory,
    ILogger<DocumentServiceLicense> logger)
{
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
    private const string CacheKey = "DocumentServiceLicense";

    private async Task<CommandResponse> GetDocumentServiceLicenseAsync(bool useCache)
    {
        if (!coreBaseSettings.Standalone)
        {
            return null;
        }

        if (string.IsNullOrEmpty(filesLinkUtility.DocServiceCommandUrl))
        {
            return null;
        }

        var commandResponse = useCache ? cache.Get<CommandResponse>(CacheKey) : null;
        if (commandResponse == null)
        {
            commandResponse = await CommandRequestAsync(
                   filesLinkUtility.DocServiceCommandUrl,
                   CommandMethod.License,
                   null,
                   null,
                   null,
                   null,
                   filesLinkUtility.DocServiceSignatureSecret,
                   filesLinkUtility.DocServiceSignatureHeader,
                   await filesLinkUtility.GetDocServiceSslVerificationAsync(),
                   clientFactory
                   );

            // A failed answer is not kept: the editing service often starts later than the portal, and a cached
            // failure would hide its license for the whole cache period
            if (useCache && commandResponse is { Error: ErrorTypes.NoError })
            {
                cache.Insert(CacheKey, commandResponse, DateTime.UtcNow.Add(_cacheExpiration));
            }
        }

        return commandResponse;
    }

    public async Task<LicenseValidationResult> ValidateLicense(License license)
    {
        ArgumentNullException.ThrowIfNull(license);

        var pipeline = resiliencePipelineProvider.GetPipeline<LicenseValidationResult>(LicenseResiliencePipelineName);

        var errorMsg = string.Empty;

        var response = await pipeline.ExecuteAsync(async _ =>
        {
            var commandResponse = await GetDocumentServiceLicenseAsync(false);

            if (commandResponse == null)
            {
                return new LicenseValidationResult(true, null);
            }

            if (commandResponse.Error == ErrorTypes.ParseError)
            {
                errorMsg = commandResponse.ErrorString;
                return null; // Possible DocumentService behavior without a license when the response contains "end_date":null
            }

            if (commandResponse.Error != ErrorTypes.NoError)
            {
                return new LicenseValidationResult(false, commandResponse.ErrorString);
            }

            // A response without the resource key is not a match: Document Server re-reads the license file
            // with a delay, so right after the file is written it may still answer as if there were no license
            if (!string.IsNullOrEmpty(license.ResourceKey) &&
                commandResponse.License?.ResourceKey != license.ResourceKey)
            {
                errorMsg = $"Resource key mismatch: expected {license.ResourceKey}, got {commandResponse.License?.ResourceKey ?? "none"}";
                return null; // Document Server has not re-read the license file yet
            }

            if (commandResponse.License != null &&
                !string.IsNullOrEmpty(commandResponse.License.CustomerId) &&
                !string.IsNullOrEmpty(license.CustomerId) &&
                commandResponse.License.CustomerId != license.CustomerId)
            {
                errorMsg = $"Customer id mismatch: expected {license.CustomerId}, got {commandResponse.License.CustomerId}";
                return null; // Customer id mismatch. Possibly cached result
            }

            if (commandResponse.Server == null)
            {
                return new LicenseValidationResult(false, "Server is null");
            }

            return commandResponse.Server.ResultType is CommandResponse.ServerInfo.ResultTypes.Success or CommandResponse.ServerInfo.ResultTypes.SuccessLimit
                ? new LicenseValidationResult(true, null)
                : new LicenseValidationResult(false, $"ResultType is {commandResponse.Server.ResultType}");
        });

        return response ?? new LicenseValidationResult(false, $"Failure after several attempts. {errorMsg}");
    }

    public async Task<(Dictionary<string, DateTime>, License)> GetLicenseQuotaAsync(bool useCache = true)
    {
        var commandResponse = await GetDocumentServiceLicenseAsync(useCache);
        return commandResponse == null ?
            (null, null) :
            (commandResponse.Quota?.Users?.ToDictionary(r => r.UserId, r => r.Expire), commandResponse.License);
    }

    /// <summary>
    /// Asks the editing service whether it runs its own trial, the state it is in while it has no license file.
    /// </summary>
    /// <param name="useCache">Whether a successful answer of the last 15 minutes may be reused.</param>
    /// <returns>
    /// <see cref="DocsTrialState.NotTrial"/> on an installation that has no editing service to ask;
    /// <see cref="DocsTrialState.Unknown"/> when the service cannot be reached or its answer cannot be read.
    /// </returns>
    public async Task<DocsTrialCheck> GetTrialAsync(bool useCache)
    {
        try
        {
            var commandResponse = await GetDocumentServiceLicenseAsync(useCache);

            return commandResponse == null
                ? DocsTrialCheck.NotTrial
                : ClassifyTrial(commandResponse, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.WarningDocsTrialRequest(ex);
            return DocsTrialCheck.Unknown;
        }
    }

    /// <summary>
    /// Reads the trial state from the editing service's answer to the license command.
    /// </summary>
    /// <remarks>
    /// Only an answer that says <c>trial</c> describes a trial: the service also answers <c>ExpiredTrial</c> for a
    /// license file it cannot accept, with the fields of that file and no <c>trial</c> flag. An answer that cannot be
    /// read is <see cref="DocsTrialState.Unknown"/>, never an expired trial. The end of the trial is kept as a UTC
    /// date, because the service moves it by seconds when it restarts. An expired trial always ends before today, so
    /// the portal's tariff becomes unpaid at once.
    /// </remarks>
    public static DocsTrialCheck ClassifyTrial(CommandResponse commandResponse, DateTime utcNow)
    {
        if (commandResponse is not { Error: ErrorTypes.NoError })
        {
            return DocsTrialCheck.Unknown;
        }

        if (commandResponse.License is not { Trial: true } license)
        {
            return DocsTrialCheck.NotTrial;
        }

        if (commandResponse.Server == null || license.DueDate == default)
        {
            return DocsTrialCheck.Unknown;
        }

        var dueDate = DateTime.SpecifyKind(license.DueDate.ToUniversalTime().Date, DateTimeKind.Utc);

        switch (commandResponse.Server.ResultType)
        {
            case CommandResponse.ServerInfo.ResultTypes.Success:
            case CommandResponse.ServerInfo.ResultTypes.SuccessLimit:
            case CommandResponse.ServerInfo.ResultTypes.Connections:
            case CommandResponse.ServerInfo.ResultTypes.UsersCount:
                return new DocsTrialCheck(DocsTrialState.Active, new DocsTrialLicense(dueDate));

            case CommandResponse.ServerInfo.ResultTypes.ExpiredTrial:
            case CommandResponse.ServerInfo.ResultTypes.Expired:
            case CommandResponse.ServerInfo.ResultTypes.ExpiredLimited:
                var yesterday = utcNow.Date.AddDays(-1);
                return new DocsTrialCheck(DocsTrialState.Expired, new DocsTrialLicense(dueDate < yesterday ? dueDate : yesterday));

            default:
                return DocsTrialCheck.Unknown;
        }
    }
}

/// <summary>
/// The state of the editing service's own trial.
/// </summary>
public enum DocsTrialState
{
    /// <summary>The service could not be asked, or its answer cannot be read.</summary>
    Unknown,

    /// <summary>The service runs under a license file, or there is no service to ask.</summary>
    NotTrial,

    /// <summary>The service runs its trial.</summary>
    Active,

    /// <summary>The service's trial has ended.</summary>
    Expired
}

/// <summary>
/// The state of the editing service's own trial, with the trial when the state is <see cref="DocsTrialState.Active"/>
/// or <see cref="DocsTrialState.Expired"/>.
/// </summary>
public record DocsTrialCheck(DocsTrialState State, DocsTrialLicense Trial)
{
    public static readonly DocsTrialCheck Unknown = new(DocsTrialState.Unknown, null);

    public static readonly DocsTrialCheck NotTrial = new(DocsTrialState.NotTrial, null);
}
