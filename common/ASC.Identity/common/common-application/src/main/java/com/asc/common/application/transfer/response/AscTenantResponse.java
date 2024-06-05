package com.asc.common.application.transfer.response;

import lombok.*;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AscTenantResponse {
  private int tenantId;
  private String tenantAlias;
  private String name;
}
