// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.application.configuration.serialization;

import com.asc.registration.core.domain.entity.Client;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.jetbrains.annotations.NotNull;
import org.springframework.data.redis.serializer.RedisSerializer;
import org.springframework.data.redis.serializer.SerializationException;

/**
 * Custom serializer that uses a configured ObjectMapper with Client deserializer.
 *
 * <p>This serializer specifically handles {@link Client} objects and uses the custom {@link
 * ClientDeserializer} for deserialization.
 */
public class ClientSerializer implements RedisSerializer<Object> {
  private final ObjectMapper objectMapper;

  public ClientSerializer(ObjectMapper objectMapper) {
    this.objectMapper = objectMapper;
  }

  @NotNull
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
      return objectMapper.readValue(bytes, Client.class);
    } catch (Exception e) {
      throw new SerializationException("Could not deserialize: " + e.getMessage(), e);
    }
  }
}
