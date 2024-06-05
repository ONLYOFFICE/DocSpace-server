package com.asc.registration.application.transfer;

import lombok.*;

/**
 * This class represents an error response. It contains a single field, reason, which provides a
 * human-readable explanation of the error.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ErrorResponse {

  /** The reason for the error. */
  private String reason;
}
