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
