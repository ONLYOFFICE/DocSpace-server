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

package com.asc.common.messaging.configuration;

import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.utilities.cleanup.RemovedClientReporter;
import java.time.Duration;
import java.util.concurrent.Executors;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Provides the {@link RemovedClientReporter} that turns a lookup showing a client is gone into the
 * usual client removal cleanup.
 *
 * <p>The cleanup goes through the same {@link ClientRemovedEvent} the delete itself publishes, so
 * it removes every authorization and consent the client left behind, in every region, and is
 * harmless to repeat. A single definition serves both services, including the minified build that
 * runs them in one process.
 */
@Configuration
public class RemovedClientReporterConfiguration {
  private static final int MAX_CONCURRENT_LOOKUPS = 4;

  /**
   * Creates the reporter shared by every place that can notice a removed client.
   *
   * @param publisher the publisher of client removal events
   * @param grace how old a leftover row must be before its client is reported
   * @return the reporter
   */
  @Bean(destroyMethod = "close")
  public RemovedClientReporter removedClientReporter(
      AuthorizationMessagePublisher<ClientRemovedEvent> publisher,
      @Value("${IDENTITY_REMOVED_CLIENT_GRACE:PT1M}") Duration grace) {
    return new RemovedClientReporter(
        clientId -> publisher.publish(ClientRemovedEvent.builder().clientId(clientId).build()),
        grace,
        Executors.newVirtualThreadPerTaskExecutor(),
        MAX_CONCURRENT_LOOKUPS);
  }
}
