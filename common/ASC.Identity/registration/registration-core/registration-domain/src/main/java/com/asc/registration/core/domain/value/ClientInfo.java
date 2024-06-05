package com.asc.registration.core.domain.value;

/**
 * ClientInfo is a value object that holds basic information about a client. It contains the
 * client's name, description, and logo.
 *
 * @param name the name of the client
 * @param description a brief description of the client
 * @param logo the URL or path to the client's logo
 */
public record ClientInfo(String name, String description, String logo) {}
