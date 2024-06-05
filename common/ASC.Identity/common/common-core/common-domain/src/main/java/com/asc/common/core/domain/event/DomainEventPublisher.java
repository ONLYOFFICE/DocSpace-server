package com.asc.common.core.domain.event;

/**
 * Represents a publisher for domain events of type T, where T extends {@link DomainEvent}.
 *
 * @param <T> The type of the domain event to be published.
 */
public interface DomainEventPublisher<T extends DomainEvent> {

  /**
   * Publishes the given domain event.
   *
   * @param event The domain event to be published.
   */
  void publish(T event);
}
