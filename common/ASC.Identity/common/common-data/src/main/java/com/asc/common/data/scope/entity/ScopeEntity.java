package com.asc.common.data.scope.entity;

import jakarta.persistence.*;
import java.io.Serializable;
import lombok.*;

/**
 * Entity class representing a scope in the identity management system.
 *
 * <p>This class is mapped to the {@code identity_scopes} table in the database and is used to store
 * information about different scopes, which are used for access control in the system.
 *
 * <p>The class includes fields for the scope's name, group, and type, all of which are unique and
 * non-nullable.
 *
 * <p>The class is annotated with Lombok annotations to generate boilerplate code such as getters,
 * setters, constructors, and the builder pattern.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Entity
@Table(name = "identity_scopes")
public class ScopeEntity implements Serializable {

  /**
   * The name of the scope.
   *
   * <p>This field is used as the primary key in the database and must be unique and non-null.
   */
  @Id
  @Column(name = "name", nullable = false, unique = true)
  private String name;

  /**
   * The group to which the scope belongs.
   *
   * <p>This field must be unique and non-null.
   */
  @Column(name = "group", nullable = false, unique = true)
  private String group;

  /**
   * The type of the scope.
   *
   * <p>This field must be unique and non-null.
   */
  @Column(name = "type", nullable = false, unique = true)
  private String type;
}
