package com.asc.registration.service.transfer.request.create;

import com.asc.common.utilities.validation.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.io.Serializable;
import java.util.Set;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

/**
 * CreateTenantClientCommand is a Data Transfer Object (DTO) used to transfer data for creating a
 * tenant client. It contains validation annotations to ensure data integrity.
 */
@Getter
@Setter
@Builder
@AllArgsConstructor
public class CreateTenantClientCommand implements Serializable {

  /** The ID of the tenant. Must be greater than or equal to 1. */
  @Min(value = 1, message = "tenant id must be greater than or equal to 1")
  @JsonProperty("tenant_id")
  private int tenantId;

  /** The name of the client. Must not be empty and should be between 3 and 256 characters. */
  @NotEmpty(message = "client name must not be empty")
  @Size(
      min = 3,
      max = 256,
      message = "client name length is expected to be between 3 and 256 characters")
  private String name;

  /** A description of the client. */
  private String description;

  /** The logo of the client, expected to be passed as a base64 string. */
  @Pattern(
      regexp = "^data:image\\/(?:png|jpeg|jpg|svg\\+xml);base64,.*.{1,}",
      message = "client logo is expected to be passed as base64")
  private String logo;

  /** Indicates if PKCE (Proof Key for Code Exchange) is allowed. */
  @JsonProperty("allow_pkce")
  private boolean allowPkce;

  /** Indicates if the client is public. */
  @JsonProperty("is_public")
  private boolean isPublic;

  /** The website URL of the client. Must be a valid URL. */
  @JsonProperty("website_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "website url is expected to be passed as a valid url")
  private String websiteUrl;

  /** The terms of service URL of the client. Must be a valid URL. */
  @JsonProperty("terms_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "terms url is expected to be passed as a valid url")
  private String termsUrl;

  /** The privacy policy URL of the client. Must be a valid URL. */
  @JsonProperty("policy_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "policy url is expected to be passed as a valid url")
  private String policyUrl;

  /** The redirect URIs for the client. Each must be a valid URL. */
  @JsonProperty("redirect_uris")
  @URLCollection
  private Set<String> redirectUris;

  /** The allowed origins for the client. Each must be a valid URL. */
  @JsonProperty("allowed_origins")
  @URLCollection
  private Set<String> allowedOrigins;

  /** The logout redirect URI for the client. Must be a valid URL. */
  @JsonProperty("logout_redirect_uri")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "logout redirect uri is expected to be passed as a valid url")
  private String logoutRedirectUri;

  /** The scopes for the client. Must not be empty. */
  @NotEmpty(message = "scopes field cannot be empty")
  private Set<String> scopes;
}
