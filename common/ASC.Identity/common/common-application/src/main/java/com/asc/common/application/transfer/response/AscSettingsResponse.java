package com.asc.common.application.transfer.response;

import lombok.*;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AscSettingsResponse {
  private String timezone;
}
