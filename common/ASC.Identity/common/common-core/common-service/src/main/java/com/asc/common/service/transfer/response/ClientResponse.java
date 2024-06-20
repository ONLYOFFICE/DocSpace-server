package com.asc.common.service.transfer.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Set;
import lombok.*;

/**
 * ClientResponse is a Data Transfer Object (DTO) used to transfer detailed client information in
 * responses. This class encapsulates all relevant information about a client, including
 * identification, authentication methods, and various URLs associated with the client.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ClientResponse implements Serializable {

  /** The name of the client. */
  private String name;

  /** The unique identifier of the client. */
  @JsonProperty("client_id")
  private String clientId;

  /** The client secret. Included only if it is not null. */
  @JsonProperty("client_secret")
  @JsonInclude(JsonInclude.Include.NON_NULL)
  private String clientSecret;

  /** The description of the client. */
  private String description;

  /** The website URL of the client. */
  @JsonProperty("website_url")
  private String websiteUrl;

  /** The terms of service URL of the client. */
  @JsonProperty("terms_url")
  private String termsUrl;

  /** The privacy policy URL of the client. */
  @JsonProperty("policy_url")
  private String policyUrl;

  /** The logo of the client. */
  @JsonProperty("logo")
  private String logo;

  /** The authentication methods supported by the client. */
  @JsonProperty("authentication_methods")
  private Set<String> authenticationMethods;

  /** The tenant ID associated with the client. */
  private int tenant;

  /** The redirect URIs registered for the client. */
  @JsonProperty("redirect_uris")
  private Set<String> redirectUris;

  /** The allowed origins for the client. */
  @JsonProperty("allowed_origins")
  private Set<String> allowedOrigins;

  /** The logout redirect URIs registered for the client. */
  @JsonProperty("logout_redirect_uris")
  private Set<String> logoutRedirectUri;

  /** The scopes assigned to the client. */
  private Set<String> scopes;

  /** The date and time when the client was created. */
  @JsonProperty("created_on")
  private ZonedDateTime createdOn;

  /** The user who created the client. */
  @JsonProperty("created_by")
  private String createdBy;

  /** The date and time when the client was last modified. */
  @JsonProperty("modified_on")
  private ZonedDateTime modifiedOn;

  /** The user who last modified the client. */
  @JsonProperty("modified_by")
  private String modifiedBy;

  /** The avatar of the creator. Included only if it is not null. */
  @JsonProperty("creator_avatar")
  @JsonInclude(JsonInclude.Include.NON_NULL)
  private String creatorAvatar;

  /** The display name of the creator. Included only if it is not null. */
  @JsonProperty("creator_display_name")
  @JsonInclude(JsonInclude.Include.NON_NULL)
  private String creatorDisplayName;

  /** Indicates if the client is public. */
  @JsonProperty("is_public")
  private boolean isPublic;

  /** Indicates if the client is enabled. */
  private boolean enabled;

  /** Indicates if the client is invalidated. */
  private boolean invalidated;
}
