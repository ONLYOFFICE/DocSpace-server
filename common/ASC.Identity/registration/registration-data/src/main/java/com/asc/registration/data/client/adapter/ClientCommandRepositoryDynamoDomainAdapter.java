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

package com.asc.registration.data.client.adapter;

import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.data.client.repository.DynamoClientRepository;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.UUID;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Repository;
import org.springframework.transaction.annotation.Transactional;

/**
 * Adapter implementation of the {@link ClientCommandRepository} interface for DynamoDB. This class
 * provides methods for managing client data in a DynamoDB repository, including saving, updating,
 * regenerating secrets, changing visibility or activation, and deleting clients.
 */
@Slf4j
@Repository
@Profile(value = "saas")
@RequiredArgsConstructor
public class ClientCommandRepositoryDynamoDomainAdapter implements ClientCommandRepository {
  private static final String UTC = "UTC";

  private final DynamoClientRepository dynamoClientRepository;
  private final ClientDataAccessMapper clientDataAccessMapper;
  private final AuthorizationMessagePublisher<ClientRemovedEvent> authorizationMessagePublisher;
  private final DomainEventPublisher<ClientEvent> messagePublisher;

  /**
   * Saves a client to the DynamoDB repository and publishes the associated event.
   *
   * @param event the client event to be published
   * @param client the client entity to be saved
   * @return the saved client entity
   */
  @Transactional(readOnly = true)
  public Client saveClient(ClientEvent event, Client client) {
    dynamoClientRepository.save(clientDataAccessMapper.toDynamoEntity(client));
    messagePublisher.publish(event);
    return client;
  }

  /**
   * Updates a client in the DynamoDB repository and publishes the associated event.
   *
   * @param event the client event to be published
   * @param client the client entity to be updated
   * @return the updated client entity
   */
  @Transactional(readOnly = true)
  public Client updateClient(ClientEvent event, Client client) {
    var result = dynamoClientRepository.update(clientDataAccessMapper.toDynamoEntity(client));
    messagePublisher.publish(event);
    return clientDataAccessMapper.toDomain(result);
  }

  /**
   * Regenerates the client secret for a specific tenant and client in the DynamoDB repository and
   * publishes the associated event.
   *
   * @param event the client event to be published
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @return the newly generated client secret
   */
  @Transactional(readOnly = true)
  public String regenerateClientSecretByTenantIdAndClientId(
      ClientEvent event, TenantId tenantId, ClientId clientId) {
    var secret = UUID.randomUUID().toString();
    dynamoClientRepository.updateClientSecret(
        clientId.getValue().toString(),
        tenantId.getValue(),
        secret,
        ZonedDateTime.now(ZoneId.of(UTC)));
    messagePublisher.publish(event);
    return secret;
  }

  /**
   * Changes the visibility of a client for a specific tenant and client in the DynamoDB repository
   * and publishes the associated event.
   *
   * @param event the client event to be published
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @param visible the new visibility state
   */
  @Transactional(readOnly = true)
  public void changeVisibilityByTenantIdAndClientId(
      ClientEvent event, TenantId tenantId, ClientId clientId, boolean visible) {
    dynamoClientRepository.updateVisibility(
        clientId.getValue().toString(),
        tenantId.getValue(),
        visible,
        ZonedDateTime.now(ZoneId.of(UTC)));
    messagePublisher.publish(event);
  }

  /**
   * Changes the activation state of a client for a specific tenant and client in the DynamoDB
   * repository and publishes the associated event.
   *
   * @param event the client event to be published
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @param enabled the new activation state
   */
  @Transactional(readOnly = true)
  public void changeActivationByTenantIdAndClientId(
      ClientEvent event, TenantId tenantId, ClientId clientId, boolean enabled) {
    dynamoClientRepository.updateActivation(
        clientId.getValue().toString(),
        tenantId.getValue(),
        enabled,
        ZonedDateTime.now(ZoneId.of(UTC)));
    messagePublisher.publish(event);
  }

  /**
   * Deletes a client for a specific tenant and client in the DynamoDB repository, publishes the
   * associated removal event, and returns the result.
   *
   * @param event the client event to be published
   * @param tenantId the tenant ID
   * @param clientId the client ID
   * @return 1 if the client was successfully deleted, 0 otherwise
   */
  @Transactional(readOnly = true)
  public int deleteByTenantIdAndClientId(ClientEvent event, TenantId tenantId, ClientId clientId) {

    authorizationMessagePublisher.publish(
        ClientRemovedEvent.builder().clientId(clientId.getValue().toString()).build());
    messagePublisher.publish(event);
    return dynamoClientRepository.deleteByIdAndTenantId(
                clientId.getValue().toString(), tenantId.getValue())
            != null
        ? 1
        : 0;
  }
}
