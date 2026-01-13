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

package com.asc.common.autoconfigurations.condition;

import ch.qos.logback.core.boolex.PropertyConditionBase;

/**
 * A Logback property condition that evaluates to true if a specified property is NOT defined.
 *
 * <p>This class extends Logback's PropertyConditionBase and is used with the {@code <condition>}
 * element in logback configuration files to conditionally include configuration based on whether a
 * property does not exist.
 *
 * <p>Example usage in logback.xml:
 *
 * <pre>{@code
 * <condition class="com.asc.common.autoconfigurations.condition.PropertyIsNotDefinedCondition">
 *   <key>LOG_FILE_PATH</key>
 * </condition>
 * <if>
 *   <then>
 *     <!-- Configuration when LOG_FILE_PATH is NOT defined -->
 *   </then>
 * </if>
 * }</pre>
 */
public class PropertyIsNotDefinedCondition extends PropertyConditionBase {
  private String key;

  /**
   * Sets the property key to check for non-existence.
   *
   * @param key the property key to evaluate
   */
  public void setKey(String key) {
    this.key = key;
  }

  /**
   * Evaluates whether the configured property is NOT defined.
   *
   * @return {@code true} if the property is not defined, {@code false} otherwise
   */
  @Override
  public boolean evaluate() {
    return !isDefined(key);
  }
}
