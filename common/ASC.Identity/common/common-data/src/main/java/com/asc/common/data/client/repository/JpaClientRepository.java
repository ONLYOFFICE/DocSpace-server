package com.asc.common.data.client.repository;

import com.asc.common.data.client.entity.ClientEntity;
import jakarta.annotation.Nonnull;
import java.time.ZonedDateTime;
import java.util.Optional;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.CrudRepository;
import org.springframework.data.repository.query.Param;

/**
 * JPA repository interface for performing CRUD operations on {@link ClientEntity} objects. This
 * interface extends {@link CrudRepository}, providing basic CRUD functionality.
 */
public interface JpaClientRepository extends CrudRepository<ClientEntity, String> {

  /**
   * Finds a client entity by its ID, provided it is not invalidated.
   *
   * @param id the ID of the client entity
   * @return an optional containing the found client entity, or empty if not found
   */
  @Nonnull
  @EntityGraph(
      value = "ClientEntity.withScopesAndAuthMethods",
      type = EntityGraph.EntityGraphType.LOAD)
  @Query("SELECT c FROM ClientEntity c WHERE c.clientId = :id AND c.invalidated = false")
  Optional<ClientEntity> findById(@Param("id") @Nonnull String id);

  /**
   * Finds a client entity by its ID and visibility status, provided it is not invalidated.
   *
   * @param id the ID of the client entity
   * @param accessible the visibility status of the client entity
   * @return an optional containing the found client entity, or empty if not found
   */
  @Nonnull
  @EntityGraph(
      value = "ClientEntity.withScopesAndAuthMethods",
      type = EntityGraph.EntityGraphType.LOAD)
  @Query(
      "SELECT c FROM ClientEntity c WHERE c.clientId = :id AND c.invalidated = false AND c.accessible = :accessible")
  Optional<ClientEntity> findByIdAndVisibility(
      @Param("id") @Nonnull String id, @Param("accessible") boolean accessible);

  /**
   * Finds a client entity by its client_id, provided it is not invalidated.
   *
   * @param clientId the client_id of the client entity
   * @return an optional containing the found client entity, or empty if not found
   */
  @EntityGraph(
      value = "ClientEntity.withScopesAndAuthMethods",
      type = EntityGraph.EntityGraphType.LOAD)
  Optional<ClientEntity> findClientByClientId(String clientId);

  /**
   * Deletes a client entity by its ID and tenant ID.
   *
   * @param id the ID of the client entity
   * @param tenant the tenant ID
   * @return the number of entities deleted
   */
  int deleteByClientIdAndTenantId(String id, int tenant);

  /**
   * Finds a client entity by its ID and tenant ID, fetching scopes and tenant details eagerly.
   *
   * @param id the ID of the client entity
   * @param tenant the tenant ID
   * @return an optional containing the found client entity, or empty if not found
   */
  @EntityGraph(
      value = "ClientEntity.withScopesAndAuthMethods",
      type = EntityGraph.EntityGraphType.LOAD)
  Optional<ClientEntity> findClientByClientIdAndTenantId(
      @Param("id") String id, @Param("tenantId") int tenant);

  /**
   * Finds all client entities for a specific tenant, with pagination support.
   *
   * @param tenant the tenant ID
   * @param pageable the pagination information
   * @return a page of client entities
   */
  @EntityGraph(
      value = "ClientEntity.withScopesAndAuthMethods",
      type = EntityGraph.EntityGraphType.LOAD)
  @Query("SELECT c FROM ClientEntity c WHERE c.tenantId = :tenant AND c.invalidated = false")
  Page<ClientEntity> findAllByTenantId(@Param("tenant") int tenant, Pageable pageable);

  /**
   * Finds all public clients and private clients belonging to a specific tenant, with pagination
   * support.
   *
   * @param tenant the tenant ID
   * @param pageable the pagination information
   * @return a page of client entities
   */
  @EntityGraph(
      value = "ClientEntity.withScopesAndAuthMethods",
      type = EntityGraph.EntityGraphType.LOAD)
  @Query("SELECT c FROM ClientEntity c WHERE c.accessible = true OR c.tenantId = :tenant")
  Page<ClientEntity> findAllPublicAndPrivateByTenant(
      @Param("tenant") int tenant, Pageable pageable);

  /**
   * Regenerates the client secret for a specific client entity by its ID and tenant ID.
   *
   * @param tenant the tenant ID
   * @param clientId the ID of the client entity
   * @param secret the new client secret
   * @param modifiedOn the date and time when the modification was made
   */
  @Modifying
  @Query(
      "UPDATE ClientEntity c SET c.clientSecret = :secret, c.modifiedOn = :modifiedOn WHERE c.clientId = :clientId AND c.tenantId = :tenant")
  void regenerateClientSecretByClientId(
      @Param("tenant") int tenant,
      @Param("clientId") String clientId,
      @Param("secret") String secret,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  /**
   * Changes the visibility status of a specific client entity by its ID and tenant ID.
   *
   * @param tenant the tenant ID
   * @param clientId the ID of the client entity
   * @param accessible the new visibility status
   * @param modifiedOn the date and time when the modification was made
   */
  @Modifying
  @Query(
      "UPDATE ClientEntity c SET c.accessible = :accessible, c.modifiedOn = :modifiedOn WHERE c.clientId = :clientId AND c.tenantId = :tenant")
  void changeVisibility(
      @Param("tenant") int tenant,
      @Param("clientId") String clientId,
      @Param("accessible") boolean accessible,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  /**
   * Changes the activation status of a specific client entity by its ID and tenant ID.
   *
   * @param tenant the tenant ID
   * @param clientId the ID of the client entity
   * @param enabled the new activation status
   * @param modifiedOn the date and time when the modification was made
   */
  @Modifying
  @Query(
      "UPDATE ClientEntity c SET c.enabled = :enabled, c.modifiedOn = :modifiedOn WHERE c.clientId = :clientId AND c.tenantId = :tenant")
  void changeActivation(
      @Param("tenant") int tenant,
      @Param("clientId") String clientId,
      @Param("enabled") boolean enabled,
      @Param("modifiedOn") ZonedDateTime modifiedOn);
}
