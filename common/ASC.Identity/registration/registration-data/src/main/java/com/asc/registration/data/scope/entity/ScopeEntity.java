// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.registration.data.scope.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
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
