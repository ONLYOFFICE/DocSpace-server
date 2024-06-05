package com.asc.common.core.domain.value;

import java.util.UUID;

/** This class represents a client ID. */
public class ClientId extends BaseId<UUID> {

  /**
   * Constructs a new ClientId with the specified value.
   *
   * @param value the value of the client ID
   */
  public ClientId(UUID value) {
    super(value);
  }
}
