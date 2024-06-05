package com.asc.registration.service.transfer.request.create;

import com.asc.common.utilities.validation.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.*;
import java.io.Serializable;
import java.util.Set;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

/**
 * CreateTenantClientCommand is a Data Transfer Object (DTO) used to transfer data for creating a
 * tenant client. It contains validation annotations to ensure the data integrity.
 */
@Getter
@Setter
@Builder
@AllArgsConstructor
public class CreateTenantClientCommand implements Serializable {
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  @NotBlank(message = "tenant url must not be blank")
  @JsonProperty("tenant_url")
  private String tenantUrl;

  @NotEmpty(message = "client name must not be empty")
  @Size(
      min = 3,
      max = 256,
      message = "client name length is expected to be between 3 and 256 characters")
  private String name;

  private String description;

  @Pattern(
      regexp = "^data:image\\/(?:png|jpeg|jpg|svg\\+xml);base64,.*.{1,}",
      message = "client logo is expected to be passed as base64")
  private String logo;

  @JsonProperty("allow_pkce")
  private boolean allowPkce;

  @JsonProperty("website_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "website url is expected to be passed as a valid url")
  private String websiteUrl;

  @JsonProperty("terms_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "terms url is expected to be passed as a valid url")
  private String termsUrl;

  @JsonProperty("policy_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "policy url is expected to be passed as a valid url")
  private String policyUrl;

  @JsonProperty("redirect_uris")
  @URLCollection
  private Set<String> redirectUris;

  @JsonProperty("allowed_origins")
  @URLCollection
  private Set<String> allowedOrigins;

  @JsonProperty("logout_redirect_uri")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "logout redirect uri is expected to be passed as a valid url")
  private String logoutRedirectUri;

  @NotEmpty(message = "scopes field cannot be empty")
  private Set<String> scopes;
}
