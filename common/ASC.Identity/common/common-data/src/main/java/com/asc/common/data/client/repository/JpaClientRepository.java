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
   * @param id the ID of the client entity.
   * @return an optional containing the found client entity, or empty if not found.
   */
  @Nonnull
  @Query("SELECT c FROM ClientEntity c WHERE c.clientId = :id AND c.invalidated = false")
  Optional<ClientEntity> findById(@Param("id") @Nonnull String id);

  /**
   * Finds a client entity by its client_id, provided it is not invalidated.
   *
   * @param clientId the client_id of the client entity.
   * @return an optional containing the found client entity, or empty if not found.
   */
  Optional<ClientEntity> findClientByClientId(String clientId);

  /**
   * Deletes a client entity by its ID and tenant.
   *
   * @param id the ID of the client entity.
   * @param tenant the tenant ID.
   * @return the number of entities deleted.
   */
  int deleteByClientIdAndTenant(String id, int tenant);

  /**
   * Finds a client entity by its ID and tenant, fetching scopes and tenant details eagerly.
   *
   * @param id the ID of the client entity.
   * @param tenant the tenant ID.
   * @return an optional containing the found client entity, or empty if not found.
   */
  @EntityGraph(attributePaths = {"scopes", "tenant"})
  Optional<ClientEntity> findClientByClientIdAndTenant(
      @Param("id") String id, @Param("tenant") int tenant);

  /**
   * Finds all client entities for a specific tenant, with pagination.
   *
   * @param tenant the tenant ID.
   * @param pageable the pagination information.
   * @return a page of client entities.
   */
  Page<ClientEntity> findAllByTenant(int tenant, Pageable pageable);

  /**
   * Regenerates the client secret for a specific client entity by its ID and tenant.
   *
   * @param tenant the tenant ID.
   * @param clientId the ID of the client entity.
   * @param secret the new client secret.
   * @param modifiedOn the date and time when the modification was made.
   */
  @Modifying
  @Query(
      "UPDATE ClientEntity c SET c.clientSecret = :secret, c.modifiedOn = :modifiedOn WHERE c.clientId = :clientId AND c.tenant = :tenant")
  void regenerateClientSecretByClientId(
      @Param("tenant") int tenant,
      @Param("clientId") String clientId,
      @Param("secret") String secret,
      @Param("modifiedOn") ZonedDateTime modifiedOn);

  /**
   * Changes the activation status of a specific client entity by its ID and tenant.
   *
   * @param tenant the tenant ID.
   * @param clientId the ID of the client entity.
   * @param enabled the new activation status.
   * @param modifiedOn the date and time when the modification was made.
   */
  @Modifying
  @Query(
      "UPDATE ClientEntity c SET c.enabled = :enabled, c.modifiedOn = :modifiedOn WHERE c.clientId = :clientId AND c.tenant = :tenant")
  void changeActivation(
      @Param("tenant") int tenant,
      @Param("clientId") String clientId,
      @Param("enabled") boolean enabled,
      @Param("modifiedOn") ZonedDateTime modifiedOn);
}
