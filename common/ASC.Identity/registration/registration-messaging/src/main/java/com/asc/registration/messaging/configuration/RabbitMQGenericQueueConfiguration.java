package com.asc.registration.messaging.configuration;

import lombok.Getter;
import lombok.Setter;
import lombok.ToString;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;

/**
 * Configuration class for defining the properties of a RabbitMQ queue, exchange, and binding. It
 * also includes configurations for dead letter queues (DLQ), maximum message bytes, delivery
 * limits, and TTL.
 */
@Slf4j
@Getter
@Setter
@ToString
public class RabbitMQGenericQueueConfiguration {
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
   * Validates the queue configuration to ensure all required fields are set correctly. If any
   * required field is missing or invalid, an IllegalArgumentException is thrown.
   *
   * @throws IllegalArgumentException if any required field is missing or invalid
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

    try {
      log.info("Validating generic queue configurations");

      if (queue == null || queue.isBlank())
        throw new IllegalArgumentException("Generic queue configuration must have 'queue' field");

      if (exchange == null || exchange.isBlank())
        throw new IllegalArgumentException(
            "Generic queue configuration must have 'exchange' field");

      if (routing == null || routing.isBlank())
        throw new IllegalArgumentException("Generic queue configuration must have 'routing' field");

      if (deadQueue != null && !deadQueue.isBlank()) {
        MDC.put("deadQueue", deadQueue);
        MDC.put("deadExchange", deadExchange);
        MDC.put("deadRouting", deadRouting);
        log.info("Building a DLQ, DLX, DLR for the queue");

        if (deadExchange == null || deadExchange.isBlank())
          throw new IllegalArgumentException(
              "Generic queue configuration must have 'exchange' for every declared dead letter queue");
        if (deadRouting == null || deadRouting.isBlank())
          throw new IllegalArgumentException(
              "Generic queue configuration must have 'routing' for every declared dead letter queue");
      }
    } finally {
      MDC.clear();
    }
  }
}
