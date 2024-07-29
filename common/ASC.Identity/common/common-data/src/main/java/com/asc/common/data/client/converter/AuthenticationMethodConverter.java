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

package com.asc.common.data.client.converter;

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
