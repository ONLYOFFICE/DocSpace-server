package com.asc.registration.service.transfer.request.fetch;

import com.fasterxml.jackson.annotation.JsonProperty;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotBlank;
import lombok.*;

/**
 * ConsentsPaginationQuery is a Data Transfer Object (DTO) used to transfer data for querying
 * consents with pagination support. It contains validation annotations to ensure data integrity.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ConsentsPaginationQuery {

  /** The id of the principal (user) whose consents are to be retrieved. Must not be blank. */
  @NotBlank(message = "principal id must not be blank")
  @JsonProperty("principal_id")
  private String principalId;

  /** The page number to retrieve. Must be greater than or equal to 0. */
  @Min(value = 0, message = "page must be greater than or equal to 0")
  private int page;

  /** The number of consents per page. Must be greater than or equal to 1. */
  @Min(value = 1, message = "limit must be greater than or equal to 1")
  private int limit;
}
