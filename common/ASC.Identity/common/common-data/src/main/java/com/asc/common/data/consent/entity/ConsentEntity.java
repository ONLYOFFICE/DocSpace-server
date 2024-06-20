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
