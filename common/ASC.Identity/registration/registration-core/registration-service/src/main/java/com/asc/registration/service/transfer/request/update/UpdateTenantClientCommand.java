package com.asc.registration.service.transfer.request.update;

import com.asc.common.utilities.validation.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Pattern;
import java.io.Serializable;
import java.util.Set;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

/**
 * UpdateTenantClientCommand is a Data Transfer Object (DTO) used to update the information of a
 * tenant client. It contains validation annotations to ensure data integrity.
 */
@Getter
@Setter
@Builder
@AllArgsConstructor
public class UpdateTenantClientCommand implements Serializable {

  /** The ID of the tenant. Must be greater than or equal to 1. */
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  /** The ID of the client. Must not be blank. */
  @NotBlank(message = "client id must not be blank")
  @JsonProperty("client_id")
  private String clientId;

  /** The name of the client. Must not be blank. */
  @NotBlank(message = "name must not be blank")
  private String name;

  /** The description of the client. Must not be blank. */
  @NotBlank(message = "description must not be blank")
  private String description;

  /** The logo of the client, expected to be passed as a base64 string. Must not be blank. */
  @NotBlank(message = "client logo is expected to be passed as base64")
  @Pattern(
      regexp = "^data:image\\/(?:png|jpeg|jpg|svg\\+xml);base64,.*.{1,}",
      message = "Client logo is expected to be passed as base64")
  private String logo;

  /** Indicates if PKCE (Proof Key for Code Exchange) is allowed. */
  @JsonProperty("allow_pkce")
  private boolean allowPkce;

  /** Indicates if the client is public. */
  @JsonProperty("is_public")
  private boolean isPublic;

  /** The allowed origins for the client. Each must be a valid URL. */
  @URLCollection
  @JsonProperty("allowed_origins")
  private Set<String> allowedOrigins;
}
