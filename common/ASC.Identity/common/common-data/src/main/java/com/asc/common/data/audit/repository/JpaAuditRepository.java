package com.asc.common.data.audit.repository;

import com.asc.common.data.audit.entity.AuditEntity;
import org.springframework.data.repository.CrudRepository;

/**
 * JPA repository interface for performing CRUD operations on {@link AuditEntity} objects. This
 * interface extends {@link CrudRepository}, providing basic CRUD functionality.
 */
public interface JpaAuditRepository extends CrudRepository<AuditEntity, Integer> {}
