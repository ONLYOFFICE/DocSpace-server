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

package com.asc.registration.service.ports.output.resilience;

import java.util.function.Supplier;

/**
 * Port interface for executing operations with retry logic. This abstraction allows the service
 * layer to remain agnostic of specific retry implementations
 *
 * <p>Implementations should handle retry attempts, backoff strategies, and exception handling based
 * on the configured retry policy.
 */
public interface RetryExecutor {

  /**
   * Executes an operation with retry logic using the specified retry configuration, converting a
   * specific exception type to a domain exception when retries are exhausted.
   *
   * @param <T> the return type of the operation
   * @param retryName the name of the retry configuration to use
   * @param operation the operation to execute with retry support
   * @param catchType the exception type to catch and convert
   * @param exceptionSupplier supplier for the exception to throw when catchType is encountered
   *     after retry exhaustion
   * @return the result of the operation
   * @throws RuntimeException the domain exception if catchType is caught, or the original exception
   *     for other exception types
   */
  <T> T executeWithRetry(
      String retryName,
      Supplier<T> operation,
      Class<? extends Exception> catchType,
      Supplier<? extends RuntimeException> exceptionSupplier);

  /**
   * Executes a void operation with retry logic using the specified retry configuration, converting
   * a specific exception type to a domain exception when retries are exhausted.
   *
   * @param retryName the name of the retry configuration to use
   * @param operation the operation to execute with retry support
   * @param catchType the exception type to catch and convert
   * @param exceptionSupplier supplier for the exception to throw when catchType is encountered
   *     after retry exhaustion
   * @throws RuntimeException the domain exception if catchType is caught, or the original exception
   *     for other exception types
   */
  void executeWithRetry(
      String retryName,
      Runnable operation,
      Class<? extends Exception> catchType,
      Supplier<? extends RuntimeException> exceptionSupplier);
}
