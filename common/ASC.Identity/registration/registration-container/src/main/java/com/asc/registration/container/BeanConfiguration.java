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
