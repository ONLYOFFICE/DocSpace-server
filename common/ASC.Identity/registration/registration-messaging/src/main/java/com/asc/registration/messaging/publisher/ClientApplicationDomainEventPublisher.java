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
public class ClientApplicationDomainEventPublisher
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
