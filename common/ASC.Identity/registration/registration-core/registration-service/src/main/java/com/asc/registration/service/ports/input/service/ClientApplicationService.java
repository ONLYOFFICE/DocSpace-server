package com.asc.registration.service.ports.input.service;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.request.fetch.TenantClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.TenantConsentsPaginationQuery;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.*;
import jakarta.validation.Valid;

/**
 * ClientApplicationService defines the contract for client-related operations in the application
 * layer. It includes methods for managing clients, such as creating, updating, deleting, and
 * retrieving client information, as well as handling client consents and activation states.
 */
public interface ClientApplicationService {
  /**
   * Retrieves detailed information about a specific client.
   *
   * @param query The query containing the tenant ID and client ID.
   * @return A response containing the client's details.
   */
  ClientResponse getClient(@Valid TenantClientQuery query);

  /**
   * Retrieves basic information about a specific client.
   *
   * @param query The query containing the tenant ID and client ID.
   * @return A response containing the client's basic information.
   */
  ClientInfoResponse getClientInfo(@Valid TenantClientInfoQuery query);

  /**
   * Retrieves a paginated list of clients for a specific tenant.
   *
   * @param query The query containing pagination parameters and tenant ID.
   * @return A pageable response containing a list of client responses.
   */
  PageableResponse<ClientResponse> getClients(@Valid TenantClientsPaginationQuery query);

  /**
   * Retrieves a paginated list of consents for a specific tenant.
   *
   * @param query The query containing pagination parameters, tenant ID, and principal name.
   * @return A pageable response containing a list of consent responses.
   */
  PageableResponse<ConsentResponse> getConsents(@Valid TenantConsentsPaginationQuery query);

  /**
   * Creates a new client for a specific tenant.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the details for creating a new client.
   * @return A response containing the created client's details.
   */
  ClientResponse createClient(@Valid Audit audit, @Valid CreateTenantClientCommand command);

  /**
   * Regenerates the secret key for a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the tenant ID and client ID.
   * @return A response containing the new client secret.
   */
  ClientSecretResponse regenerateSecret(
      @Valid Audit audit, @Valid RegenerateTenantClientSecretCommand command);

  /**
   * Changes the activation state of a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the tenant ID, client ID, and the new activation state.
   */
  void changeActivation(@Valid Audit audit, @Valid ChangeTenantClientActivationCommand command);

  /**
   * Updates the details of a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the updated client details.
   * @return A response containing the updated client's details.
   */
  ClientResponse updateClient(@Valid Audit audit, @Valid UpdateTenantClientCommand command);

  /**
   * Deletes a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the tenant ID and client ID.
   */
  void deleteClient(@Valid Audit audit, @Valid DeleteTenantClientCommand command);

  /**
   * Revokes the consent of a specific client.
   *
   * @param audit The audit information containing details about the user performing the operation.
   * @param command The command containing the tenant ID, client ID, and principal name.
   */
  void revokeClientConsent(@Valid Audit audit, @Valid RevokeClientConsentCommand command);
}
