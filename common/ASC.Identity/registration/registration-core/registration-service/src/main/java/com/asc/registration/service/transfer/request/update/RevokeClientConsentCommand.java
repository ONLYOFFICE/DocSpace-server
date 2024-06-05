package com.asc.registration.service.transfer.request.update;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

/**
 * RevokeClientConsentCommand is a Data Transfer Object (DTO) used to revoke a client's consent. It
 * contains validation annotations to ensure the data integrity.
 */
@Getter
@Setter
@Builder
@AllArgsConstructor
public class RevokeClientConsentCommand {
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  @NotBlank(message = "client id must not be blank")
  @JsonProperty("client_id")
  private String clientId;

  @NotBlank(message = "principal name must not be blank")
  @JsonProperty("principal_name")
  private String principalName;
}
