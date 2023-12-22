package com.onlyoffice.authorization.web.security.crypto.aes;

import com.onlyoffice.authorization.configuration.ApplicationConfiguration;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.context.annotation.Profile;
import org.springframework.stereotype.Component;

import javax.crypto.Cipher;
import javax.crypto.NoSuchPaddingException;
import javax.crypto.SecretKey;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.GCMParameterSpec;
import javax.crypto.spec.PBEKeySpec;
import javax.crypto.spec.SecretKeySpec;
import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.nio.charset.StandardCharsets;
import java.security.InvalidAlgorithmParameterException;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.security.SecureRandom;
import java.security.spec.InvalidKeySpecException;
import java.util.Arrays;
import java.util.Base64;

@Slf4j
@Component
@RequiredArgsConstructor
@Profile(value = {"prod", "production", "p", "testing", "t", "test"})
public class AesGcmCipher implements com.onlyoffice.authorization.web.security.crypto.aes.Cipher {
    private static final String ALGORITHM = "AES/GCM/NoPadding";
    private static final String FACTORY_INSTANCE = "PBKDF2WithHmacSHA256";
    private static final int TAG_LENGTH_BIT = 128;
    private static final int IV_LENGTH_BYTE = 12;
    private static final int SALT_LENGTH_BYTE = 16;
    private static final String ALGORITHM_TYPE = "AES";
    private static final int KEY_LENGTH = 128;
    private static final int ITERATION_COUNT = 1200; // Min 1000
    private static final Charset UTF_8 = StandardCharsets.UTF_8;

    private final ApplicationConfiguration configuration;

    /**
     *
     * @param length
     * @return
     */
    private byte[] getRandomNonce(int length) {
        var nonce = new byte[length];
        new SecureRandom().nextBytes(nonce);

        MDC.put("nonce", Arrays.toString(nonce));
        log.debug("Generating random nonce");
        MDC.clear();

        return nonce;
    }

    /**
     *
     * @param password
     * @param salt
     * @return
     * @throws NoSuchAlgorithmException
     * @throws InvalidKeySpecException
     */
    private SecretKey getSecretKey(String password, byte[] salt)
            throws NoSuchAlgorithmException, InvalidKeySpecException {
        MDC.put("password", password);
        log.debug("Generating secret key");
        MDC.clear();

        var spec = new PBEKeySpec(password.toCharArray(), salt, ITERATION_COUNT, KEY_LENGTH);
        var factory = SecretKeyFactory.getInstance(FACTORY_INSTANCE);
        return new SecretKeySpec(factory.generateSecret(spec).getEncoded(), ALGORITHM_TYPE);
    }

    /**
     *
     * @param mode
     * @param secretKey
     * @param iv
     * @return
     * @throws InvalidKeyException
     * @throws InvalidAlgorithmParameterException
     * @throws NoSuchPaddingException
     * @throws NoSuchAlgorithmException
     */
    private Cipher initCipher(int mode, SecretKey secretKey, byte[] iv)
            throws InvalidKeyException, InvalidAlgorithmParameterException,
            NoSuchPaddingException, NoSuchAlgorithmException {
        MDC.put("algorithm", ALGORITHM);
        MDC.put("tag", String.valueOf(TAG_LENGTH_BIT));
        log.debug("Initializing a new cipher");
        MDC.clear();

        var cipher = Cipher.getInstance(ALGORITHM);
        cipher.init(mode, secretKey, new GCMParameterSpec(TAG_LENGTH_BIT, iv));
        return cipher;
    }

    /**
     *
     * @param plainMessage
     * @return
     * @throws Exception
     */
    public String encrypt(String plainMessage) throws Exception {
        MDC.put("plainMessage", plainMessage);
        log.debug("Trying to encrypt plain message");
        MDC.clear();

        var salt = getRandomNonce(SALT_LENGTH_BYTE);
        var secretKey = getSecretKey(configuration.getSecurity().getCipherSecret(), salt);
        var iv = getRandomNonce(IV_LENGTH_BYTE);
        var cipher = initCipher(Cipher.ENCRYPT_MODE, secretKey, iv);
        var encryptedMessageByte = cipher.doFinal(plainMessage.getBytes(UTF_8));
        var cipherByte = ByteBuffer.allocate(iv.length + salt.length + encryptedMessageByte.length)
                .put(iv)
                .put(salt)
                .put(encryptedMessageByte)
                .array();

        var encrypted = Base64.getEncoder().encodeToString(cipherByte);

        MDC.put("plainMessage", plainMessage);
        MDC.put("encryptedMessage", encrypted);
        log.debug("Managed to encrypt plain text message");
        MDC.clear();

        return encrypted;
    }

    /**
     *
     * @param cipherMessage
     * @return
     * @throws Exception
     */
    public String decrypt(String cipherMessage) throws Exception {
        MDC.put("cipherMessage", cipherMessage);
        log.debug("Trying to decrypt cipher message");
        MDC.clear();

        var decodedCipherByte = Base64.getDecoder().decode(cipherMessage.getBytes(UTF_8));
        ByteBuffer byteBuffer = ByteBuffer.wrap(decodedCipherByte);

        var iv = new byte[IV_LENGTH_BYTE];
        byteBuffer.get(iv);

        var salt = new byte[SALT_LENGTH_BYTE];
        byteBuffer.get(salt);

        var encryptedByte = new byte[byteBuffer.remaining()];
        byteBuffer.get(encryptedByte);

        var secretKey = getSecretKey(configuration.getSecurity().getCipherSecret(), salt);
        var cipher = initCipher(Cipher.DECRYPT_MODE, secretKey, iv);

        var decryptedMessageByte = cipher.doFinal(encryptedByte);

        var decrypted = new String(decryptedMessageByte, UTF_8);

        MDC.put("cipherMessage", cipherMessage);
        MDC.put("decryptedMessage", decrypted);
        log.debug("Decrypted cipher message");
        MDC.clear();

        return decrypted;
    }
}