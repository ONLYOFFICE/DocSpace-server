package com.asc.registration.service.transfer.request.update;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import lombok.*;

/**
 * DeleteTenantClientCommand is a Data Transfer Object (DTO) used to delete a tenant client. It
 * contains validation annotations to ensure data integrity.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class DeleteTenantClientCommand {

  /** The ID of the tenant. Must be greater than or equal to 1. */
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  /** The ID of the client. Must not be blank. */
  @NotBlank(message = "client id must not be blank")
  @JsonProperty("client_id")
  private String clientId;
}