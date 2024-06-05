package com.asc.authorization.data.key.entity;

import com.asc.common.core.domain.value.KeyPairType;
import jakarta.persistence.*;
import java.time.ZonedDateTime;
import lombok.*;
import org.hibernate.annotations.UuidGenerator;
import org.springframework.data.annotation.CreatedDate;

/** Entity class representing a key pair. */
@Entity
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "identity_certs")
public class KeyPair {
  /** The unique identifier for the key pair. */
  @Id @UuidGenerator private String id;

  /** The public key. */
  @Column(name = "public_key", nullable = false)
  private String publicKey;

  /** The private key. */
  @Column(name = "private_key", nullable = false)
  private String privateKey;

  /** The type of the key pair. */
  @Column(name = "pair_type", nullable = false)
  @Enumerated(EnumType.ORDINAL)
  private KeyPairType pairType;

  /** The timestamp when the key pair was created. */
  @Column(name = "created_at")
  @CreatedDate
  private ZonedDateTime createdAt;

  /** Sets the createdAt timestamp to the current time before persisting the entity. */
  @PrePersist
  void fillAction() {
    createdAt = ZonedDateTime.now();
  }
}
