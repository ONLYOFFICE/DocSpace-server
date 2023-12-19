/**
 *
 */
package com.onlyoffice.authorization.api.configuration;

import lombok.Getter;
import lombok.Setter;
import lombok.ToString;

/**
 *
 */
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

    public void validate() throws IllegalArgumentException {
        if (queue == null || queue.isBlank())
            throw new IllegalArgumentException("Generic queue configuration must have 'queue' field");
        if (exchange == null || exchange.isBlank())
            throw new IllegalArgumentException("Generic queue configuration must have 'exchange' field");
        if (routing == null || routing.isBlank())
            throw new IllegalArgumentException("Generic queue configuration must have 'routing' field");
        if (deadQueue != null && !deadQueue.isBlank()) {
            if (deadExchange == null || deadExchange.isBlank())
                throw new IllegalArgumentException("Generic queue configuration must have 'exchange' for every declared dead letter queue");
            if (deadRouting == null || deadRouting.isBlank())
                throw new IllegalArgumentException("Generic queue configuration must have 'routing' for every declared dead letter queue");
        }
        if (deadQueue != null && autoDelete)
            throw new IllegalArgumentException("Queues with dlq, dlx, dlr and auto-delete are not supported");
    }
}
