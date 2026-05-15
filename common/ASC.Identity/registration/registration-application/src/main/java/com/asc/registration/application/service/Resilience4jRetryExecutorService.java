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

package com.asc.registration.application.service;

import com.asc.registration.core.domain.exception.OptimisticLockingException;
import com.asc.registration.service.ports.output.resilience.RetryExecutor;
import io.github.resilience4j.retry.Retry;
import io.github.resilience4j.retry.RetryRegistry;
import java.util.function.Supplier;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.dao.OptimisticLockingFailureException;
import org.springframework.stereotype.Component;

/**
 * Resilience4j-based implementation of {@link RetryExecutor}.
 *
 * <p>This implementation uses the Resilience4j library to provide retry functionality with
 * configurable backoff strategies. The retry configuration is obtained from the {@link
 * RetryRegistry} using the provided configuration name.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class Resilience4jRetryExecutorService implements RetryExecutor {
  private final RetryRegistry retryRegistry;

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
  public <T> T executeWithRetry(
      String retryName,
      Supplier<T> operation,
      Class<? extends Exception> catchType,
      Supplier<? extends RuntimeException> exceptionSupplier) {
    var retry = retryRegistry.retry(retryName);
    try {
      return Retry.decorateSupplier(retry, operation).get();
    } catch (Exception e) {
      return handleException(e, catchType, exceptionSupplier);
    }
  }

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
  public void executeWithRetry(
      String retryName,
      Runnable operation,
      Class<? extends Exception> catchType,
      Supplier<? extends RuntimeException> exceptionSupplier) {
    var retry = retryRegistry.retry(retryName);
    try {
      Retry.decorateRunnable(retry, operation).run();
    } catch (Exception e) {
      handleException(e, catchType, exceptionSupplier);
    }
  }

  private <T> T handleException(
      Exception e,
      Class<? extends Exception> catchType,
      Supplier<? extends RuntimeException> exceptionSupplier) {
    var translatedException = translateException(e);

    if (catchType.isInstance(translatedException)) {
      log.warn("Retry exhausted for operation, throwing domain exception", e);
      throw exceptionSupplier.get();
    }

    if (translatedException instanceof RuntimeException re) throw re;

    throw new RuntimeException(translatedException);
  }

  private Exception translateException(Exception e) {
    if (e instanceof OptimisticLockingFailureException)
      return new OptimisticLockingException(
          "Optimistic locking failure occurred due to concurrent access", e);
    return e;
  }
}
