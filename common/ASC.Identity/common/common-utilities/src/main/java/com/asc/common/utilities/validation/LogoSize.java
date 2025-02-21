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

package com.asc.common.utilities.validation;

import jakarta.validation.Constraint;
import jakarta.validation.Payload;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/**
 * Annotation to validate the size of a base64-encoded logo string. Ensures that the logo does not
 * exceed a specified byte size and character length.
 */
@Constraint(validatedBy = LogoSizeValidator.class)
@Target({ElementType.FIELD, ElementType.PARAMETER})
@Retention(RetentionPolicy.RUNTIME)
public @interface LogoSize {

  /**
   * The error message to be returned if validation fails.
   *
   * @return the error message
   */
  String message() default "Logo size exceeds the maximum allowed size";

  /**
   * The maximum allowed size of the logo in bytes.
   *
   * @return the maximum size in bytes
   */
  long maxBytes() default 5242888;

  /**
   * The maximum allowed length of the base64-encoded string.
   *
   * @return the maximum character length
   */
  int maxLength() default 5600000;

  /**
   * Groups for categorizing the validation.
   *
   * @return the validation groups
   */
  Class<?>[] groups() default {};

  /**
   * Payload for carrying metadata information during validation.
   *
   * @return the payload class
   */
  Class<? extends Payload>[] payload() default {};
}
