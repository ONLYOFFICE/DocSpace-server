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

package com.asc.registration.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;

/**
 * ClientCreateCommandHandler handles the creation of new clients. This component coordinates the
 * client creation process by interacting with the domain service, encryption service, repository,
 * and event publisher.
 */
@Slf4j
@RequiredArgsConstructor
public class ClientCreateCommandHandler {
  private final ClientCommandRepository clientCommandRepository;

  private final ClientDomainService clientDomainService;
  private final EncryptionService encryptionService;

  private final ClientDataMapper clientDataMapper;

  /**
   * Creates a new client based on the provided command and audit information.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the details for creating a new client.
   * @return The response containing the created client's details.
   */
  public ClientResponse createClient(Audit audit, CreateTenantClientCommand command) {
    log.info("Trying to create a new client");

    var client = clientDataMapper.toDomain(command);
    var event = clientDomainService.createClient(audit, client);

    var clientSecret = client.getSecret().value();
    client.encryptSecret(encryptionService::encrypt);

    clientCommandRepository.saveClient(event, client);

    var response = clientDataMapper.toClientResponse(client);
    response.setClientSecret(clientSecret);

    return response;
  }
}
