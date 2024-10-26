// (c) Copyright Ascensio System SIA 2009-2024
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.application.transfer;

import com.asc.common.utilities.validation.URLCollection;
import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
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
  @Size(max = 255, message = "client description length is expected to be less than 256 characters")
  private String description;

  /**
   * The logo of the client in base64 format. The client logo is expected to be passed as base64.
   */
  @NotEmpty
  @Pattern(
      regexp = "^data:image\\/(?:png|jpeg|jpg|svg\\+xml);base64,.*.{1,}",
      message = "client logo is expected to be passed as base64")
  private String logo;

  /** Indicates whether PKCE is allowed for the client. */
  @JsonProperty("allow_pkce")
  private boolean allowPkce;

  /** Indicates if the client is public. */
  @JsonProperty("is_public")
  private boolean isPublic;

  /** The website URL of the client. The website URL is expected to be passed as a URL. */
  @JsonProperty("website_url")
  @NotEmpty
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "website url is expected to be passed as url")
  private String websiteUrl;

  /** The terms URL of the client. The terms URL is expected to be passed as a URL. */
  @JsonProperty("terms_url")
  @NotEmpty
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "terms url is expected to be passed as url")
  private String termsUrl;

  /** The policy URL of the client. The policy URL is expected to be passed as a URL. */
  @JsonProperty("policy_url")
  @NotEmpty
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "policy url is expected to be passed as url")
  private String policyUrl;

  /** The redirect URIs for the client. */
  @JsonProperty("redirect_uris")
  @NotNull
  @URLCollection
  private Set<String> redirectUris;

  /** The allowed origins for the client. */
  @JsonProperty("allowed_origins")
  @NotNull
  @URLCollection
  private Set<String> allowedOrigins;

  /**
   * The logout redirect URI for the client. The logout redirect URI is expected to be passed as a
   * URL.
   */
  @JsonProperty("logout_redirect_uri")
  @NotEmpty
  @Pattern(
      regexp =
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)",
      message = "logout redirect uri is expected to be passed as url")
  private String logoutRedirectUri;

  /** The scopes for the client. This field cannot be empty. */
  @NotEmpty(message = "scopes field can not be empty")
  private Set<String> scopes;
}
