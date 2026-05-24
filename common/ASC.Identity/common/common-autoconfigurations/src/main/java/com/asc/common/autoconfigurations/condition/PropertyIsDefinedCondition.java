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

package com.asc.common.autoconfigurations.condition;

import ch.qos.logback.core.boolex.PropertyConditionBase;

/**
 * A Logback property condition that evaluates to true if a specified property is defined.
 *
 * <p>This class extends Logback's PropertyConditionBase and is used with the {@code <condition>}
 * element in logback configuration files to conditionally include configuration based on whether a
 * property exists.
 *
 * <p>Example usage in logback.xml:
 *
 * <pre>{@code
 * <condition class="com.asc.common.autoconfigurations.condition.PropertyIsDefinedCondition">
 *   <key>LOG_FILE_PATH</key>
 * </condition>
 * <if>
 *   <then>
 *     <!-- Configuration when LOG_FILE_PATH is defined -->
 *   </then>
 * </if>
 * }</pre>
 */
public class PropertyIsDefinedCondition extends PropertyConditionBase {
  private String key;

  /**
   * Sets the property key to check for existence.
   *
   * @param key the property key to evaluate
   */
  public void setKey(String key) {
    this.key = key;
  }

  /**
   * Evaluates whether the configured property is defined.
   *
   * @return {@code true} if the property is defined, {@code false} otherwise
   */
  @Override
  public boolean evaluate() {
    return isDefined(key);
  }
}
