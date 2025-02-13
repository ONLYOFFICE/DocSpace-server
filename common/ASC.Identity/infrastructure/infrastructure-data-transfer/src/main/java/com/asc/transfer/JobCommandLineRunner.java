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
package com.asc.transfer;

import lombok.RequiredArgsConstructor;
import org.springframework.batch.core.Job;
import org.springframework.batch.core.JobParametersBuilder;
import org.springframework.batch.core.launch.JobLauncher;
import org.springframework.boot.CommandLineRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration class that defines a {@link CommandLineRunner} bean to launch the client data
 * transfer job.
 *
 * <p>This class uses Spring Boot's {@code CommandLineRunner} interface to trigger the execution of
 * the Spring Batch job upon application startup. It leverages the {@link JobLauncher} to run the
 * {@code clientDataTransferJob} with a unique set of job parameters (including the current
 * timestamp) to ensure a distinct job instance for each execution.
 *
 * <p>The class is annotated with {@code @Configuration} to indicate that it provides bean
 * definitions, and {@code @RequiredArgsConstructor} to automatically generate a constructor with
 * the required fields.
 */
@Configuration
@RequiredArgsConstructor
public class JobCommandLineRunner {

  /** The {@link JobLauncher} used to execute the Spring Batch job. */
  private final JobLauncher jobLauncher;

  /** The Spring Batch job that transfers client data from the relational database to DynamoDB. */
  private final Job clientDataTransferJob;

  /**
   * Creates a {@link CommandLineRunner} bean that launches the client data transfer job at
   * application startup.
   *
   * <p>The {@code run} method of the returned {@link CommandLineRunner} triggers the job execution
   * by calling {@link JobLauncher#run(Job, org.springframework.batch.core.JobParameters)} with a
   * set of job parameters. A unique timestamp is added as a parameter to ensure that each job
   * instance is distinct.
   *
   * @return a {@link CommandLineRunner} that starts the client data transfer job when the
   *     application runs
   */
  @Bean
  public CommandLineRunner commandLineRunner() {
    return args ->
        jobLauncher.run(
            clientDataTransferJob,
            new JobParametersBuilder()
                .addLong("time", System.currentTimeMillis())
                .toJobParameters());
  }
}
