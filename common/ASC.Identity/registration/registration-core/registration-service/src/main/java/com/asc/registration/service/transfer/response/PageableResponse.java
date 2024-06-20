package com.asc.registration.service.transfer.response;

import lombok.*;

/**
 * PageableResponse is a Data Transfer Object (DTO) used to encapsulate paginated data responses.
 *
 * @param <D> The type of data contained in the paginated response.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class PageableResponse<D> {

  /** The data contained in the paginated response. */
  private Iterable<D> data;

  /** The current page number. */
  private int page;

  /** The maximum number of items per page. */
  private int limit;

  /** The next page number, if available. */
  private Integer next;

  /** The previous page number, if available. */
  private Integer previous;
}
