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
package com.asc.transfer.job;

import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.transfer.configuration.BatchProcessingConfiguration;
import com.asc.transfer.entity.ClientDynamoEntity;
import com.asc.transfer.entity.ClientEntity;
import com.asc.transfer.entity.ScopeEntity;
import com.asc.transfer.reader.EnrichedClientItemReader;
import java.util.ArrayList;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import org.springframework.batch.core.Job;
import org.springframework.batch.core.Step;
import org.springframework.batch.core.configuration.annotation.EnableBatchProcessing;
import org.springframework.batch.core.job.builder.JobBuilder;
import org.springframework.batch.core.repository.JobRepository;
import org.springframework.batch.core.step.builder.StepBuilder;
import org.springframework.batch.item.ItemProcessor;
import org.springframework.batch.item.ItemWriter;
import org.springframework.batch.item.database.JdbcPagingItemReader;
import org.springframework.batch.item.support.CompositeItemProcessor;
import org.springframework.batch.repeat.policy.SimpleCompletionPolicy;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.transaction.PlatformTransactionManager;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbEnhancedClient;
import software.amazon.awssdk.enhanced.dynamodb.TableSchema;

/**
 * Spring Batch job configuration for transferring client data from a relational database to
 * DynamoDB.
 *
 * <p>This configuration defines a single-step job that performs the following actions:
 *
 * <ol>
 *   <li><strong>Data Conversion</strong> – Reads {@link ClientEntity} objects via an enriched
 *       reader and converts them into {@link ClientDynamoEntity} objects suitable for DynamoDB
 *       storage.
 *   <li><strong>Client Secret Re-encryption</strong> – Re-encrypts the client secret of the
 *       converted entities by decrypting with the current encryption key and encrypting with the
 *       new encryption key.
 *   <li><strong>Data Persistence</strong> – Writes the fully processed {@link ClientDynamoEntity}
 *       objects directly to DynamoDB.
 * </ol>
 *
 * <p>When configured with a batch size and page size of 1, the job processes each record completely
 * (read, convert, re-encrypt, and write) before fetching the next record.
 */
@Configuration
@RequiredArgsConstructor
@EnableBatchProcessing(dataSourceRef = "batchDataSource")
public class ClientTransferJob {

  private final EncryptionService fromEncryptionService;
  private final EncryptionService toEncryptionService;
  private final BatchProcessingConfiguration batchProcessingConfiguration;
  private final PlatformTransactionManager transactionManager;

  /**
   * Creates a composite item processor that sequentially applies two processors:
   *
   * <ol>
   *   <li>The first processor converts a {@link ClientEntity} into a {@link ClientDynamoEntity}.
   *   <li>The second processor re-encrypts the client secret of the converted entity.
   * </ol>
   *
   * @return a composite item processor that transforms and re-encrypts client data
   * @throws Exception if the processor is not properly configured
   */
  @Bean
  public CompositeItemProcessor<ClientEntity, ClientDynamoEntity> compositeProcessor()
      throws Exception {
    var compositeProcessor = new CompositeItemProcessor<ClientEntity, ClientDynamoEntity>();
    var delegates = new ArrayList<ItemProcessor<?, ?>>();
    delegates.add(toDynamoEntityProcessor());
    delegates.add(reEncryptionProcessor());
    compositeProcessor.setDelegates(delegates);
    compositeProcessor.afterPropertiesSet();
    return compositeProcessor;
  }

  /**
   * Converts a {@link ClientEntity} to a {@link ClientDynamoEntity} for persistence in DynamoDB.
   *
   * <p>This processor maps fields from the relational entity to the DynamoDB entity, including
   * conversion of authentication methods and scopes when available.
   *
   * @return an item processor that transforms a {@link ClientEntity} into a {@link
   *     ClientDynamoEntity}
   */
  @Bean
  public ItemProcessor<ClientEntity, ClientDynamoEntity> toDynamoEntityProcessor() {
    return clientEntity -> {
      var dynamoEntity = new ClientDynamoEntity();
      dynamoEntity.setClientId(clientEntity.getClientId());
      dynamoEntity.setTenantId(clientEntity.getTenantId());
      dynamoEntity.setClientSecret(clientEntity.getClientSecret());
      dynamoEntity.setName(clientEntity.getName());
      dynamoEntity.setDescription(clientEntity.getDescription());
      dynamoEntity.setLogo(clientEntity.getLogo());
      if (clientEntity.getAuthenticationMethods() != null) {
        dynamoEntity.setAuthenticationMethods(
            clientEntity.getAuthenticationMethods().stream()
                .map(Enum::name)
                .collect(Collectors.toSet()));
      }
      dynamoEntity.setRedirectUris(clientEntity.getRedirectUris());
      dynamoEntity.setAllowedOrigins(clientEntity.getAllowedOrigins());
      dynamoEntity.setWebsiteUrl(clientEntity.getWebsiteUrl());
      dynamoEntity.setTermsUrl(clientEntity.getTermsUrl());
      dynamoEntity.setPolicyUrl(clientEntity.getPolicyUrl());
      dynamoEntity.setLogoutRedirectUri(clientEntity.getLogoutRedirectUri());
      dynamoEntity.setAccessible(clientEntity.isAccessible());
      dynamoEntity.setEnabled(clientEntity.isEnabled());
      if (clientEntity.getScopes() != null) {
        dynamoEntity.setScopes(
            clientEntity.getScopes().stream()
                .map(ScopeEntity::getName)
                .collect(Collectors.toSet()));
      }
      dynamoEntity.setCreatedOn(
          clientEntity.getCreatedOn() != null ? clientEntity.getCreatedOn().toString() : null);
      dynamoEntity.setCreatedBy(clientEntity.getCreatedBy());
      dynamoEntity.setModifiedOn(
          clientEntity.getModifiedOn() != null ? clientEntity.getModifiedOn().toString() : null);
      dynamoEntity.setModifiedBy(clientEntity.getModifiedBy());
      return dynamoEntity;
    };
  }

  /**
   * Re-encrypts the client secret of a {@link ClientDynamoEntity}.
   *
   * <p>This processor decrypts the client secret using the current encryption key and then encrypts
   * it with the new encryption key.
   *
   * @return an item processor that updates the client secret of a {@link ClientDynamoEntity}
   */
  @Bean
  public ItemProcessor<ClientDynamoEntity, ClientDynamoEntity> reEncryptionProcessor() {
    return dynamoEntity -> {
      var clientSecret = fromEncryptionService.decrypt(dynamoEntity.getClientSecret());
      dynamoEntity.setClientSecret(toEncryptionService.encrypt(clientSecret));
      return dynamoEntity;
    };
  }

  /**
   * Writes {@link ClientDynamoEntity} objects to a specified DynamoDB table.
   *
   * @param tableName the name of the DynamoDB table where data will be stored
   * @param enhancedClient the DynamoDB enhanced client used for performing table operations
   * @return an item writer that persists {@link ClientDynamoEntity} objects into DynamoDB
   */
  @Bean
  public ItemWriter<ClientDynamoEntity> dynamoClientWriter(
      @Value("${spring.cloud.aws.dynamodb.tables.registeredClient}") String tableName,
      DynamoDbEnhancedClient enhancedClient) {
    return items -> {
      var table = enhancedClient.table(tableName, TableSchema.fromBean(ClientDynamoEntity.class));
      for (var item : items) {
        table.putItem(item);
      }
    };
  }

  /**
   * Creates an enriched reader that wraps a delegate {@link JdbcPagingItemReader} to read {@link
   * ClientEntity} objects and enrich them with additional related data.
   *
   * <p>The enriched reader fetches raw client data and supplements it by retrieving associated
   * authentication methods, redirect URIs, allowed origins, and scopes.
   *
   * @param delegateClientReader the delegate reader for {@link ClientEntity} objects
   * @param namedParameterJdbcTemplate the JDBC template for executing additional queries
   * @return an enriched reader for client data
   */
  @Bean
  public EnrichedClientItemReader enrichedClientReader(
      JdbcPagingItemReader<ClientEntity> delegateClientReader,
      NamedParameterJdbcTemplate namedParameterJdbcTemplate) {
    return new EnrichedClientItemReader(
        delegateClientReader,
        namedParameterJdbcTemplate,
        batchProcessingConfiguration.getBatchSize());
  }

  /**
   * Defines a single step that sequentially reads, processes, and writes client data.
   *
   * <p>This step uses a chunk-oriented approach where the chunk size is defined by the batch
   * processing configuration. For each chunk, the reader retrieves client data, the composite
   * processor applies data conversion and re-encryption, and the writer persists the processed data
   * to DynamoDB.
   *
   * @param jobRepository the job repository for managing step execution metadata
   * @param enrichedClientReader the enriched reader for retrieving {@link ClientEntity} objects
   * @param enhancedClient the DynamoDB enhanced client for table operations
   * @param tableName the name of the DynamoDB table where data will be stored
   * @return a configured step for processing client data
   * @throws Exception if an error occurs during step configuration
   */
  @Bean
  public Step clientDataTransferStep(
      JobRepository jobRepository,
      EnrichedClientItemReader enrichedClientReader,
      DynamoDbEnhancedClient enhancedClient,
      @Value("${spring.cloud.aws.dynamodb.tables.registeredClient}") String tableName)
      throws Exception {
    return new StepBuilder("clientDataTransferStep", jobRepository)
        .<ClientEntity, ClientDynamoEntity>chunk(
            new SimpleCompletionPolicy(batchProcessingConfiguration.getBatchSize()),
            transactionManager)
        .reader(enrichedClientReader)
        .processor(compositeProcessor())
        .writer(dynamoClientWriter(tableName, enhancedClient))
        .build();
  }

  /**
   * Configures the Spring Batch job for transferring client data to DynamoDB.
   *
   * <p>The job consists of a single step that reads, processes, and writes client data
   * sequentially. This ensures that each record is fully processed (converted and re-encrypted)
   * before the next record is read.
   *
   * @param jobRepository the job repository for managing job execution metadata
   * @param clientTransferStep the step that processes client data
   * @return a configured job for transferring client data
   * @throws Exception if an error occurs during job configuration
   */
  @Bean
  public Job clientDataTransferJob(JobRepository jobRepository, Step clientTransferStep)
      throws Exception {
    return new JobBuilder("clientDataTransferJob", jobRepository).start(clientTransferStep).build();
  }
}
