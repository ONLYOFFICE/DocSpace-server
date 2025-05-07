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
//

package com.asc.common.autoconfigurations.cloudwatch;

import ch.qos.logback.classic.spi.ILoggingEvent;
import ch.qos.logback.core.UnsynchronizedAppenderBase;
import ch.qos.logback.core.encoder.Encoder;
import java.net.URI;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.concurrent.*;
import lombok.Setter;
import software.amazon.awssdk.auth.credentials.AwsBasicCredentials;
import software.amazon.awssdk.auth.credentials.InstanceProfileCredentialsProvider;
import software.amazon.awssdk.auth.credentials.StaticCredentialsProvider;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.cloudwatchlogs.CloudWatchLogsClient;
import software.amazon.awssdk.services.cloudwatchlogs.model.*;

/**
 * A Logback appender that sends log events to Amazon CloudWatch Logs service.
 *
 * <p>This appender batches log events and sends them periodically to AWS CloudWatch Logs. It
 * handles log group and log stream creation, throttling, and retry logic.
 *
 * <p>Required properties:
 *
 * <ul>
 *   <li>logGroupName - The name of the CloudWatch log group
 *   <li>logStreamName - The name of the CloudWatch log stream
 *   <li>encoder - The encoder to format log events
 * </ul>
 *
 * <p>Optional properties:
 *
 * <ul>
 *   <li>accessKey - AWS access key ID
 *   <li>secretKey - AWS secret access key
 *   <li>region - AWS region (defaults to US_EAST_1)
 *   <li>batchSize - Number of logs to batch before sending (defaults to 10)
 *   <li>enabled - Whether the appender is enabled (defaults to true)
 *   <li>useLocalstack - Whether to use Localstack for local development (defaults to false)
 *   <li>endpoint - Custom endpoint URL for Localstack or other testing services
 * </ul>
 */
@Setter
public class CloudWatchAppender extends UnsynchronizedAppenderBase<ILoggingEvent> {
  private boolean enabled = true;
  private String logStreamName;
  private String logGroupName;
  private String accessKey;
  private String secretKey;
  private String region;
  private int batchSize = 10;
  private String endpoint;
  private boolean useLocalstack = false;
  private boolean useInstanceProfileProvider = false;

  private CloudWatchLogsClient client;
  private Encoder<ILoggingEvent> encoder;

  private static final int MAX_QUEUE_SIZE = 1000;
  private static final int MAX_RETRY_ATTEMPTS = 3;
  private static final int FLUSH_INTERVAL_SECONDS = 1;

  private final BlockingQueue<InputLogEvent> eventQueue = new LinkedBlockingQueue<>(MAX_QUEUE_SIZE);
  private final ScheduledExecutorService scheduler =
      Executors.newScheduledThreadPool(
          1,
          new ThreadFactory() {
            private final ThreadFactory defaultFactory = Executors.defaultThreadFactory();

            public Thread newThread(Runnable r) {
              Thread thread = defaultFactory.newThread(r);
              thread.setName("cloudwatch-appender-" + thread.getName());
              thread.setDaemon(true);
              return thread;
            }
          });

  /**
   * Initializes and starts the appender. Creates the AWS CloudWatch client, ensures log group and
   * stream exist, and schedules periodic flushing of queued events.
   *
   * @throws IllegalStateException if required configuration is missing
   */
  public void start() {
    if (!enabled) return;

    validateConfiguration();
    initializeClient();
    ensureLogGroupAndStreamExist(true);

    Runnable guardedFlushTask =
        () -> {
          try {
            flushEvents();
          } catch (Exception e) {
            addWarn("Unhandled exception in scheduled flush task", e);
          }
        };

    scheduler.scheduleAtFixedRate(
        guardedFlushTask, FLUSH_INTERVAL_SECONDS, FLUSH_INTERVAL_SECONDS, TimeUnit.SECONDS);

    super.start();
  }

  /**
   * Validates that all required configuration properties are set.
   *
   * @throws IllegalStateException if any required property is missing
   */
  private void validateConfiguration() {
    if (encoder == null || logGroupName == null || logStreamName == null)
      throw new IllegalStateException(
          "Missing required configuration: encoder, logGroupName, and logStreamName are required");
  }

  /**
   * Initializes the AWS CloudWatch Logs client. Uses either AWS credentials or Localstack
   * credentials based on configuration.
   */
  private void initializeClient() {
    var awsRegion = (region != null && !region.isEmpty()) ? Region.of(region) : Region.US_EAST_1;

    if (useLocalstack) {
      client = buildLocalstackClient(awsRegion);
      return;
    }

    client = buildAwsClient(awsRegion);
  }

  /**
   * Builds a CloudWatch Logs client configured for Localstack.
   *
   * @param awsRegion The AWS region to use
   * @return Configured CloudWatch Logs client
   */
  private CloudWatchLogsClient buildLocalstackClient(Region awsRegion) {
    return CloudWatchLogsClient.builder()
        .endpointOverride(URI.create(endpoint))
        .region(awsRegion)
        .credentialsProvider(
            StaticCredentialsProvider.create(
                AwsBasicCredentials.create(
                    accessKey != null ? accessKey : "localstack",
                    secretKey != null ? secretKey : "localstack")))
        .build();
  }

  /**
   * Builds a standard CloudWatch Logs client for AWS.
   *
   * @param awsRegion The AWS region to use
   * @return Configured CloudWatch Logs client
   */
  private CloudWatchLogsClient buildAwsClient(Region awsRegion) {
    if (useInstanceProfileProvider)
      return CloudWatchLogsClient.builder()
          .region(awsRegion)
          .credentialsProvider(InstanceProfileCredentialsProvider.create())
          .build();
    return CloudWatchLogsClient.builder()
        .region(awsRegion)
        .credentialsProvider(
            StaticCredentialsProvider.create(AwsBasicCredentials.create(accessKey, secretKey)))
        .build();
  }

  /** Creates the log group and log stream if they don't already exist. */
  private void ensureLogGroupAndStreamExist(boolean fail) {
    createLogGroupIfNotExists(fail);
    createLogStreamIfNotExists(fail);
  }

  /** Creates the CloudWatch log group if it doesn't already exist. */
  private void createLogGroupIfNotExists(boolean fail) {
    try {
      client.createLogGroup(CreateLogGroupRequest.builder().logGroupName(logGroupName).build());
      addInfo("Created log group: " + logGroupName);
    } catch (ResourceAlreadyExistsException e) {
      addInfo("Log group already exists: " + logGroupName);
    } catch (Exception e) {
      if (fail) addError("Could not create log group " + logGroupName + ": " + e.getMessage());
      else addWarn("Could not create log group " + logGroupName + ": " + e.getMessage());
    }
  }

  /** Creates the CloudWatch log stream if it doesn't already exist. */
  private void createLogStreamIfNotExists(boolean fail) {
    try {
      client.createLogStream(
          CreateLogStreamRequest.builder()
              .logGroupName(logGroupName)
              .logStreamName(logStreamName)
              .build());
      addInfo("Created log stream: " + logStreamName);
    } catch (ResourceAlreadyExistsException e) {
      addInfo("Log stream already exists: " + logStreamName);
    } catch (Exception e) {
      if (fail) addError("Could not create log stream " + logStreamName + ": " + e.getMessage());
      else addWarn("Could not create log stream " + logStreamName + ": " + e.getMessage());
    }
  }

  /**
   * Processes a logging event by encoding it and queuing it for delivery to CloudWatch. If the
   * queue reaches the batch size, triggers an immediate flush.
   *
   * @param event The logging event to be appended
   */
  protected void append(ILoggingEvent event) {
    if (!isStarted() || !enabled) return;

    try {
      queueLogEvent(event);
    } catch (Exception e) {
      addError("Failed to queue log event: " + e.getMessage());
    }
  }

  /**
   * Formats and queues a log event for sending to CloudWatch.
   *
   * @param event The logging event to queue
   * @throws InterruptedException if interrupted while waiting to put event in queue
   */
  private void queueLogEvent(ILoggingEvent event) throws InterruptedException {
    var formattedMessage = new String(encoder.encode(event));
    var logEvent =
        InputLogEvent.builder().message(formattedMessage).timestamp(event.getTimeStamp()).build();
    if (eventQueue.size() >= batchSize) scheduler.execute(this::flushEvents);

    eventQueue.put(logEvent);

    if (eventQueue.size() >= batchSize) scheduler.execute(this::flushEvents);
  }

  /**
   * Flushes queued log events to CloudWatch. Handles retries for throttling and service errors.
   * This method is synchronized to prevent concurrent flushes.
   */
  private synchronized void flushEvents() {
    if (eventQueue.isEmpty() || !isStarted()) return;

    var batch = new ArrayList<InputLogEvent>();
    eventQueue.drainTo(batch, batchSize);

    if (batch.isEmpty()) return;

    batch.sort(Comparator.comparingLong(InputLogEvent::timestamp));
    for (var attempt = 0; attempt < MAX_RETRY_ATTEMPTS; attempt++) {
      try {
        sendBatchToCloudWatch(batch);
        addInfo("Sent " + batch.size() + " log events to CloudWatch");
        return;
      } catch (ResourceNotFoundException e) {
        handleResourceNotFoundException();
      } catch (ThrottlingException e) {
        handleThrottlingException(attempt);
      } catch (Exception e) {
        handleGeneralException(e, attempt);
      }
    }

    addError("Failed to send logs to CloudWatch after " + MAX_RETRY_ATTEMPTS + " attempts");
  }

  /**
   * Sends a batch of log events to CloudWatch.
   *
   * @param batch The batch of log events to send
   */
  private void sendBatchToCloudWatch(ArrayList<InputLogEvent> batch) {
    var request =
        PutLogEventsRequest.builder()
            .logGroupName(logGroupName)
            .logStreamName(logStreamName)
            .logEvents(batch)
            .build();

    client.putLogEvents(request);
  }

  /** Handles the case where the log group or stream is not found. */
  private void handleResourceNotFoundException() {
    addInfo("Log group or stream not found, recreating");
    ensureLogGroupAndStreamExist(false);
  }

  /**
   * Handles throttling exceptions from CloudWatch.
   *
   * @param attempt The current retry attempt number
   */
  private void handleThrottlingException(int attempt) {
    addWarn("Throttled by CloudWatch, waiting before retry");
    sleep(calculateBackoffTime(attempt));
  }

  /**
   * Handles general exceptions when sending logs to CloudWatch.
   *
   * @param e The exception that occurred
   * @param attempt The current retry attempt number
   */
  private void handleGeneralException(Exception e, int attempt) {
    addWarn("Error sending logs to CloudWatch (attempt " + (attempt + 1) + "): " + e.getMessage());
    sleep(calculateBackoffTime(attempt));
  }

  /**
   * Calculates the backoff time for retries using exponential backoff.
   *
   * @param attempt The current retry attempt number
   * @return Time to wait in milliseconds
   */
  private long calculateBackoffTime(int attempt) {
    return Math.min(1000 * (attempt + 1), 5000);
  }

  /**
   * Sleeps for the specified duration, handling interruptions.
   *
   * @param millis Time to sleep in milliseconds
   */
  private void sleep(long millis) {
    try {
      Thread.sleep(millis);
    } catch (InterruptedException ie) {
      Thread.currentThread().interrupt();
    }
  }

  /**
   * Stops the appender, flushes any remaining events, shuts down the scheduler, and closes the
   * CloudWatch client.
   */
  public void stop() {
    if (!isStarted()) return;

    shutdownScheduler();
    flushEvents();
    closeClient();

    super.stop();
    addInfo("Stopped CloudWatchAppender");
  }

  /** Shuts down the scheduler that periodically flushes logs. */
  /**
   * Shuts down the scheduler that periodically flushes logs. Uses a two-phase shutdown approach
   * with graceful shutdown first, followed by forced shutdown if necessary.
   */
  private void shutdownScheduler() {
    scheduler.shutdown();
    try {
      if (!scheduler.awaitTermination(5, TimeUnit.SECONDS)) {
        addWarn("Scheduler did not terminate in time, forcing shutdown");
        var droppedTasks = scheduler.shutdownNow();
        if (!droppedTasks.isEmpty())
          addWarn("Dropped " + droppedTasks.size() + " pending tasks during shutdown");

        if (!scheduler.awaitTermination(2, TimeUnit.SECONDS))
          addWarn("Scheduler did not terminate after forced shutdown");
      }
    } catch (InterruptedException e) {
      addWarn("Interrupted during scheduler shutdown");
      scheduler.shutdownNow();
      Thread.currentThread().interrupt();
    }
  }

  /** Closes the CloudWatch client if it exists. */
  private void closeClient() {
    if (client != null) {
      client.close();
      client = null;
    }
  }
}
