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

import com.asc.common.utilities.crypto.AesEncryptionService;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.common.utilities.crypto.NoOpEncryptionService;
import jakarta.validation.constraints.NotBlank;
import lombok.Data;
import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.validation.annotation.Validated;

/**
 * Provides configuration for the encryption services used within the application.
 *
 * <p>This configuration class binds properties defined under the prefix {@code
 * spring.application.encryption} in external configuration sources (e.g., application.properties or
 * application.yml) to determine the encryption algorithm and keys used for encryption and
 * decryption operations.
 *
 * <p>Supported encryption types include:
 *
 * <ul>
 *   <li><strong>AES</strong> – Uses AES encryption via {@link AesEncryptionService}.
 *   <li><strong>None</strong> – Uses a no-operation encryption service via {@link
 *       NoOpEncryptionService} when encryption is not enabled.
 * </ul>
 *
 * <p>Example configuration in application.properties:
 *
 * <pre>
 * spring.application.encryption.type=AES
 * spring.application.encryption.secret=yourSecretKey
 * spring.application.encryption.newSecret=yourNewSecretKey
 * </pre>
 */
@Data
@Getter
@Setter
@Validated
@Configuration
@ConfigurationProperties(prefix = "spring.application.encryption")
public class EncryptionServiceConfiguration {

  /**
   * The encryption type to be used.
   *
   * <p>This value must not be null or blank.
   */
  @NotBlank private String type;

  /**
   * The current encryption secret.
   *
   * <p>This value must not be null or blank. It is used for operations that require the current
   * encryption key.
   */
  @NotBlank private String secret;

  /**
   * The new encryption secret.
   *
   * <p>This value must not be null or blank. It is used for operations that require the updated
   * encryption key.
   */
  @NotBlank private String newSecret;

  /**
   * Creates an {@link EncryptionService} bean for encryption operations using the current secret.
   *
   * <p>If the configured encryption type (as specified by {@code
   * spring.application.encryption.type}) is set to "aes" (case-insensitive), an {@link
   * AesEncryptionService} is instantiated with the {@code secret} property. Otherwise, a {@link
   * NoOpEncryptionService} is returned, which performs no encryption.
   *
   * @return an {@link EncryptionService} instance configured with the current encryption secret.
   */
  @Bean
  public EncryptionService fromEncryptionService() {
    if (type.equalsIgnoreCase("aes")) {
      return new AesEncryptionService(secret);
    }
    return new NoOpEncryptionService();
  }

  /**
   * Creates an {@link EncryptionService} bean for encryption operations using the new secret.
   *
   * <p>If the configured encryption type (as specified by {@code
   * spring.application.encryption.type}) is set to "aes" (case-insensitive), an {@link
   * AesEncryptionService} is instantiated with the {@code newSecret} property. Otherwise, a {@link
   * NoOpEncryptionService} is returned, which performs no encryption.
   *
   * @return an {@link EncryptionService} instance configured with the new encryption secret.
   */
  @Bean
  public EncryptionService toEncryptionService() {
    if (type.equalsIgnoreCase("aes")) {
      return new AesEncryptionService(newSecret);
    }
    return new NoOpEncryptionService();
  }
}
