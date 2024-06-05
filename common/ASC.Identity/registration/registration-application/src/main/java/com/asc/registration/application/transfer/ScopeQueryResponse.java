package com.asc.registration.application.transfer;

import com.fasterxml.jackson.annotation.JsonInclude;
import java.io.Serializable;
import lombok.*;

/**
 * This class represents a response to a scope query. It contains three fields: name, group, and
 * type. The name field is optional and is only included if it is not null.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class ScopeQueryResponse implements Serializable {

  /** The name of the scope. This field is optional and is only included if it is not null. */
  @JsonInclude(JsonInclude.Include.NON_NULL)
  private String name;

  /** The group of the scope. */
  private String group;

  /** The type of the scope. */
  private String type;
}
