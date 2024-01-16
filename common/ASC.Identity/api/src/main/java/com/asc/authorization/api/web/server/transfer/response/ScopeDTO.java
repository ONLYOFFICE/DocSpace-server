/**
 *
 */
package com.asc.authorization.api.web.server.transfer.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.*;

import java.io.Serializable;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class ScopeDTO implements Serializable {
    @JsonInclude(JsonInclude.Include.NON_NULL)
    private String name;
    private String group;
    private String type;
}
