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
  private Iterable<D> data;
  private int page;
  private int limit;
  private Integer next;
  private Integer previous;
}
