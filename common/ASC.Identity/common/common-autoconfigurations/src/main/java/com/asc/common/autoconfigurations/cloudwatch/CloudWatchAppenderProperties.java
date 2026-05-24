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

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;

/**
 * Properties class for configuring the CloudWatchAppender. This class maps to the properties
 * defined under the `logging.cloudwatch` prefix.
 */
@Getter
@Setter
@ConfigurationProperties(prefix = "logging.cloudwatch")
public class CloudWatchAppenderProperties {

  /** Flag to enable or disable CloudWatch logging. Default value is `false`. */
  private boolean enabled = false;

  /**
   * The name of the log group in CloudWatch. This property is required when CloudWatch logging is
   * enabled.
   */
  private String logGroupName;

  /**
   * The AWS access key for authenticating with CloudWatch. This property is required when
   * CloudWatch logging is enabled.
   */
  private String accessKey;

  /**
   * The AWS secret key for authenticating with CloudWatch. This property is required when
   * CloudWatch logging is enabled.
   */
  private String secretKey;

  /**
   * The AWS region where the CloudWatch logs are stored. This property is required when CloudWatch
   * logging is enabled.
   */
  private String region;

  /** The batch size for sending log events to CloudWatch. Default value is `10`. */
  private int batchSize = 10;

  /**
   * The endpoint for the development CloudWatch service. Default value is `http://localhost:4566`,
   * which is typically used for LocalStack.
   */
  private String endpoint = "http://localhost:4566";

  /**
   * Flag to indicate whether to use LocalStack for CloudWatch logging. Default value is `false`.
   */
  private boolean useLocalstack = false;

  /**
   * Flag to indicate whether to use InstanceProvider for CloudWatch logging. Default value is
   * `false`.
   */
  private boolean useInstanceProfileProvider = false;
}
