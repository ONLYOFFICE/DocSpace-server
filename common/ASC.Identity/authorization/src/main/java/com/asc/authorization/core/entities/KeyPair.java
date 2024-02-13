package com.asc.authorization.core.entities;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.UuidGenerator;
import org.springframework.data.annotation.CreatedDate;

import java.time.ZonedDateTime;

@Entity
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "identity_certs")
public class KeyPair {
    @Id
    @UuidGenerator
    private String id;
    @Column(name = "public_key", nullable = false)
    private String publicKey;
    @Column(name = "private_key", nullable = false)
    private String privateKey;
    @Column(name = "pair_type", nullable = false)
    @Enumerated(EnumType.ORDINAL)
    private KeyPairType pairType;
    @Column(name = "created_at")
    @CreatedDate
    private ZonedDateTime createdAt;
    @PrePersist
    void fillAction() {
        createdAt = ZonedDateTime.now();
    }
}
