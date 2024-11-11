package com.asc.common.utilities.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;
import java.util.Base64;

/**
 * Validator for the {@link LogoSize} annotation. Ensures that a base64-encoded string representing
 * a logo does not exceed the specified byte size and character length.
 */
public class LogoSizeValidator implements ConstraintValidator<LogoSize, String> {
  private int maxLength;
  private long maxBytes;

  /**
   * Initializes the validator with the parameters defined in the {@link LogoSize} annotation.
   *
   * @param constraintAnnotation the annotation instance for a given constraint declaration
   */
  public void initialize(LogoSize constraintAnnotation) {
    this.maxLength = constraintAnnotation.maxLength();
    this.maxBytes = constraintAnnotation.maxBytes();
  }

  /**
   * Validates that the provided base64-encoded logo string does not exceed the specified byte size
   * and character length.
   *
   * @param value the base64-encoded string to validate
   * @param context the context in which the constraint is evaluated
   * @return {@code true} if the string is valid; {@code false} otherwise
   */
  public boolean isValid(String value, ConstraintValidatorContext context) {
    if (value == null || value.isEmpty()) {
      return true;
    }

    // Early rejection for excessively long base64 strings
    if (value.length() > maxLength) {
      return false;
    }

    try {
      // Remove base64 metadata prefix if present (e.g., "data:image/png;base64,")
      int base64Index = value.indexOf(",");
      if (base64Index >= 0) value = value.substring(base64Index + 1);

      byte[] decodedBytes = Base64.getDecoder().decode(value);
      return decodedBytes.length <= maxBytes;
    } catch (IllegalArgumentException e) {
      return false;
    }
  }
}
