package com.asc.registration.service.transfer.request.fetch;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.NotBlank;
import lombok.*;

/**
 * TenantClientInfoQuery is a Data Transfer Object (DTO) used to query information about a tenant
 * client. It contains validation annotations to ensure the data integrity.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class TenantClientInfoQuery {
  @NotBlank(message = "client id must not be blank")
  @JsonProperty("client_id")
  private String clientId;
}
