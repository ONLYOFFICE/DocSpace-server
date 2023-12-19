/**
 *
 */
package com.onlyoffice.authorization.core.entities;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.Immutable;

import java.io.Serializable;
import java.time.ZonedDateTime;
import java.util.Objects;

/**
 *
 */
@Builder
@Getter
@Setter
@Entity
@Immutable
@NoArgsConstructor
@EqualsAndHashCode
@AllArgsConstructor
@IdClass(Consent.ConsentId.class)
@Table(name = "identity_consents")
public class Consent {
    @Id
    @Column(name = "registered_client_id")
    private String registeredClientId;
    @Id
    @Column(name = "principal_name")
    private String principalName;
    @Column(name = "scopes")
    @Lob
    private String scopes;
    @Column(name = "modified_at")
    private ZonedDateTime modifiedAt;
    @Column(name = "invalidated")
    private Boolean invalidated;
    @PrePersist
    private void prePersist() {
        this.invalidated = false;
    }
    @Getter
    @Setter
    @AllArgsConstructor
    @NoArgsConstructor
    public static class ConsentId implements Serializable {
        private static final long serialVersionUID = 1L;
        private String registeredClientId;
        private String principalName;

        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;
            ConsentId that = (ConsentId) o;
            return registeredClientId.equals(that.registeredClientId) && principalName.equals(that.principalName);
        }

        public int hashCode() {
            return Objects.hash(registeredClientId, principalName);
        }
    }
}
