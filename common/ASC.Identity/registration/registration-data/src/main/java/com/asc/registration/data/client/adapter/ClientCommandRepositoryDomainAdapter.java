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

package com.asc.registration.data.client.adapter;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Repository;

/**
 * Adapter class for handling client command operations and mapping between domain and data layers.
 * Implements the {@link ClientCommandRepository} interface.
 */
@Slf4j
@Repository
@RequiredArgsConstructor
public class ClientCommandRepositoryDomainAdapter implements ClientCommandRepository {
  private static final String UTC = "UTC";

  private final JpaClientRepository jpaClientRepository;
  private final ClientDataAccessMapper clientDataAccessMapper;

  /**
   * Saves a client entity to the database.
   *
   * @param client the client entity to save
   * @return the saved client entity
   */
  public Client saveClient(Client client) {
    log.debug("Persisting a new client");

    var entity = clientDataAccessMapper.toEntity(client);
    var result = jpaClientRepository.save(entity);
    return clientDataAccessMapper.toDomain(result);
  }

  /**
   * Regenerates the secret for a client identified by tenant ID and client ID.
   *
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @return the newly generated client secret
   */
  public String regenerateClientSecretByTenantIdAndClientId(TenantId tenantId, ClientId clientId) {
    log.debug("Regenerating and persisting a new secret");

    var secret = UUID.randomUUID().toString();

    log.debug("Newly generated secret: {}", secret);

    jpaClientRepository.regenerateClientSecretByClientId(
        tenantId.getValue(),
        clientId.getValue().toString(),
        secret,
        ZonedDateTime.now(ZoneId.of(UTC)));

    return secret;
  }

  /**
   * Changes the visibility of a client identified by tenant ID and client ID.
   *
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @param visible the new visibility status
   */
  public void changeVisibilityByTenantIdAndClientId(
      TenantId tenantId, ClientId clientId, boolean visible) {
    log.debug("Persisting client visibility changes");

    jpaClientRepository.changeVisibility(
        tenantId.getValue(),
        clientId.getValue().toString(),
        visible,
        ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Changes the activation status of a client identified by tenant ID and client ID.
   *
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @param enabled the new activation status
   */
  public void changeActivationByTenantIdAndClientId(
      TenantId tenantId, ClientId clientId, boolean enabled) {
    log.debug("Persisting activation changes");

    jpaClientRepository.changeActivation(
        tenantId.getValue(),
        clientId.getValue().toString(),
        enabled,
        ZonedDateTime.now(ZoneId.of(UTC)));
  }

  /**
   * Deletes a client identified by tenant ID and client ID.
   *
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @return the number of clients deleted (typically 0 or 1)
   */
  public int deleteByTenantIdAndClientId(TenantId tenantId, ClientId clientId) {
    log.debug("Persisting invalidated marker");

    return jpaClientRepository.deleteByClientIdAndTenantId(
        clientId.getValue().toString(), tenantId.getValue());
  }
}
