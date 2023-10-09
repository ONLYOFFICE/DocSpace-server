package com.onlyoffice.authorization.api.dto.response.docspace;

import lombok.*;

@NoArgsConstructor
@AllArgsConstructor
@Builder
@Getter
@Setter
public class TenantDTO {
    private int tenantId;
    private String tenantAlias;
    private String name;
}
