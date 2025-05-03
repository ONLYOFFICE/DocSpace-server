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
package com.asc.transfer.configuration;

import lombok.Data;
import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration properties for Spring Batch processing.
 *
 * <p>This class binds properties with the prefix {@code spring.batch.processing} from external
 * configuration sources (such as application.properties or application.yml) to configure batch
 * processing settings in the application.
 *
 * <p>Default values:
 *
 * <ul>
 *   <li>{@code pageSize} - Number of items to process per page (default is 100).
 *   <li>{@code batchSize} - Number of items to process per batch (default is 100).
 * </ul>
 *
 * <p>Example configuration:
 *
 * <pre>
 * spring.batch.processing.pageSize=100
 * spring.batch.processing.batchSize=100
 * </pre>
 */
@Configuration
@ConfigurationProperties(prefix = "spring.batch.processing")
@Data
@Getter
@Setter
public class BatchProcessingConfiguration {

  /** The number of items to process in each page during batch processing. Defaults to 100. */
  private int pageSize = 100;

  /** The number of items to process in each batch. Defaults to 100. */
  private int batchSize = 100;
}
