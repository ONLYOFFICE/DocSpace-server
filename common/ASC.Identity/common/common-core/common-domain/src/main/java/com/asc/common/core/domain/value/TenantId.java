package com.asc.common.core.domain.value;

/** This class represents a tenant ID. */
public class TenantId extends BaseId<Integer> {

  /**
   * Constructs a new TenantId with the specified value.
   *
   * @param value the value of the tenant ID
   */
  public TenantId(Integer value) {
    super(value);
  }
}
