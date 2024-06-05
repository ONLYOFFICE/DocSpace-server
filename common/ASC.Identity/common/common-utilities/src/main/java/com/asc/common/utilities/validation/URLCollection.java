package com.asc.common.utilities.validation;

import jakarta.validation.Constraint;
import jakarta.validation.Payload;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/** The URLCollection annotation is used to validate a collection containing URLs. */
@Retention(RetentionPolicy.RUNTIME)
@Target({ElementType.METHOD, ElementType.FIELD})
@Constraint(validatedBy = URLCollectionValidator.class)
public @interface URLCollection {

  /**
   * Specifies the default error message when the validation fails.
   *
   * @return the default error message
   */
  String message() default "url collection has invalid entries";

  /**
   * Specifies the validation groups this constraint belongs to.
   *
   * @return the validation groups
   */
  Class<?>[] groups() default {};

  /**
   * Specifies the payload associated with the constraint.
   *
   * @return the payload classes
   */
  Class<? extends Payload>[] payload() default {};
}
