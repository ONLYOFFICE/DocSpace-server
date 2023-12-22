/**
 *
 */
package com.onlyoffice.authorization.web.clients.transfer;

import lombok.*;

/**
 *
 * @param <B>
 */
@Builder
@Getter
@Setter
@NoArgsConstructor
@AllArgsConstructor
public class APIClientDTOWrapper<B> {
    private B response;
    private int status;
    private int statusCode;
}
