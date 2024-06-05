package com.asc.common.core.domain.entity;

/**
 * This abstract class represents an aggregate root in the domain model. An aggregate root is an
 * entity that is responsible for maintaining the consistency of the aggregate it belongs to. It is
 * the only entity that can be directly accessed from outside the aggregate.
 *
 * @param <T> The type of the aggregate root's identifier.
 */
public abstract class AggregateRoot<T> extends BaseEntity<T> {}
