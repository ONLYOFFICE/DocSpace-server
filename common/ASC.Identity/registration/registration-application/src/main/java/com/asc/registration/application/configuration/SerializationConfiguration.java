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

package com.asc.registration.application.configuration;

import org.springframework.boot.jackson.autoconfigure.JsonMapperBuilderCustomizer;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import tools.jackson.databind.DeserializationFeature;
import tools.jackson.module.blackbird.BlackbirdModule;

/**
 * Configuration class for customizing JSON serialization and deserialization behavior.
 *
 * <p>This configuration applies application-wide Jackson ObjectMapper settings to enhance
 * performance and adjust deserialization behavior for handling null values in primitives.
 */
@Configuration
public class SerializationConfiguration {

  /**
   * Customizes the Jackson ObjectMapper builder with performance optimizations and deserialization
   * settings.
   *
   * <p>Applies the following customizations:
   *
   * <ul>
   *   <li>Registers the Blackbird module for improved serialization/deserialization performance
   *       through bytecode generation
   *   <li>Disables {@link DeserializationFeature#FAIL_ON_NULL_FOR_PRIMITIVES} to allow null values
   *       to be deserialized as default primitive values (e.g., null → 0 for int)
   * </ul>
   *
   * @return a customizer that configures the Jackson ObjectMapper builder
   */
  @Bean
  JsonMapperBuilderCustomizer jacksonCustomizer() {
    return builder ->
        builder
            .addModule(new BlackbirdModule())
            .disable(DeserializationFeature.FAIL_ON_NULL_FOR_PRIMITIVES);
  }
}
