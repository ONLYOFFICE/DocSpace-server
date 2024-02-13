/**
 *
 */
package com.asc.authorization.web.security.crypto.jwks;

import com.asc.authorization.core.entities.KeyPairType;
import com.asc.authorization.web.server.utilities.KeyPairMapper;
import com.nimbusds.jose.jwk.Curve;
import com.nimbusds.jose.jwk.ECKey;
import com.nimbusds.jose.jwk.JWK;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.context.annotation.Primary;
import org.springframework.stereotype.Component;

import java.math.BigInteger;
import java.security.KeyPair;
import java.security.NoSuchAlgorithmException;
import java.security.PrivateKey;
import java.security.PublicKey;
import java.security.interfaces.ECPrivateKey;
import java.security.interfaces.ECPublicKey;
import java.security.spec.*;

/**
 *
 */
@Slf4j
@Primary
@Component
@Qualifier("ec")
@RequiredArgsConstructor
public class ECGenerator implements JwksKeyPairGenerator {
    private final KeyPairMapper keyMapper;

    /**
     *
     * @return
     * @throws NoSuchAlgorithmException
     */
    public KeyPair generateKeyPair() throws NoSuchAlgorithmException {
        log.info("Generating elliptic curve jwk key pair");

        EllipticCurve ellipticCurve = new EllipticCurve(
                new ECFieldFp(
                        new BigInteger("115792089210356248762697446949407573530086143415290314195533631308867097853951")),
                new BigInteger("115792089210356248762697446949407573530086143415290314195533631308867097853948"),
                new BigInteger("41058363725152142129326129780047268409114441015993725554835256314039467401291"));
        ECPoint ecPoint = new ECPoint(
                new BigInteger("48439561293906451759052585252797914202762949526041747995844080717082404635286"),
                new BigInteger("36134250956749795798585127919587881956611106672985015071877198253568414405109"));
        ECParameterSpec ecParameterSpec = new ECParameterSpec(
                ellipticCurve,
                ecPoint,
                new BigInteger("115792089210356248762697446949407573529996955224135760342422259061068512044369"),
                1);

        KeyPair keyPair;

        try {
            java.security.KeyPairGenerator keyPairGenerator = java.security.KeyPairGenerator.getInstance("EC");
            keyPairGenerator.initialize(ecParameterSpec);
            keyPair = keyPairGenerator.generateKeyPair();
        } catch (Exception ex) {
            log.error("Could not generate a jwks key pair", ex);
            throw new NoSuchAlgorithmException(ex);
        }

        return keyPair;
    }

    /**
     *
     * @param id
     * @param publicKey
     * @param privateKey
     * @return
     */
    public JWK buildKey(String id, PublicKey publicKey, PrivateKey privateKey) {
        ECPublicKey ecPublicKey = (ECPublicKey) publicKey;
        ECPrivateKey ecPrivateKey = (ECPrivateKey) privateKey;
        Curve curve = Curve.forECParameterSpec(ecPublicKey.getParams());
        return new ECKey.Builder(curve, ecPublicKey)
                .privateKey(ecPrivateKey)
                .keyID(id)
                .build();
    }

    /**
     *
     * @param id
     * @param publicKey
     * @param privateKey
     * @return
     * @throws NoSuchAlgorithmException
     * @throws InvalidKeySpecException
     */
    public JWK buildKey(String id, String publicKey, String privateKey) throws NoSuchAlgorithmException, InvalidKeySpecException {
        ECPublicKey ecPublicKey = (ECPublicKey) keyMapper.toPublicKey(publicKey, "EC");
        ECPrivateKey ecPrivateKey = (ECPrivateKey) keyMapper.toPrivateKey(privateKey, "EC");
        Curve curve = Curve.forECParameterSpec(ecPublicKey.getParams());
        return new ECKey.Builder(curve, ecPublicKey)
                .privateKey(ecPrivateKey)
                .keyID(id)
                .build();
    }

    /**
     *
     * @return
     */
    public KeyPairType type() {
        return KeyPairType.EC;
    }
}
