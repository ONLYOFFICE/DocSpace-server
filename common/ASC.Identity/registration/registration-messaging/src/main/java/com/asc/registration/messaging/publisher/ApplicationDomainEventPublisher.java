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

package com.asc.registration.messaging.publisher;

import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.registration.core.domain.event.ClientEvent;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.ApplicationEventPublisherAware;
import org.springframework.stereotype.Component;

/**
 * ClientApplicationDomainEventPublisher is responsible for publishing client domain events within
 * the Spring application context.
 *
 * <p>This class implements {@link ApplicationEventPublisherAware} and {@link
 * DomainEventPublisher<ClientEvent>}. It uses the Spring {@link ApplicationEventPublisher} to
 * publish events and logs the publishing process.
 */
@Slf4j
@Component
public class ApplicationDomainEventPublisher
    implements ApplicationEventPublisherAware, DomainEventPublisher<ClientEvent> {
  private ApplicationEventPublisher applicationEventPublisher;

  /**
   * Sets the {@link ApplicationEventPublisher} that this object runs in.
   *
   * @param applicationEventPublisher the {@link ApplicationEventPublisher} to be used by this
   *     object
   */
  public void setApplicationEventPublisher(ApplicationEventPublisher applicationEventPublisher) {
    this.applicationEventPublisher = applicationEventPublisher;
  }

  /**
   * Publishes the given client event within the Spring application context.
   *
   * <p>This method also logs the user ID and action code from the event's audit information using
   * the MDC (Mapped Diagnostic Context).
   *
   * @param event the client event to be published
   */
  public void publish(ClientEvent event) {
    applicationEventPublisher.publishEvent(event);
    MDC.put("user_id", event.getAudit().getUserId());
    MDC.put("action", String.valueOf(event.getAudit().getAuditCode().getCode()));
    log.info("Client event has been published inside Spring application");
    MDC.clear();
  }
}
