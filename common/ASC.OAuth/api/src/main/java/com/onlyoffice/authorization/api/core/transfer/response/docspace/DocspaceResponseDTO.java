/**
 *
 */
package com.onlyoffice.authorization.api.core.transfer.response.docspace;

import lombok.*;

/**
 *
 * @param <B>
 */
@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class DocspaceResponseDTO<B> {
    private B response;
    private int status;
    private int statusCode;
}
