package com.asc.registration.service.transfer.request.fetch;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import lombok.*;

/**
 * TenantClientsPaginationQuery is a Data Transfer Object (DTO) used to query paginated information
 * about tenant clients. It contains validation annotations to ensure the data integrity.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class TenantClientsPaginationQuery {
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  @Min(value = 0, message = "page must be greater than or equal to 0")
  private int page;

  @Min(value = 1, message = "limit must be greater than or equal to 1")
  private int limit;
}
