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

package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

/**
 * ClientCreateCommandHandler handles the creation of new clients. This component coordinates the
 * client creation process by interacting with the domain service, encryption service, repository,
 * and event publisher.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientCreateCommandHandler {
  private final ClientCommandRepository clientCommandRepository;
  private final ClientDataMapper clientDataMapper;
  private final ClientDomainService clientDomainService;
  private final DomainEventPublisher<ClientEvent> messagePublisher;
  private final EncryptionService encryptionService;

  /**
   * Creates a new client based on the provided command and audit information.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the details for creating a new client.
   * @return The response containing the created client's details.
   */
  @Transactional(
      timeout = 2,
      rollbackFor = {Exception.class})
  public ClientResponse createClient(Audit audit, CreateTenantClientCommand command) {
    log.info("Trying to create a new client");

    var client = clientDataMapper.toDomain(command);

    var event = clientDomainService.createClient(audit, client);
    var clientSecret = client.getSecret().value();
    client.encryptSecret(encryptionService::encrypt);

    clientCommandRepository.saveClient(client);
    messagePublisher.publish(event);

    var response = clientDataMapper.toClientResponse(client);
    response.setClientSecret(clientSecret);

    return response;
  }
}
