package com.asc.registration.service.transfer.request.update;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.NotBlank;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import lombok.Setter;

/**
 * Command object for revoking client consent. This class is used to transfer the necessary data to
 * revoke a client's consent.
 */
@Getter
@Setter
@Builder
@AllArgsConstructor
public class RevokeClientConsentCommand {

  /** The unique identifier of the client. */
  @NotBlank(message = "client id must not be blank")
  @JsonProperty("client_id")
  private String clientId;

  /** The id of the principal (user) whose consent is to be revoked. */
  @NotBlank(message = "principal id must not be blank")
  @JsonProperty("principal_id")
  private String principalId;
}
