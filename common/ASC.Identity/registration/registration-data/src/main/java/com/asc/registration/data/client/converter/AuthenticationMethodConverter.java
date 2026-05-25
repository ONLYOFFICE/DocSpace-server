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

package com.asc.registration.data.client.converter;

import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import jakarta.persistence.AttributeConverter;
import jakarta.persistence.Converter;

/**
 * Converter class for transforming {@link AuthenticationMethod} enum values to their string
 * representations in the database and vice versa.
 *
 * <p>This converter is automatically applied to all entity attributes of type {@link
 * AuthenticationMethod} due to the {@code autoApply = true} setting.
 */
@Converter(autoApply = true)
public class AuthenticationMethodConverter
    implements AttributeConverter<AuthenticationMethod, String> {

  /**
   * Converts the {@link AuthenticationMethod} attribute to its string representation for storage in
   * the database.
   *
   * @param attribute the {@link AuthenticationMethod} enum value
   * @return the string representation of the enum value, or {@code null} if the attribute is {@code
   *     null}
   */
  public String convertToDatabaseColumn(AuthenticationMethod attribute) {
    if (attribute == null) return null;
    return attribute.getMethod();
  }

  /**
   * Converts the string representation of an authentication method from the database to the
   * corresponding {@link AuthenticationMethod} enum value.
   *
   * @param dbData the string representation of the authentication method from the database
   * @return the corresponding {@link AuthenticationMethod} enum value, or {@code null} if the
   *     dbData is {@code null}
   */
  public AuthenticationMethod convertToEntityAttribute(String dbData) {
    if (dbData == null) return null;
    return AuthenticationMethod.fromMethod(dbData);
  }
}
