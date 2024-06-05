package com.asc.authorization.container;

import com.asc.common.utilities.cipher.AesEncryptionService;
import com.asc.common.utilities.cipher.EncryptionService;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration class for defining beans related to encryption services.
 *
 * <p>This configuration ensures that an {@link EncryptionService} bean is available in the
 * application context. If no other {@link EncryptionService} bean is defined, a default {@link
 * AesEncryptionService} bean is created.
 */
@Configuration
public class BeanConfiguration {

  /**
   * Creates a default {@link EncryptionService} bean if no other {@link EncryptionService} bean is
   * present in the context.
   *
   * <p>This method uses the provided application secret to initialize the {@link
   * AesEncryptionService}.
   *
   * @param secret the secret key used for encryption and decryption, injected from the application
   *     properties
   * @return an instance of {@link AesEncryptionService} initialized with the provided secret
   */
  @Bean
  @ConditionalOnMissingBean
  public EncryptionService encryptionService(@Value("${application.secret}") String secret) {
    return new AesEncryptionService(secret);
  }
}
