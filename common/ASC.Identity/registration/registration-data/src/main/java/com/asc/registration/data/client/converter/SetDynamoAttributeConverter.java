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
