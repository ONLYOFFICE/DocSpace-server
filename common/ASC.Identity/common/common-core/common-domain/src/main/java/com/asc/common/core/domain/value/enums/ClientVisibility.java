package com.asc.common.core.domain.value.enums;

/**
 * Enum representing the visibility status of a client.
 *
 * <p>Client visibility determines the accessibility level of a client's information and services.
 *
 * <ul>
 *   <li>{@link #PUBLIC}: The client is publicly accessible.
 *   <li>{@link #PRIVATE}: The client is only accessible to authorized entities.
 * </ul>
 */
public enum ClientVisibility {
  /** Indicates that the client is publicly accessible. */
  PUBLIC,

  /** Indicates that the client is only accessible to authorized entities. */
  PRIVATE
}
