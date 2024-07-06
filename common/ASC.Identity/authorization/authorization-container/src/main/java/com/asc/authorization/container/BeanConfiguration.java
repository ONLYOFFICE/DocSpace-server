// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.authorization.container;

import com.asc.common.utilities.crypto.AesEncryptionService;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.common.utilities.crypto.HashingService;
import com.asc.common.utilities.crypto.SHA256HashingService;
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

  @Bean
  @ConditionalOnMissingBean
  public HashingService hashingService() {
    return new SHA256HashingService();
  }
}
