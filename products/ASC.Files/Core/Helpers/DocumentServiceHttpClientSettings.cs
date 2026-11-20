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

/// <summary>
/// The resilience settings of the document service clients: how long one attempt may take and how many times a failed
/// request is retried. Read once, when the first of those clients is built, so that a value that does not parse or is
/// out of bounds gets a warning in the log instead of stopping the host.
/// </summary>
public sealed class DocumentServiceHttpClientSettings
{
    public const string TimeoutKey = "files:docservice:timeout";
    public const string TryKey = "files:docservice:try";

    public const int DefaultTimeoutMilliseconds = 100_000;
    public const int MinTimeoutMilliseconds = 1_000;
    public const int MaxTimeoutMilliseconds = 100_000;

    public const int DefaultRetryCount = 6;
    public const int MinRetryCount = 1;
    public const int MaxRetryCount = 20;

    public DocumentServiceHttpClientSettings(IConfiguration configuration, ILogger<DocumentServiceHttpClientSettings> logger)
    {
        // Time one attempt may take, in milliseconds: 1000 to 100000, e.g. 30000. The clients keep the default
        // HttpClient.Timeout of 100 seconds, which bounds all attempts of a request together, so a longer attempt
        // could never end on its own timeout.
        AttemptTimeout = TimeSpan.FromMilliseconds(configuration.ReadInt(TimeoutKey, DefaultTimeoutMilliseconds, MinTimeoutMilliseconds, MaxTimeoutMilliseconds, logger));

        // Retries after the first attempt: 1 to 20, e.g. 3. The retry strategy refuses 0.
        RetryCount = configuration.ReadInt(TryKey, DefaultRetryCount, MinRetryCount, MaxRetryCount, logger);

        var delays = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: RetryCount).ToArray();

        RetryOptions = new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = RetryCount,

            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TaskCanceledException>()
                            .Handle<TimeoutRejectedException>()
                            .HandleResult(response => !response.IsSuccessStatusCode),

            DelayGenerator = args => ValueTask.FromResult<TimeSpan?>(delays[args.AttemptNumber])
        };
    }

    /// <summary>
    /// How long one attempt may take before it is cancelled and retried.
    /// </summary>
    public TimeSpan AttemptTimeout { get; }

    /// <summary>
    /// How many times a failed request is retried after the first attempt.
    /// </summary>
    public int RetryCount { get; }

    /// <summary>
    /// The retry strategy of the clients' resilience pipeline, built from <see cref="RetryCount"/>.
    /// </summary>
    public RetryStrategyOptions<HttpResponseMessage> RetryOptions { get; }
}
