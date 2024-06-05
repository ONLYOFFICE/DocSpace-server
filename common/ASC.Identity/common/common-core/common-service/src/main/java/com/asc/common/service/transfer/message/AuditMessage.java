package com.asc.common.service.transfer.message;

import java.io.Serializable;
import java.time.ZonedDateTime;
import lombok.*;

@Builder
@Getter
@Setter
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
public class AuditMessage implements Serializable {
  private String tag;
  private String initiator;
  private String target;
  private String ip;
  private String browser;
  private String platform;
  private ZonedDateTime date;
  private int tenantId;
  private String userEmail;
  private String userName;
  private String userId;
  private String page;
  private int action;
  private String description;
}
