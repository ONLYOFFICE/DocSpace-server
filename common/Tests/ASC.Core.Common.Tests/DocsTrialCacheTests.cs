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
/// How the answer of the editing service about its own trial is kept in the memory cache: what a cached reader gets
/// while the service fails, and what a fresh question does to the cached answer.
/// </summary>
/// <remarks>
/// A real in-memory FusionCache with no backplane; the request to the editing service is a function that answers from
/// a queue and counts the calls.
/// </remarks>
public sealed class DocsTrialCacheTests : IDisposable
{
    private static readonly DocsTrialCheck _active = new(DocsTrialState.Active, new DocsTrialLicense(new DateTime(2026, 12, 11, 0, 0, 0, DateTimeKind.Utc)));
    private static readonly DocsTrialCheck _expired = new(DocsTrialState.Expired, new DocsTrialLicense(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc)));

    private readonly FusionCache _cache = new(new FusionCacheOptions());
    private readonly Queue<DocsTrialCheck> _answers = new();
    private int _requests;

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Fact]
    public async Task GetAsync_FreshQuestionUnknown_KeepsTheLastAnswerKnown()
    {
        // A failure seen by the setup wizard or the background refresh must not remove the answer the anonymous
        // question of the wizard page relies on.
        var trialCache = CreateCache(DocsTrialCacheOptions.Default);
        _answers.Enqueue(_active);
        _answers.Enqueue(DocsTrialCheck.Unknown);

        (await trialCache.GetAsync(RequestAsync, useCache: true)).Should().Be(_active);
        (await trialCache.GetAsync(RequestAsync, useCache: false)).Should().Be(DocsTrialCheck.Unknown);

        (await trialCache.GetAsync(RequestAsync, useCache: true)).Should().Be(_active);
        _requests.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_FreshQuestionAnswered_ReplacesTheCachedAnswer()
    {
        var trialCache = CreateCache(DocsTrialCacheOptions.Default);
        _answers.Enqueue(_active);
        _answers.Enqueue(_expired);

        (await trialCache.GetAsync(RequestAsync, useCache: true)).Should().Be(_active);
        (await trialCache.GetAsync(RequestAsync, useCache: false)).Should().Be(_expired);

        (await trialCache.GetAsync(RequestAsync, useCache: true)).Should().Be(_expired);
        _requests.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_AnswerExpiredAndServiceFails_UsesTheLastAnswerKnown()
    {
        var trialCache = CreateCache(new DocsTrialCacheOptions(
            Duration: TimeSpan.FromMilliseconds(100),
            FailSafeMaxDuration: TimeSpan.FromHours(1),
            FailureDuration: TimeSpan.FromMinutes(1),
            HardTimeout: TimeSpan.FromSeconds(10)));
        _answers.Enqueue(_active);
        _answers.Enqueue(DocsTrialCheck.Unknown);

        (await trialCache.GetAsync(RequestAsync, useCache: true)).Should().Be(_active);

        // Let the answer expire; it is a duration running out, not an asynchronous write to wait for
        await Task.Delay(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);

        (await trialCache.GetAsync(RequestAsync, useCache: true)).Should().Be(_active);
        _requests.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_NoAnswerKnownAndServiceFails_KeepsTheFailureForAWhile()
    {
        var trialCache = CreateCache(DocsTrialCacheOptions.Default);
        _answers.Enqueue(DocsTrialCheck.Unknown);

        (await trialCache.GetAsync(RequestAsync, useCache: true)).Should().Be(DocsTrialCheck.Unknown);
        (await trialCache.GetAsync(RequestAsync, useCache: true)).Should().Be(DocsTrialCheck.Unknown);

        _requests.Should().Be(1);
    }

    private DocsTrialCache CreateCache(DocsTrialCacheOptions options)
    {
        return new DocsTrialCache(_cache, NullLogger<DocumentServiceLicense>.Instance, options);
    }

    private Task<DocsTrialCheck> RequestAsync()
    {
        _requests++;
        return Task.FromResult(_answers.Dequeue());
    }
}
