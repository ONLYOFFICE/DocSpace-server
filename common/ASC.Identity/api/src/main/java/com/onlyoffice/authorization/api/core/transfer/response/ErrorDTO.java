/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.response;

import lombok.*;

/**
 *
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class ErrorDTO {
    private String reason;
}
