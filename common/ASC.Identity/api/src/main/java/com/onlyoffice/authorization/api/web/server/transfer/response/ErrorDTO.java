/**
 *
 */
package com.onlyoffice.authorization.api.web.server.transfer.response;

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
