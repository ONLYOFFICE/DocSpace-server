package com.asc.registration.application.transfer;

import com.asc.common.utilities.validation.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
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
 * CreateTenantClientCommandRequest is a data transfer object (DTO) used in the REST layer. It
 * represents a request to create a new tenant client. This class contains the necessary information
 * to create a new client for a tenant. It implements {@link Serializable} to allow instances of
 * this class to be serialized.
 *
 * <p>The class is annotated with Lombok annotations to generate boilerplate code:
 *
 * <ul>
 *   <li>{@link Getter} - Generates getter methods for all fields.
 *   <li>{@link Setter} - Generates setter methods for all fields.
 *   <li>{@link Builder} - Implements the builder pattern for object creation.
 *   <li>{@link AllArgsConstructor} - Generates an all-arguments constructor.
 * </ul>
 *
 * <p>The class also includes validation annotations to ensure that the input data meets the
 * expected format:
 *
 * <ul>
 *   <li>{@link NotEmpty} - Ensures that the field is not empty.
 *   <li>{@link Size} - Validates the size of the string for the name field.
 *   <li>{@link Pattern} - Validates that the field matches the specified regular expression.
 * </ul>
 *
 * Example usage:
 *
 * <pre>{@code
 * CreateTenantClientCommandRequest request = CreateTenantClientCommandRequest.builder()
 *     .name("Example Client")
 *     .description("Description of the client")
 *     .logo("data:image/png;base64,...")
 *     .allowPkce(true)
 *     .websiteUrl("http://example.com")
 *     .termsUrl("http://example.com/terms")
 *     .policyUrl("http://example.com/policy")
 *     .redirectUris(Set.of("http://example.com/redirect"))
 *     .allowedOrigins(Set.of("http://example.com"))
 *     .logoutRedirectUri("http://example.com/logout")
 *     .scopes(Set.of("read", "write"))
 *     .build();
 * }</pre>
 *
 * @see Serializable
 */
@Getter
@Setter
@Builder
@AllArgsConstructor
public class CreateTenantClientCommandRequest implements Serializable {
  /**
   * The name of the client. The client name length is expected to be between 3 and 256 characters.
   */
  @NotEmpty
  @Size(
      min = 3,
      max = 256,
      message = "client name length is expected to be between 3 and 256 characters")
  private String name;

  /** The description of the client. */
  private String description;

  /**
   * The logo of the client in base64 format. The client logo is expected to be passed as base64.
   */
  @Pattern(
      regexp = "^data:image\\/(?:png|jpeg|jpg|svg\\+xml);base64,.*.{1,}",
      message = "client logo is expected to be passed as base64")
  private String logo;

  /** Indicates whether PKCE is allowed for the client. */
  @JsonProperty("allow_pkce")
  private boolean allowPkce;

  /** The website URL of the client. The website URL is expected to be passed as a URL. */
  @JsonProperty("website_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "website url is expected to be passed as url")
  private String websiteUrl;

  /** The terms URL of the client. The terms URL is expected to be passed as a URL. */
  @JsonProperty("terms_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "terms url is expected to be passed as url")
  private String termsUrl;

  /** The policy URL of the client. The policy URL is expected to be passed as a URL. */
  @JsonProperty("policy_url")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "policy url is expected to be passed as url")
  private String policyUrl;

  /** The redirect URIs for the client. */
  @JsonProperty("redirect_uris")
  @URLCollection
  private Set<String> redirectUris;

  /** The allowed origins for the client. */
  @JsonProperty("allowed_origins")
  @URLCollection
  private Set<String> allowedOrigins;

  /**
   * The logout redirect URI for the client. The logout redirect URI is expected to be passed as a
   * URL.
   */
  @JsonProperty("logout_redirect_uri")
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "logout redirect uri is expected to be passed as url")
  private String logoutRedirectUri;

  /** The scopes for the client. This field cannot be empty. */
  @NotEmpty(message = "scopes field can not be empty")
  private Set<String> scopes;
}
