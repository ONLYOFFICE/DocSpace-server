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

package com.asc.common.data.message.repository;

import com.asc.common.service.ports.output.repository.PendingMessageRepository;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;

/**
 * Keeps messages the broker could not take in {@code identity_pending_messages} until the relay
 * sends them again.
 */
@Repository
@RequiredArgsConstructor
public class JdbcPendingMessageRepository implements PendingMessageRepository {
  private final JdbcTemplate jdbcTemplate;

  @Override
  public void save(String exchange, String routingKey, String headers, byte[] body) {
    jdbcTemplate.update(
        """
        INSERT INTO identity_pending_messages
            (exchange_name, routing_key, headers, body, attempts, deliver_at, created_at)
        VALUES (?, ?, ?, ?, 0, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))
        """,
        exchange,
        routingKey,
        headers,
        body);
  }

  @Override
  public List<PendingMessage> findDue(int limit) {
    return jdbcTemplate.query(
        """
        SELECT id, exchange_name, routing_key, headers, body, attempts,
               TIMESTAMPDIFF(SECOND, created_at, UTC_TIMESTAMP(6)) AS age_seconds
        FROM identity_pending_messages
        WHERE deliver_at <= UTC_TIMESTAMP(6)
        ORDER BY id
        LIMIT ?
        """,
        (rs, row) ->
            new PendingMessage(
                rs.getLong("id"),
                rs.getString("exchange_name"),
                rs.getString("routing_key"),
                rs.getString("headers"),
                rs.getBytes("body"),
                rs.getInt("attempts"),
                rs.getLong("age_seconds")),
        limit);
  }

  @Override
  public void delete(long id) {
    jdbcTemplate.update("DELETE FROM identity_pending_messages WHERE id = ?", id);
  }

  @Override
  public void postpone(long id, long delaySeconds) {
    jdbcTemplate.update(
        """
        UPDATE identity_pending_messages
        SET attempts = attempts + 1, deliver_at = UTC_TIMESTAMP(6) + INTERVAL ? SECOND
        WHERE id = ?
        """,
        delaySeconds,
        id);
  }

  @Override
  public long count() {
    var count =
        jdbcTemplate.queryForObject("SELECT COUNT(*) FROM identity_pending_messages", Long.class);
    return count == null ? 0 : count;
  }
}
