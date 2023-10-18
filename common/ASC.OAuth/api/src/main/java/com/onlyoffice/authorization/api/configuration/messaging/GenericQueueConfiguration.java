/**
 *
 */
package com.onlyoffice.authorization.api.configuration.messaging;

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
}
