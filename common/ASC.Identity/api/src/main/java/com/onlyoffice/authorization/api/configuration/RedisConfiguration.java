package com.onlyoffice.authorization.api.configuration;

import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;
import lombok.extern.slf4j.Slf4j;
import org.redisson.Redisson;
import org.redisson.api.RateIntervalUnit;
import org.redisson.api.RateType;
import org.redisson.api.RedissonClient;
import org.redisson.config.Config;
import org.slf4j.MDC;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

/**
 *
 */
@Slf4j
@Getter
@Setter
@Configuration
@ConfigurationProperties(prefix = "spring.cloud.ratelimiter.redis")
public class RedisConfiguration {
    private List<String> addresses = new ArrayList<>();
    private List<RateLimiterConfiguration> limiters = new ArrayList<>();
    @Setter
    @NoArgsConstructor
    private static class RateLimiterConfiguration {
        private String name;
        private long limit;
        private long refresh;
    }

    /**
     *
     * @return
     */
    @Bean
    public RedissonClient config(){
        MDC.put("addresses", String.join(",", addresses));
        log.info("Building redisson client");
        MDC.clear();

        var config = new Config();
        var sanitizedAddresses = addresses.stream().map(address -> {
            if (!address.startsWith("redis://"))
                return String.format("redis://%s", address);
            return address;
        }).collect(Collectors.toList());

        if (sanitizedAddresses.size() < 1)
            throw new RuntimeException("No redis address provided");
        if (sanitizedAddresses.size() == 1)
            config.useSingleServer().setAddress(sanitizedAddresses.get(0));
        if (sanitizedAddresses.size() > 1)
            config.useClusterServers().setNodeAddresses(sanitizedAddresses);

        return buildRateLimiters(Redisson.create(config));
    }

    /**
     *
     * @param redissonClient
     * @return
     */
    private RedissonClient buildRateLimiters(RedissonClient redissonClient) {
        for (var limiter : limiters) {
            MDC.put("name", limiter.name);
            MDC.put("limit", String.valueOf(limiter.limit));
            MDC.put("refresh", String.valueOf(limiter.refresh));
            log.info("Adding distributed rate-limiter to redisson client");
            MDC.clear();

            var rlimiter = redissonClient.getRateLimiter(limiter.name);
            rlimiter.setRate(RateType.PER_CLIENT, limiter.limit,
                    limiter.refresh, RateIntervalUnit.SECONDS);
        }

        return redissonClient;
    }
}
