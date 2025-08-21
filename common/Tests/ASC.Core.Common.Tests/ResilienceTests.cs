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
