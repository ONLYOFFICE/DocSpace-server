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

package com.asc.registration.messaging.listener;

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
import org.springframework.messaging.Message;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Component;

/** RabbitClientAuditMessageListener listens for audit messages from RabbitMQ and processes them. */
@Slf4j
@Component
@RequiredArgsConstructor
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

      auditCreateCommandHandler.createAudits(
          messages.stream()
              .map(s -> auditDataMapper.toAudit(s.getPayload()))
              .collect(Collectors.toSet()));
    }
  }
}
