/**
 *
 */
package com.asc.authorization.api.web.client.transfer;

import lombok.*;

/**
 *
 * @param <B>
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class APIClientDTOWrapper<B> {
    private B response;
    private int status;
    private int statusCode;
}