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

package com.asc.registration.data.client.converter;

import java.util.HashSet;
import java.util.Set;
import software.amazon.awssdk.enhanced.dynamodb.AttributeConverter;
import software.amazon.awssdk.enhanced.dynamodb.AttributeValueType;
import software.amazon.awssdk.enhanced.dynamodb.EnhancedType;
import software.amazon.awssdk.services.dynamodb.model.AttributeValue;

/**
 * A converter for transforming between a {@link Set} of {@link String} and DynamoDB {@link
 * AttributeValue}. This class implements the {@link AttributeConverter} interface to facilitate the
 * conversion process.
 */
public class SetDynamoAttributeConverter implements AttributeConverter<Set<String>> {

  /**
   * Transforms a set of strings into a DynamoDB {@link AttributeValue}.
   *
   * @param input the set of strings to be converted
   * @return an {@link AttributeValue} containing the set of strings
   */
  public AttributeValue transformFrom(Set<String> input) {
    return AttributeValue.builder().ss(input).build();
  }

  /**
   * Transforms a DynamoDB {@link AttributeValue} into a set of strings.
   *
   * @param input the {@link AttributeValue} to be converted
   * @return a set of strings extracted from the {@link AttributeValue}
   */
  public Set<String> transformTo(AttributeValue input) {
    try {
      return new HashSet<>(input.ss());
    } catch (Exception e) {
      return Set.of();
    }
  }

  /**
   * Returns the enhanced type for a set of strings.
   *
   * @return an {@link EnhancedType} representing a set of strings
   */
  public EnhancedType<Set<String>> type() {
    return EnhancedType.setOf(String.class);
  }

  /**
   * Returns the attribute value type for a set of strings.
   *
   * @return the {@link AttributeValueType} for a set of strings
   */
  public AttributeValueType attributeValueType() {
    return AttributeValueType.SS;
  }
}
