// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.registration.messaging.mapper;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.transfer.message.AuditMessage;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import org.springframework.stereotype.Component;

/**
 * Mapper class for converting between {@link Audit} and {@link AuditMessage} objects. Provides
 * methods to map from domain entities to messaging data transfer objects and vice versa.
 */
@Component
public class AuditDataMapper {
  private static final String UTC = "UTC";

  /**
   * Converts an {@link Audit} object to an {@link AuditMessage}.
   *
   * @param audit the audit object to be converted
   * @return the resulting audit message
   */
  public AuditMessage toMessage(Audit audit) {
    return AuditMessage.builder()
        .action(audit.getAuditCode().getCode())
        .initiator(audit.getInitiator())
        .target(audit.getTarget())
        .ip(audit.getIp())
        .browser(audit.getBrowser())
        .platform(audit.getPlatform())
        .tenantId(audit.getTenantId())
        .userEmail(audit.getUserEmail())
        .userName(audit.getUserName())
        .userId(audit.getUserId())
        .page(audit.getPage())
        .description(audit.getDescription())
        .date(ZonedDateTime.now(ZoneId.of(UTC)))
        .build();
  }

  /**
   * Converts an {@link AuditMessage} object to an {@link Audit}.
   *
   * @param message the audit message to be converted
   * @return the resulting audit object
   */
  public Audit toAudit(AuditMessage message) {
    return Audit.Builder.builder()
        .auditCode(AuditCode.of(message.getAction()))
        .initiator(message.getInitiator())
        .target(message.getTarget())
        .ip(message.getIp())
        .browser(message.getBrowser())
        .platform(message.getPlatform())
        .tenantId(message.getTenantId())
        .userEmail(message.getUserEmail())
        .userName(message.getUserName())
        .userId(message.getUserId())
        .page(message.getPage())
        .description(message.getDescription())
        .build();
  }
}
