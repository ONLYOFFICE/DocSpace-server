package com.asc.common.application.transfer.response;

import java.io.Serializable;
import lombok.*;

/**
 * A wrapper class for responses that includes additional metadata such as status and status code.
 *
 * @param <R> the type of the response object being wrapped
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AscResponseWrapper<R> implements Serializable {

  /** The response object being wrapped. */
  private R response;

  /** The status of the response. */
  private int status;

  /** The status code of the response. */
  private int statusCode;
}
