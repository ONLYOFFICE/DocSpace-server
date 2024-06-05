package com.asc.registration.core.domain.value;

import java.util.Set;

/**
 * ClientRedirectInfo is a value object that holds information about the client's redirect URIs,
 * allowed origins, and logout redirect URIs.
 *
 * @param redirectUris the set of redirect URIs for the client
 * @param allowedOrigins the set of allowed origins for CORS (Cross-Origin Resource Sharing)
 * @param logoutRedirectUris the set of URIs to redirect to after logout
 */
public record ClientRedirectInfo(
    Set<String> redirectUris, Set<String> allowedOrigins, Set<String> logoutRedirectUris) {}
