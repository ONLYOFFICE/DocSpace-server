// (c) Copyright Ascensio System SIA 2009-2025
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

package com.asc.authorization.data.consent.entity;

import jakarta.persistence.*;
import java.io.Serial;
import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Objects;
import java.util.Set;
import lombok.*;
import org.hibernate.annotations.ColumnDefault;

/**
 * Entity class representing user consent information. This class is mapped to the
 * `identity_consents` table and stores data related to user consents, such as registered client ID,
 * principal ID, and consented scopes.
 */
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Entity
@IdClass(ConsentEntity.ConsentId.class)
@Table(name = "identity_consents")
public class ConsentEntity {

  /**
   * The ID of the registered client associated with the consent. This field is part of the
   * composite primary key.
   */
  @Id
  @Column(name = "registered_client_id")
  private String registeredClientId;

  /**
   * The ID of the principal (user or entity) associated with the consent. This field is part of the
   * composite primary key.
   */
  @Id
  @Column(name = "principal_id")
  private String principalId;

  /**
   * The set of scopes consented to by the principal for the registered client. This data is stored
   * in the `identity_consent_scopes` table.
   */
  @ElementCollection(fetch = FetchType.EAGER)
  @CollectionTable(
      name = "identity_consent_scopes",
      joinColumns = {
        @JoinColumn(name = "registered_client_id", referencedColumnName = "registered_client_id"),
        @JoinColumn(name = "principal_id", referencedColumnName = "principal_id")
      })
  private Set<String> scopes;

  /** The timestamp indicating the last modification date and time of the consent. */
  @Column(name = "modified_at")
  private ZonedDateTime modifiedAt;

  /**
   * A flag indicating whether the consent has been invalidated. Defaults to {@code false} when the
   * entity is persisted.
   */
  @Column(name = "is_invalidated")
  @ColumnDefault("false")
  private boolean invalidated;

  /**
   * Lifecycle callback method invoked before persisting the entity. Ensures that the `invalidated`
   * flag is set to {@code false}.
   */
  @PrePersist
  private void prePersist() {
    this.invalidated = false;
  }

  /**
   * Represents the composite primary key for {@link ConsentEntity}. Combines the
   * `registeredClientId` and `principalId` fields.
   */
  @Getter
  @Setter
  @AllArgsConstructor
  @NoArgsConstructor
  public static class ConsentId implements Serializable {
    @Serial private static final long serialVersionUID = 1L;

    /** The ID of the registered client. */
    private String registeredClientId;

    /** The ID of the principal (user or entity). */
    private String principalId;

    /**
     * Checks equality between two {@link ConsentId} objects.
     *
     * @param o The object to compare with.
     * @return {@code true} if the objects are equal; {@code false} otherwise.
     */
    public boolean equals(Object o) {
      if (this == o) return true;
      if (o == null || getClass() != o.getClass()) return false;
      ConsentId that = (ConsentId) o;
      return registeredClientId.equals(that.registeredClientId)
          && principalId.equals(that.principalId);
    }

    /**
     * Generates the hash code for the {@link ConsentId} object.
     *
     * @return The hash code of the object.
     */
    public int hashCode() {
      return Objects.hash(registeredClientId, principalId);
    }
  }
}
