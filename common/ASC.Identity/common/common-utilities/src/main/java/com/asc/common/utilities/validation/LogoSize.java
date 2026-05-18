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
