package com.asc.common.data.audit.entity;

import com.asc.common.core.domain.value.enums.AuditCode;
import jakarta.persistence.*;
import java.time.ZonedDateTime;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.NoArgsConstructor;

/** Represents an audit event entity. */
@Entity
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "audit_events")
public class AuditEntity {

  /** The unique identifier for the audit event. */
  @Id private int id;

  /** The initiator of the audit event. */
  private String initiator;

  /** The target of the audit event. */
  private String target;

  /** The IP address associated with the audit event. This field is not nullable. */
  @Column(nullable = false)
  private String ip;

  /** The browser used during the audit event. This field is not nullable. */
  @Column(nullable = false)
  private String browser;

  /** The platform used during the audit event. This field is not nullable. */
  @Column(nullable = false)
  private String platform;

  /** The date and time of the audit event. This field is not nullable. */
  @Column(nullable = false)
  private ZonedDateTime date;

  /** The identifier for the tenant associated with the audit event. This field is not nullable. */
  @Column(nullable = false, name = "tenant_id")
  private int tenantId;

  /** The identifier for the user associated with the audit event. This field is not nullable. */
  @Column(nullable = false, name = "user_id")
  private String userId;

  /** The page associated with the audit event. This field is not nullable. */
  @Basic
  @Column(nullable = false)
  private String page;

  /** The action associated with the audit event. This field is not nullable. */
  @Column(nullable = false)
  private int action;

  /**
   * The action associated with the audit event, represented as an enum. This field is transient and
   * will not be persisted in the database.
   */
  @Transient private AuditCode actionEnum;

  /** The description of the audit event. */
  private String description;

  /**
   * This method is called before the entity is persisted. It sets the date to the current date and
   * time, and sets the action based on the actionEnum if it is not null.
   */
  @PrePersist
  void fillAction() {
    this.date = ZonedDateTime.now();
    if (actionEnum != null) {
      this.action = actionEnum.getCode();
    }
  }
}
