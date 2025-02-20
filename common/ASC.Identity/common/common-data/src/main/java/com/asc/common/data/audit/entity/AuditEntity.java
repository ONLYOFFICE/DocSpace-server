// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
  @Id private long id;

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
  private long tenantId;

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
