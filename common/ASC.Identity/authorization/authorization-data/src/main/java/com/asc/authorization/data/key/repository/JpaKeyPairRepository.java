package com.asc.authorization.data.key.repository;

import com.asc.authorization.data.key.entity.KeyPair;
import org.springframework.data.repository.CrudRepository;

/**
 * Repository interface for performing CRUD operations on {@link KeyPair} entities.
 *
 * <p>This interface extends {@link CrudRepository} and provides methods for interacting with the
 * KeyPair entities stored in the database.
 */
public interface JpaKeyPairRepository extends CrudRepository<KeyPair, String> {}
