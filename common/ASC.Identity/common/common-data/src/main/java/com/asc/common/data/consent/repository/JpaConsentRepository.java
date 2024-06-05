package com.asc.common.data.consent.repository;

import com.asc.common.data.consent.entity.ConsentEntity;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
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

  /**
   * Finds all consent entities by principal name and tenant, joining with the client entity to
   * ensure the client is not invalidated.
   *
   * @param principalName the principal name associated with the consent.
   * @param tenant the tenant ID.
   * @param pageable the pagination information.
   * @return a page of consent entities matching the criteria.
   */
  @Query(
      "SELECT c FROM ConsentEntity c JOIN c.client cl WHERE c.principalName = :principalName AND cl.tenant = :tenant AND cl.invalidated = false")
  Page<ConsentEntity> findAllConsentsByPrincipalNameAndTenant(
      @Param("principalName") String principalName, @Param("tenant") int tenant, Pageable pageable);
}
