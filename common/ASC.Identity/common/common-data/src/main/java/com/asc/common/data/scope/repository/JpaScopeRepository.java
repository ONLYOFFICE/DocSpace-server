package com.asc.common.data.scope.repository;

import com.asc.common.data.scope.entity.ScopeEntity;
import org.springframework.data.jpa.repository.JpaRepository;

/**
 * JPA repository interface for performing CRUD operations on {@link ScopeEntity} objects.
 *
 * <p>This interface extends {@link JpaRepository}, providing methods for interacting with {@link
 * ScopeEntity} instances stored in the database. It allows for basic CRUD operations, pagination,
 * and sorting functionality.
 *
 * <p>Spring Data JPA will automatically generate the implementation of this interface at runtime,
 * allowing you to use the repository without providing an implementation.
 */
public interface JpaScopeRepository extends JpaRepository<ScopeEntity, String> {}
