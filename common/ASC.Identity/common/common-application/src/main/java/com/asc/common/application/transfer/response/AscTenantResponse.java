package com.asc.common.application.transfer.response;

import java.io.Serializable;
import lombok.*;

/** A response class that represents the details of a tenant. */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AscTenantResponse implements Serializable {

  /** The unique identifier for the tenant. */
  private int tenantId;

  /** The alias of the tenant. */
  private String tenantAlias;

  /** The name of the tenant. */
  private String name;
}
