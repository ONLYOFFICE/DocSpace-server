package com.asc.authorization.api.core.entities;

import jakarta.persistence.*;
import lombok.Builder;

import java.time.ZonedDateTime;

/**
 *
 */
@Entity
@Builder
@Table(name = "audit_events")
public class Audit {
    @Id
    private int id;
    private String initiator;
    private String target;
    @Column(nullable = false)
    private String ip;
    @Column(nullable = false)
    private String browser;
    @Column(nullable = false)
    private String platform;
    @Column(nullable = false)
    private ZonedDateTime date;
    @Column(nullable = false, name = "tenant_id")
    private int tenantId;
    @Column(nullable = false, name = "user_id")
    private String userId;
    @Column(nullable = false)
    private String page;
    @Basic
    @Column(nullable = false)
    private int action;
    @Transient
    private Action actionEnum;
    private String description;
    @PrePersist
    void fillAction() {
        if (actionEnum != null) {
            this.action = actionEnum.getCode();
        }
    }
}
