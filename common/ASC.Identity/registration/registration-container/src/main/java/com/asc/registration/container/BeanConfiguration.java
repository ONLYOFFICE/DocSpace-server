package com.asc.registration.container;

import com.asc.common.utilities.crypto.AesEncryptionService;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.CoreClientDomainService;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnMissingBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/** This class provides configuration for beans used in the application. */
@Configuration
public class BeanConfiguration {

  /**
   * This method creates a ClientDomainService bean.
   *
   * @return A new instance of CoreClientDomainService.
   */
  @Bean
  public ClientDomainService clientDomainService() {
    return new CoreClientDomainService();
  }

  /**
   * This method creates an EncryptionService bean. It uses the AesEncryptionService implementation
   * and injects the application secret as a constructor argument. If an EncryptionService bean has
   * already been created, this method will not be called.
   *
   * @param secret The application secret.
   * @return A new instance of AesEncryptionService.
   */
  @Bean
  @ConditionalOnMissingBean
  public EncryptionService encryptionService(@Value("${application.secret}") String secret) {
    return new AesEncryptionService(secret);
  }
}
