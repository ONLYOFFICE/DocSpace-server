/**
 *
 */
package com.onlyoffice.authorization.api.extensions.validators;

import com.onlyoffice.authorization.api.extensions.annotations.URLCollection;
import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;

import java.util.Collection;
import java.util.regex.Pattern;

/**
 *
 */
public class UrlCollectionValidator implements ConstraintValidator<URLCollection, Collection<String>> {
    private Pattern pattern = Pattern.compile("https?:\\/\\/(www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_\\+.~#?&//=]*)");
    @Override
    public boolean isValid(Collection<String> urls, ConstraintValidatorContext context) {
        return urls != null && urls.size() > 0 && urls.stream()
                .allMatch(url -> pattern.matcher(url).matches());
    }
}