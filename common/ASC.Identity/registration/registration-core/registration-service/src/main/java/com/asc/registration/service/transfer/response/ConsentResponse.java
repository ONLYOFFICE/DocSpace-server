package com.asc.registration.service.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.ZonedDateTime;
import lombok.*;

/**
 * ConsentResponse is a Data Transfer Object (DTO) used to transfer consent information in
 * responses.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ConsentResponse {

  /** The ID of the registered client. */
  @JsonProperty("registered_client_id")
  private String registeredClientId;

  /** The name of the principal (user) who granted the consent. */
  @JsonProperty("principal_name")
  private String principalName;

  /** The scopes associated with the consent. */
  private String scopes;

  /** The date and time when the consent was last modified. */
  @JsonProperty("modified_at")
  private ZonedDateTime modifiedOn;

  /** Indicates if the consent is invalidated. */
  private boolean invalidated;

  /** Detailed information about the client associated with the consent. */
  private ClientInfoResponse client;
}
