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

import com.asc.common.messaging.mapper.RabbitAuditDataMapper;
import com.asc.common.service.ports.output.message.publisher.AuditMessagePublisher;
import com.asc.registration.core.domain.event.ClientEvent;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.event.TransactionalEventListener;

/** ClientApplicationDomainEventListener listens for client domain events and processes them. */
@Slf4j
@Component
@RequiredArgsConstructor
public class ApplicationDomainEventListener {
  // We do not need outbox here since it is
  // not that crucial to handle all the incoming
  // audit messages
  private final AuditMessagePublisher messagePublisher;
  private final RabbitAuditDataMapper auditDataMapper;

  /**
   * Processes client domain events and publishes audit messages.
   *
   * @param event The client event to process.
   */
  @TransactionalEventListener
  public void process(ClientEvent event) {
    messagePublisher.publish(auditDataMapper.toMessage(event.getAudit()));
  }
}
