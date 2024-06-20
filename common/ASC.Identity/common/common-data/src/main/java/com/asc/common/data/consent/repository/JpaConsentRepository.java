package com.asc.common.data.consent.repository;

import com.asc.common.data.consent.entity.ConsentEntity;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

/**
 * JPA repository interface for performing CRUD operations on {@link ConsentEntity} objects. This
 * interface extends {@link JpaRepository}, providing basic CRUD functionality and pagination
 * support.
 */
public interface JpaConsentRepository
    extends JpaRepository<ConsentEntity, ConsentEntity.ConsentId> {

  @EntityGraph(value = "ConsentEntity.withClientAndScopes", type = EntityGraph.EntityGraphType.LOAD)
  @Query(
      "SELECT c FROM ConsentEntity c JOIN c.client cl WHERE c.principalId = :principalId AND cl.invalidated = false")
  Page<ConsentEntity> findAllConsentsByPrincipalId(
      @Param("principalId") String principalId, Pageable pageable);

  /**
   * Finds all consent entities by principal id and tenant, joining with the client entity to ensure
   * the client is not invalidated.
   *
   * @param principalId the principal id associated with the consent.
   * @param tenant the tenant ID.
   * @param pageable the pagination information.
   * @return a page of consent entities matching the criteria.
   */
  @EntityGraph(value = "ConsentEntity.withClientAndScopes", type = EntityGraph.EntityGraphType.LOAD)
  @Query(
      "SELECT c FROM ConsentEntity c JOIN c.client cl WHERE c.principalId = :principalId AND cl.tenantId = :tenant AND cl.invalidated = false")
  Page<ConsentEntity> findAllConsentsByPrincipalIdAndTenant(
      @Param("principalId") String principalId, @Param("tenant") int tenant, Pageable pageable);
}
