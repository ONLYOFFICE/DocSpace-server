package com.asc.common.service.transfer.message;

import java.io.Serializable;
import java.time.ZonedDateTime;
import lombok.*;

/**
 * Represents an audit message that captures details of an audit event.
 *
 * <p>This class encapsulates the details of an audit event including information about the
 * initiator, target, IP address, browser, platform, date, tenant ID, user details, page, action,
 * and description. It implements {@link Serializable} to allow instances to be serialized.
 */
@Builder
@Getter
@Setter
@EqualsAndHashCode
@NoArgsConstructor
@AllArgsConstructor
public class AuditMessage implements Serializable {

  /** A tag associated with the audit event. */
  private String tag;

  /** The initiator of the audit event. */
  private String initiator;

  /** The target of the audit event. */
  private String target;

  /** The IP address from where the audit event was triggered. */
  private String ip;

  /** The browser used during the audit event. */
  private String browser;

  /** The platform used during the audit event. */
  private String platform;

  /** The date and time when the audit event occurred. */
  private ZonedDateTime date;

  /** The tenant ID associated with the audit event. */
  private int tenantId;

  /** The email of the user involved in the audit event. */
  private String userEmail;

  /** The name of the user involved in the audit event. */
  private String userName;

  /** The ID of the user involved in the audit event. */
  private String userId;

  /** The page associated with the audit event. */
  private String page;

  /** The action performed during the audit event, represented as an integer. */
  private int action;

  /** A description of the audit event. */
  private String description;
}
