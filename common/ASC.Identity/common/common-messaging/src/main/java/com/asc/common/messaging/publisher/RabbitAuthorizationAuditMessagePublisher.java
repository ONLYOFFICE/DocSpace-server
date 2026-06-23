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

package com.asc.common.messaging.publisher;

import com.asc.common.service.ports.output.message.publisher.AuditMessagePublisher;
import com.asc.common.service.transfer.message.AuditMessage;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.apache.logging.log4j.util.Strings;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnClass;
import org.springframework.stereotype.Component;

/**
 * RabbitMQ message publisher for audit events.
 *
 * <p>This publisher is only loaded when RabbitMQ classes are available on the classpath.
 */
@Slf4j
@Component
@RequiredArgsConstructor
@ConditionalOnClass(name = "com.rabbitmq.client.Connection")
public class RabbitAuthorizationAuditMessagePublisher implements AuditMessagePublisher {
  @Value("${spring.application.region}")
  private String region;

  private final AmqpTemplate amqpClient;

  /**
   * Publishes the given audit message to RabbitMQ.
   *
   * <p>This method retrieves the RabbitMQ queue configuration for the "audit" queue, and attempts
   * to send the audit message using the {@link AmqpTemplate}. If an exception occurs during message
   * sending, it logs the error along with the action, tenant ID, and user ID from the message using
   * the MDC (Mapped Diagnostic Context).
   *
   * @param message the audit message to be published
   */
  public void publish(AuditMessage message) {
    log.debug("Sending an audit message: {}", message);

    try {
      amqpClient.convertAndSend(
          String.format("asc_identity_audit_%s_exchange", region), Strings.EMPTY, message);
    } catch (Exception e) {
      MDC.put("action", String.valueOf(message.getAction()));
      MDC.put("tenant_id", String.valueOf(message.getTenantId()));
      MDC.put("user_id", message.getUserId());
      log.error("Could not send an audit message", e);
      MDC.clear();
    }
  }
}
