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

package com.asc.authorization.application.security.error;

import lombok.Getter;

/** Enum representing various authentication errors. */
@Getter
public enum AuthenticationError {

  /** Error indicating an issue with retrieving ASC data. */
  ASC_RETRIEVAL_ERROR("asc_retrieval_error"),

  /** Error indicating that the authentication method is not supported. */
  AUTHENTICATION_NOT_SUPPORTED_ERROR("authentication_not_supported_error"),

  /** Error indicating that the client is disabled. */
  CLIENT_DISABLED_ERROR("client_disabled_error"),

  /** Error indicating that the client was not found. */
  CLIENT_NOT_FOUND_ERROR("client_not_found_error"),

  /** Error indicating that the client does not have permission. */
  CLIENT_PERMISSION_DENIED_ERROR("client_permission_denied_error"),

  /** Error indicating that the redirect URI is invalid. */
  INVALID_REDIRECT_URI_ERROR("invalid_redirect_uri_error"),

  /** Error indicating that the ASC cookie is missing. */
  MISSING_ASC_COOKIE_ERROR("missing_asc_cookie_error"),

  /** Error indicating that the client ID is missing. */
  MISSING_CLIENT_ID_ERROR("missing_client_id_error"),

  /** Error indicating that something went wrong during the authentication process. */
  SOMETHING_WENT_WRONG_ERROR("something_went_wrong_error");

  /** The error code associated with the authentication error. */
  private final String code;

  /**
   * Constructs an AuthenticationError with the specified error code.
   *
   * @param code the error code
   */
  AuthenticationError(String code) {
    this.code = code;
  }
}
