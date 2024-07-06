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

package com.asc.authorization.application.security.oauth.services;

import com.asc.authorization.application.mapper.ClientMapper;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.cache.annotation.Cacheable;
import org.springframework.stereotype.Service;

/** Service class for handling cached operations related to registered clients. */
@Slf4j
@Service
@RequiredArgsConstructor
public class AscCacheableClientService implements CacheableRegisteredClientQueryService {
  private final ClientMapper clientMapper;
  private final JpaClientRepository jpaClientRepository;
  private final EncryptionService encryptionService;

  /**
   * Finds a client by its ID with caching support.
   *
   * @param id the ID of the client.
   * @return the ClientResponse containing the client's details or null.
   */
  @Cacheable(value = "clients", key = "#id", unless = "#result == null")
  public ClientResponse findById(String id) {
    var result = jpaClientRepository.findById(id);
    if (result.isEmpty()) return null;

    var client = clientMapper.toClientResponse(result.get());
    client.setClientSecret(encryptionService.decrypt(client.getClientSecret()));
    return client;
  }

  /**
   * Finds a client by its client ID with caching support.
   *
   * @param clientId the client ID of the client.
   * @return the ClientResponse containing the client's details or null.
   */
  @Cacheable(value = "clients", key = "#clientId", unless = "#result == null")
  public ClientResponse findByClientId(String clientId) {
    var result = jpaClientRepository.findClientByClientId(clientId);
    if (result.isEmpty()) return null;

    var client = clientMapper.toClientResponse(result.get());
    client.setClientSecret(encryptionService.decrypt(client.getClientSecret()));
    return client;
  }
}
