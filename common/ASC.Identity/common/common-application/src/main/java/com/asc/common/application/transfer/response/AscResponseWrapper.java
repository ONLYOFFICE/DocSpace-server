package com.asc.common.application.transfer.response;

import lombok.*;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
public class AscResponseWrapper<R> {
  private R response;
  private int status;
  private int statusCode;
}
