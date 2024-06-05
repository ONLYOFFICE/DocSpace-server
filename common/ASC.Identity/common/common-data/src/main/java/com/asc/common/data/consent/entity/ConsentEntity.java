package com.asc.common.data.consent.entity;

import com.asc.common.data.client.entity.ClientEntity;
import com.fasterxml.jackson.annotation.JsonManagedReference;
import jakarta.persistence.*;
import java.io.Serial;
import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Objects;
import lombok.*;

/**
 * This class represents the ConsentEntity which is a JPA entity for managing consents. It has a
 * composite primary key consisting of registeredClientId and principalName.
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

  /** The registered client id. */
  @Id
  @Column(name = "registered_client_id")
  private String registeredClientId;

  /** The principal name. */
  @Id
  @Column(name = "principal_name")
  private String principalName;

  /** The scopes of the consent. */
  @Lob
  @Column(name = "scopes")
  private String scopes;

  /** The date and time when the consent was last modified. */
  @Column(name = "modified_at")
  private ZonedDateTime modifiedAt;

  /** Indicates whether the consent is invalidated or not. */
  @Column(name = "invalidated")
  private Boolean invalidated;

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

  /**
   * This class represents the composite primary key for ConsentEntity.
   *
   * @author [Your Name]
   * @since [Version]
   */
  @Getter
  @Setter
  @AllArgsConstructor
  @NoArgsConstructor
  public static class ConsentId implements Serializable {
    @Serial private static final long serialVersionUID = 1L;
    private String registeredClientId;
    private String principalName;

    /**
     * This method checks the equality of two ConsentId objects.
     *
     * @param o the object to compare with
     * @return true if the objects are equal, false otherwise
     */
    @Override
    public boolean equals(Object o) {
      if (this == o) return true;
      if (o == null || getClass() != o.getClass()) return false;
      ConsentId that = (ConsentId) o;
      return registeredClientId.equals(that.registeredClientId)
          && principalName.equals(that.principalName);
    }

    /**
     * This method returns the hash code of the ConsentId object.
     *
     * @return the hash code of the object
     */
    @Override
    public int hashCode() {
      return Objects.hash(registeredClientId, principalName);
    }
  }
}
