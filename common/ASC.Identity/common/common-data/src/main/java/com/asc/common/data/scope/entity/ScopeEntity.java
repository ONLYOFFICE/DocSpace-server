// (c) Copyright Ascensio System SIA 2009-2024
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
