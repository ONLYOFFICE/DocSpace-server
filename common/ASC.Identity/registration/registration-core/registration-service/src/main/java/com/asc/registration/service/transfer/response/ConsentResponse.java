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
  @JsonProperty("registered_client_id")
  private String registeredClientId;

  @JsonProperty("principal_name")
  private String principalName;

  private String scopes;

  @JsonProperty("modified_at")
  private ZonedDateTime modifiedOn;

  private boolean invalidated;
  private ClientResponse client;
}
