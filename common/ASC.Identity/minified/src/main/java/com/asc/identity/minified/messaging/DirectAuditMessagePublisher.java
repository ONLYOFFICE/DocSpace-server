// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
