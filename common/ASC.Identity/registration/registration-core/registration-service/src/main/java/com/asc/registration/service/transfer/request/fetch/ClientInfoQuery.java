package com.asc.registration.service.transfer.request.fetch;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import lombok.*;

/**
 * ClientInfoQuery is a Data Transfer Object (DTO) used to query information about a tenant client.
 * It contains validation annotations to ensure the data integrity.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ClientInfoQuery {
  /** The tenant ID to fetch private applications for. Does not affect public apps */
  @Min(value = 0, message = "tenant id must be greater than or equal to 0")
  @JsonProperty("tenant_id")
  private int tenantId;

  /** The client ID to fetch */
  @NotBlank(message = "client id must not be blank")
  @JsonProperty("client_id")
  private String clientId;
}
