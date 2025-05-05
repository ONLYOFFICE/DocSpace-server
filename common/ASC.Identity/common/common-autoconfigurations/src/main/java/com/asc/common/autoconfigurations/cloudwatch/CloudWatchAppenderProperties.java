// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
