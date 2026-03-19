// (c) Copyright Ascensio System SIA 2009-2026
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

package com.asc.identity.minified.messaging;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.AuditCreateCommandHandler;
import com.asc.common.service.ports.output.message.publisher.AuditMessagePublisher;
import com.asc.common.service.transfer.message.AuditMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Primary;
import org.springframework.context.annotation.Profile;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Component;

/**
 * Direct implementation of {@link AuditMessagePublisher} for minified deployment.
 *
 * <p>Instead of sending audit messages to RabbitMQ, this publisher directly persists audit records
 * to the database. This eliminates the need for RabbitMQ infrastructure in minified deployments.
 */
@Slf4j
@Primary
@Component
@Profile("minified")
@RequiredArgsConstructor
public class DirectAuditMessagePublisher implements AuditMessagePublisher {
  private final AuditCreateCommandHandler auditCreateCommandHandler;

  /**
   * Persists the given audit message directly using the domain command handler.
   *
   * <p>This is an in-memory, minified-specific implementation that bypasses RabbitMQ and writes
   * audit records straight to the database. The operation is executed asynchronously.
   *
   * @param message audit message payload to persist
   */
  @Override
  public void publish(AuditMessage message) {
    log.debug(
        "Directly persisting audit: action={}, tenantId={}, userId={}",
        message.getAction(),
        message.getTenantId(),
        message.getUserId());

    try {
      var audit = toAudit(message);
      auditCreateCommandHandler.createAudit(audit);
    } catch (DataIntegrityViolationException e) {
      log.warn(
          "Skipping audit insertion for tenantId: {}. Reason: {}",
          message.getTenantId(),
          e.getMessage());
    } catch (Exception e) {
      log.error("Failed to persist audit record: {}", e.getMessage(), e);
    }
  }

  /**
   * Maps a messaging-layer {@link AuditMessage} into the domain {@link Audit} aggregate.
   *
   * @param message audit message received from the application layer
   * @return fully populated {@link Audit} instance ready for persistence
   */
  private Audit toAudit(AuditMessage message) {
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
