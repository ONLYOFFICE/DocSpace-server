package com.asc.authorization.application.configuration;

import javax.sql.DataSource;
import net.javacrumbs.shedlock.core.LockProvider;
import net.javacrumbs.shedlock.provider.jdbctemplate.JdbcTemplateLockProvider;
import net.javacrumbs.shedlock.spring.annotation.EnableSchedulerLock;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

/**
 * Configuration class for setting up ShedLock for scheduling key rotation tasks. Enables scheduler
 * locking with default lock times.
 */
@Configuration
@EnableSchedulerLock(defaultLockAtLeastFor = "PT10s", defaultLockAtMostFor = "PT20S")
public class KeyRotationSchedulerConfiguration {

  /**
   * Creates a {@link LockProvider} bean for managing distributed locks using ShedLock with JDBC.
   *
   * @param dataSource the data source used for JDBC operations
   * @return the configured {@link LockProvider}
   */
  @Bean
  public LockProvider lockProvider(DataSource dataSource) {
    return new JdbcTemplateLockProvider(
        JdbcTemplateLockProvider.Configuration.builder()
            .withJdbcTemplate(new JdbcTemplate(dataSource))
            .withTableName("identity_shedlock")
            .usingDbTime()
            .build());
  }
}
