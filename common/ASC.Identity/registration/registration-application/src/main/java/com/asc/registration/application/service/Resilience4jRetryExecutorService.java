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

package com.asc.registration.application.service;

import com.asc.registration.service.ports.output.resilience.RetryExecutor;
import io.github.resilience4j.retry.Retry;
import io.github.resilience4j.retry.RetryRegistry;
import java.util.function.Supplier;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
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
    if (catchType.isInstance(e)) {
      log.warn("Retry exhausted for operation, throwing domain exception", e);
      throw exceptionSupplier.get();
    }

    if (e instanceof RuntimeException re) throw re;

    throw new RuntimeException(e);
  }
}
