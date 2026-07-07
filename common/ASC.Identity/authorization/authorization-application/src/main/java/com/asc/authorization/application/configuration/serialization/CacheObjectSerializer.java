// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.authorization.application.configuration.serialization;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.data.redis.serializer.RedisSerializer;
import org.springframework.data.redis.serializer.SerializationException;
import org.springframework.lang.NonNull;

/**
 * Custom Redis serializer for cache objects using Jackson ObjectMapper.
 *
 * <p>This serializer uses a configured ObjectMapper with type information to properly serialize and
 * deserialize objects for Redis cache, ensuring type safety during deserialization.
 */
public class CacheObjectSerializer implements RedisSerializer<Object> {
  private final ObjectMapper objectMapper;

  public CacheObjectSerializer(ObjectMapper objectMapper) {
    this.objectMapper = objectMapper;
  }

  @NonNull
  public byte[] serialize(Object value) throws SerializationException {
    if (value == null) return new byte[0];

    try {
      return objectMapper.writeValueAsBytes(value);
    } catch (Exception e) {
      throw new SerializationException("Could not serialize: " + e.getMessage(), e);
    }
  }

  public Object deserialize(byte[] bytes) throws SerializationException {
    if (bytes == null || bytes.length == 0) return null;

    try {
      return objectMapper.readValue(bytes, Object.class);
    } catch (Exception e) {
      throw new SerializationException("Could not deserialize: " + e.getMessage(), e);
    }
  }
}
