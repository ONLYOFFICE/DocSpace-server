package com.asc.registration.service.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.io.Serializable;
import lombok.*;

/**
 * ClientSecretResponse is a Data Transfer Object (DTO) used to transfer client secret information
 * in responses.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ClientSecretResponse implements Serializable {

  /** The client secret. */
  @JsonProperty("client_secret")
  private String clientSecret;
}
