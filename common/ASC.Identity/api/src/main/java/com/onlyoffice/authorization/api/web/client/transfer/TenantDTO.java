/**
 *
 */
package com.onlyoffice.authorization.api.web.client.transfer;

import lombok.*;

/**
 *
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class TenantDTO {
    private int tenantId;
    private String tenantAlias;
    private String name;
}