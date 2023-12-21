/**
 *
 */
package com.onlyoffice.authorization.api.extensions.validators;

import com.onlyoffice.authorization.api.extensions.annotations.URLCollection;
import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;
import lombok.extern.slf4j.Slf4j;

import java.util.Collection;
import java.util.regex.Pattern;

/**
 *
 */
@Slf4j
public class URLCollectionValidator implements ConstraintValidator<URLCollection, Collection<String>> {
    private Pattern pattern = Pattern.compile("https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)");

    /**
     *
     * @param urls
     * @param context
     * @return
     */
    public boolean isValid(Collection<String> urls, ConstraintValidatorContext context) {
        log.debug("Validating url collection", String.join(",", urls));
        return urls != null && urls.size() > 0 && urls.stream()
                .allMatch(url -> pattern.matcher(url).matches());
    }
}