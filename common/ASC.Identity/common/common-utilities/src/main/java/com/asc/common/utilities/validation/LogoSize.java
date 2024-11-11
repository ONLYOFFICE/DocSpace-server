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
