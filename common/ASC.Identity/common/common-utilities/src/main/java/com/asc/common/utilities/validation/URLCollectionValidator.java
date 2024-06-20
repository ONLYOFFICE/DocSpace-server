package com.asc.common.utilities.validation;

import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;
import java.util.Collection;
import java.util.regex.Pattern;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

/**
 * The URLCollectionValidator class is responsible for validating a collection of URLs based on the
 * specified pattern.
 */
@Slf4j
@Component
public class URLCollectionValidator
    implements ConstraintValidator<URLCollection, Collection<String>> {
  private Pattern pattern =
      Pattern.compile(
          "https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)");

  /**
   * Validates the collection of URLs against the specified pattern.
   *
   * @param urls the collection of URLs to be validated
   * @param context the validation context
   * @return true if all URLs are valid based on the pattern, false otherwise
   */
  public boolean isValid(Collection<String> urls, ConstraintValidatorContext context) {
    if (urls == null) return false;
    for (String url : urls) {
      log.debug("Validating URL: {}", url);
      if (url == null || url.isEmpty() || !pattern.matcher(url).matches()) {
        log.warn("Invalid URL detected: {}", url);
        return false;
      }
    }
    return true;
  }
}
