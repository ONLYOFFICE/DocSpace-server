package com.asc.registration.service.transfer.request.fetch;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import lombok.*;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class TenantConsentsPaginationQuery {
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  @NotBlank(message = "principal name must not be blank")
  @JsonProperty("principal_name")
  private String principalName;

  @Min(value = 0, message = "page must be greater than or equal to 0")
  private int page;

  @Min(value = 1, message = "limit must be greater than or equal to 1")
  private int limit;
}
