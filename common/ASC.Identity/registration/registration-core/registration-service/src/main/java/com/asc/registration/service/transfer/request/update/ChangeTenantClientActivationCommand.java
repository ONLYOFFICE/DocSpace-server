package com.asc.registration.service.transfer.request.update;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import java.io.Serializable;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

/**
 * ChangeTenantClientActivationCommand is a Data Transfer Object (DTO) used to change the activation
 * status of a tenant client. It contains validation annotations to ensure the data integrity.
 */
@Getter
@Setter
@Builder
@AllArgsConstructor
public class ChangeTenantClientActivationCommand implements Serializable {
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  @NotBlank(message = "client id must not be blank")
  @JsonProperty("client_id")
  private String clientId;

  private boolean enabled;
}
