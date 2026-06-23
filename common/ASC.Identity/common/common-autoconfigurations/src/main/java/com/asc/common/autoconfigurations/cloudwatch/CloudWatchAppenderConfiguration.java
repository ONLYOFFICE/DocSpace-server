// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.common.autoconfigurations.cloudwatch;

import ch.qos.logback.classic.LoggerContext;
import org.slf4j.LoggerFactory;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Profile;

/**
 * Configuration class for setting up the CloudWatchAppender. This class is enabled only if the
 * property `logging.cloudwatch.enabled` is set to `true`.
 */
@Configuration
@Profile("saas")
@EnableConfigurationProperties(CloudWatchAppenderProperties.class)
@ConditionalOnProperty(prefix = "logging.cloudwatch", name = "enabled", havingValue = "true")
public class CloudWatchAppenderConfiguration {

  private final CloudWatchAppenderProperties properties;

  /**
   * Constructs a new CloudWatchAppenderConfiguration with the specified properties.
   *
   * @param properties the properties for configuring the CloudWatchAppender
   */
  public CloudWatchAppenderConfiguration(CloudWatchAppenderProperties properties) {
    this.properties = properties;
  }

  /**
   * Creates and configures a CloudWatchAppender bean. Validates the required properties before
   * creating the appender.
   *
   * @return the configured CloudWatchAppender bean
   */
  @Bean
  public CloudWatchAppender cloudWatchAppender() {
    validateProperties();

    var loggerContext = (LoggerContext) LoggerFactory.getILoggerFactory();
    return getCloudWatchAppender(loggerContext);
  }

  /**
   * Validates the required properties for the CloudWatchAppender. Throws an IllegalStateException
   * if any required property is missing.
   */
  private void validateProperties() {
    if (properties.isEnabled()) {
      if (properties.getLogGroupName() == null || properties.getLogGroupName().isEmpty())
        throw new IllegalStateException(
            "logGroupName must be set when CloudWatch logging is enabled.");
      if (properties.getRegion() == null || properties.getRegion().isEmpty())
        throw new IllegalStateException("region must be set when CloudWatch logging is enabled.");
      if (properties.isUseLocalstack()
          && (properties.getEndpoint() == null || properties.getEndpoint().isBlank()))
        throw new IllegalStateException(
            "it is mandatory to specify endpoint when LocalStack is enabled");
    }
  }

  /**
   * Creates and configures a CloudWatchAppender instance.
   *
   * @param loggerContext the LoggerContext to associate with the appender
   * @return the configured CloudWatchAppender instance
   */
  private CloudWatchAppender getCloudWatchAppender(LoggerContext loggerContext) {
    // Note: The encoder should be set in logback.xml
    // We don't set an encoder here to allow the one from logback.xml to be used

    // Don't automatically start the appender - it will be started by Logback
    // when it's configured in logback.xml
    var appender = new CloudWatchAppender();
    appender.setContext(loggerContext);
    appender.setEnabled(properties.isEnabled());
    appender.setLogGroupName(properties.getLogGroupName());
    appender.setEndpoint(properties.getEndpoint());
    appender.setUseLocalstack(properties.isUseLocalstack());
    appender.setUseInstanceProfileProvider(properties.isUseInstanceProfileProvider());
    appender.setRegion(properties.getRegion());
    appender.setAccessKey(properties.getAccessKey());
    appender.setSecretKey(properties.getSecretKey());
    appender.setBatchSize(properties.getBatchSize());
    return appender;
  }
}
