/**
 *
 */
package com.asc.authorization.configuration;

import lombok.Getter;
import lombok.Setter;
import lombok.ToString;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;

/**
 *
 */
@Slf4j
@Getter
@Setter
@ToString
public class GenericQueueConfiguration {
    private String exchange;
    private String queue;
    private String routing;

    private String deadQueue;
    private String deadExchange;
    private String deadRouting;

    private int deadMaxBytes = 15000000;

    private int maxBytes = 20000000;
    private int deliveryLimit = 3;
    private int messageTTL = 100000;

    private boolean nonDurable = false;
    private boolean autoDelete = false;
    private boolean fanOut = false;

    /**
     *
     * @throws IllegalArgumentException
     */
    public void validate() throws IllegalArgumentException {
        MDC.put("queue", queue);
        MDC.put("exchange", exchange);
        MDC.put("routing", routing);
        MDC.put("maxBytes", String.valueOf(maxBytes));
        MDC.put("deliveryLimit", String.valueOf(deliveryLimit));
        MDC.put("messageTTL", String.valueOf(messageTTL));
        MDC.put("nonDurable", String.valueOf(nonDurable));
        MDC.put("autoDelete", String.valueOf(autoDelete));
        MDC.put("fanOut", String.valueOf(fanOut));
        log.info("Validating generic queue configurations");

        if (queue == null || queue.isBlank()) {
            MDC.clear();
            throw new IllegalArgumentException("Generic queue configuration must have 'queue' field");
        }

        if (exchange == null || exchange.isBlank()) {
            MDC.clear();
            throw new IllegalArgumentException("Generic queue configuration must have 'exchange' field");
        }

        if (routing == null || routing.isBlank()) {
            MDC.clear();
            throw new IllegalArgumentException("Generic queue configuration must have 'routing' field");
        }

        if (deadQueue != null && !deadQueue.isBlank()) {
            MDC.put("deadQueue", deadQueue);
            MDC.put("deadExchange", deadExchange);
            MDC.put("deadRouting", deadRouting);
            log.info("Building a dlq, dlx, dlr for the queue");

            if (deadExchange == null || deadExchange.isBlank()) {
                MDC.clear();
                throw new IllegalArgumentException("Generic queue configuration must have 'exchange' for every declared dead letter queue");
            }

            if (deadRouting == null || deadRouting.isBlank()) {
                MDC.clear();
                throw new IllegalArgumentException("Generic queue configuration must have 'routing' for every declared dead letter queue");
            }
        }

        MDC.clear();
    }
}
