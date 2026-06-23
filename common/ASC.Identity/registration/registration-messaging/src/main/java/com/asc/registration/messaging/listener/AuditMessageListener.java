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

package com.asc.registration.messaging.listener;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.messaging.mapper.RabbitAuditDataMapper;
import com.asc.common.service.AuditCreateCommandHandler;
import com.asc.common.service.transfer.message.AuditMessage;
import com.rabbitmq.client.Channel;
import java.util.List;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.messaging.Message;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Component;

/**
 * RabbitClientAuditMessageListener listens for audit messages from RabbitMQ and processes them.
 *
 * <p>This listener is only loaded when RabbitMQ client classes are available on the classpath.
 */
@Slf4j
@Component
@RequiredArgsConstructor
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class AuditMessageListener {
  private final AuditCreateCommandHandler auditCreateCommandHandler;
  private final RabbitAuditDataMapper auditDataMapper;

  /**
   * Receives and processes audit messages from RabbitMQ.
   *
   * @param messages The list of audit messages.
   * @param channel The RabbitMQ channel.
   */
  @RabbitListener(
      queues = "asc_identity_audit_${spring.application.region}_queue",
      containerFactory = "batchRabbitListenerContainerFactory")
  public void receiveMessage(@Payload List<Message<AuditMessage>> messages, Channel channel) {
    if (!messages.isEmpty()) {
      MDC.put("count", String.valueOf(messages.size()));
      log.debug("Persisting audit messages");
      MDC.clear();

      var tenantAudits =
          messages.stream()
              .map(message -> auditDataMapper.toAudit(message.getPayload()))
              .collect(Collectors.groupingBy(Audit::getTenantId, Collectors.toSet()));

      tenantAudits.forEach(
          (tenantId, audits) -> {
            try {
              log.debug(
                  "Processing batch of {} audit records for tenantId: {}", audits.size(), tenantId);
              auditCreateCommandHandler.createAudits(audits);
            } catch (DataIntegrityViolationException fke) {
              log.warn(
                  "Skipping audit insertion for tenantId: {}. Reason: {}",
                  tenantId,
                  fke.getMessage());
            }
          });
    }
  }
}
