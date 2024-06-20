package com.asc.registration.service.transfer.request.fetch;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import lombok.*;

/**
 * ClientInfoPaginationQuery is used to fetch all applications belonging to a tenant and all public
 * applications. It supports pagination.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ClientInfoPaginationQuery {
  /** The tenant ID to fetch private applications for. Does not affect public apps */
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  /** The page number to fetch, starting from 0. */
  @Min(value = 0, message = "page must be greater than or equal to 0")
  private int page;

  /** The number of items per page. */
  @Min(value = 1, message = "limit must be greater than or equal to 1")
  private int limit;
}
