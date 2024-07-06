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

package com.asc.authorization.application.configuration.security;

import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.context.annotation.Configuration;

/** Configuration properties for anonymous filter security settings. */
@Getter
@Setter
@Configuration
@ConfigurationProperties(prefix = "security.auth")
public class AnonymousFilterSecurityConfigurationProperties {
  /** The name of the authentication cookie. Default value is "asc_auth_key". */
  private String authCookieName = "asc_auth_key";

  /**
   * The name of the redirect cookie back to the beginning of oauth flow. Default value is
   * "x-redirect-authorization-uri".
   */
  private String redirectAuthorizationCookie = "x-redirect-authorization-uri";

  /** The parameter name for the client ID. Default value is "client_id". */
  private String clientIdParam = "client_id";

  /** The name of the header to indicate that redirect must be performed via the redirectHeader */
  private String disableRedirectHeader = "X-Disable-Redirect";

  /** The name of the redirect header. Default value is "X-Redirect-URI". */
  private String redirectHeader = "X-Redirect-URI";
}
