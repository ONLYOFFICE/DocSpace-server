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

public class ResilienceTests
{
    [Fact]
    public async Task Resilience_RetryTest()
    {
        var token = TestContext.Current.CancellationToken;
        var context = ResilienceContextPool.Shared.Get(token);

        context.Should().NotBeNull();

        ResiliencePropertyKey<Guid> UniquePropKey = new("unique");

        var retryCount = 5;
        var actualRetryCount = 0;

        var builder = new ResiliencePipelineBuilder();

        var pipeline = builder.AddRetry(new RetryStrategyOptions()
        {
            MaxRetryAttempts = retryCount,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Constant,
            ShouldHandle = new PredicateBuilder().Handle<Exception>().HandleResult(result => (bool)result == false),
            OnRetry = args =>
            {
                actualRetryCount++;
                return ValueTask.CompletedTask;
            }
        }).Build();

        var result = false;

        try
        {
            result = await pipeline.ExecuteAsync(async (_) =>
            {
                await Task.Delay(100);

                if (actualRetryCount % 2 == 0)
                {
                    throw new Exception("Simulated failure");
                }

                return false;
            }, token);
        }
        catch(Exception)
        {
        }

        result.Should().Be(false);
        actualRetryCount.Should().Be(retryCount);
    }

    [Fact]
    public async Task Resilience_ContextPoolTest()
    {
        var token = TestContext.Current.CancellationToken;
        var count = 10;
        var bag = new ConcurrentBag<Guid>();

        ResiliencePropertyKey<Guid> UniquePropKey = new("unique");

        await Parallel.ForAsync(0, count, async (i, token) =>
        {
            var context = ResilienceContextPool.Shared.Get(token);
            context.Should().NotBeNull();

            var initialPropValue = context.Properties.GetValue(UniquePropKey, Guid.Empty);
            initialPropValue.Should().Be(Guid.Empty);

            try
            {
                var uniquePropValue = Guid.NewGuid();
                context.Properties.Set(UniquePropKey, uniquePropValue);

                await Task.Delay(100, token);

                var actualPropValue = context.Properties.GetValue(UniquePropKey, Guid.Empty);
                actualPropValue.Should().Be(uniquePropValue);

                bag.Add(uniquePropValue);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        });

        bag.ToList().Distinct().Count().Should().Be(count);
    }
}
