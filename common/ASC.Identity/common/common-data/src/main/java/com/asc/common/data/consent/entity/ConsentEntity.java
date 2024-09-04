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

package com.asc.common.data.consent.entity;

import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.scope.entity.ScopeEntity;
import com.fasterxml.jackson.annotation.JsonManagedReference;
import jakarta.persistence.*;
import java.io.Serial;
import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Objects;
import java.util.Set;
import lombok.*;
import org.hibernate.annotations.ColumnDefault;

@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Entity
@IdClass(ConsentEntity.ConsentId.class)
@Table(name = "identity_consents")
@NamedEntityGraphs({
  @NamedEntityGraph(
      name = "ConsentEntity.withClientAndScopes",
      attributeNodes = {@NamedAttributeNode("client"), @NamedAttributeNode("scopes")})
})
public class ConsentEntity {

  /** The registered client id. */
  @Id
  @Column(name = "registered_client_id")
  private String registeredClientId;

  /** The principal id. */
  @Id
  @Column(name = "principal_id")
  private String principalId;

  /** The scopes of the consent. */
  @ManyToMany(fetch = FetchType.EAGER)
  @JoinTable(
      name = "identity_consent_scopes",
      joinColumns = {
        @JoinColumn(name = "registered_client_id", referencedColumnName = "registered_client_id"),
        @JoinColumn(name = "principal_id", referencedColumnName = "principal_id")
      },
      inverseJoinColumns = @JoinColumn(name = "scope_name", referencedColumnName = "name"))
  private Set<ScopeEntity> scopes;

  /** The date and time when the consent was last modified. */
  @Column(name = "modified_at")
  private ZonedDateTime modifiedAt;

  /** Indicates whether the consent is invalidated or not. */
  @Column(name = "is_invalidated")
  @ColumnDefault("false")
  private boolean invalidated;

  /** The client entity associated with this consent. */
  @JsonManagedReference
  @ManyToOne(fetch = FetchType.EAGER)
  @JoinColumn(
      name = "registered_client_id",
      referencedColumnName = "client_id",
      insertable = false,
      updatable = false)
  private ClientEntity client;

  /** This method is called before persisting the entity and sets the invalidated flag to false. */
  @PrePersist
  private void prePersist() {
    this.invalidated = false;
  }

  /** This class represents the composite primary key for ConsentEntity. */
  @Getter
  @Setter
  @AllArgsConstructor
  @NoArgsConstructor
  public static class ConsentId implements Serializable {
    @Serial private static final long serialVersionUID = 1L;
    private String registeredClientId;
    private String principalId;

    /**
     * This method checks the equality of two ConsentId objects.
     *
     * @param o the object to compare with
     * @return true if the objects are equal, false otherwise
     */
    public boolean equals(Object o) {
      if (this == o) return true;
      if (o == null || getClass() != o.getClass()) return false;
      ConsentId that = (ConsentId) o;
      return registeredClientId.equals(that.registeredClientId)
          && principalId.equals(that.principalId);
    }

    /**
     * This method returns the hash code of the ConsentId object.
     *
     * @return the hash code of the object
     */
    public int hashCode() {
      return Objects.hash(registeredClientId, principalId);
    }
  }
}
