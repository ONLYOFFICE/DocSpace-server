package com.asc.registration.service.transfer.response;

import lombok.*;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ScopeResponse {
  /** The name of the scope. */
  private String name;

  /** The group of the scope. */
  private String group;

  /** The type of the scope. */
  private String type;
}
