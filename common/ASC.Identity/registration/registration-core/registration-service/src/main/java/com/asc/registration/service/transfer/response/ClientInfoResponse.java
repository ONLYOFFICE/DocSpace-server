package com.asc.registration.service.transfer.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.io.Serializable;
import lombok.*;

/**
 * ClientInfoResponse is a Data Transfer Object (DTO) used to transfer client information in
 * responses.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ClientInfoResponse implements Serializable {
  private String name;
  private String logo;

  @JsonProperty("website_url")
  private String websiteUrl;
}
