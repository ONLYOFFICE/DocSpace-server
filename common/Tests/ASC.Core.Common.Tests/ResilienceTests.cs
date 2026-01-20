// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Collections.Concurrent;

using FluentAssertions;

using Polly;
using Polly.Retry;

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
