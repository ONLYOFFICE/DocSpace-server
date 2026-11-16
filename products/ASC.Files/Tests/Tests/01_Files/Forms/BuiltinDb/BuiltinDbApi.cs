// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
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

namespace ASC.Files.Tests.Tests._01_Files.Forms.BuiltinDb;

/// <summary>
/// The built-in forms database endpoints over raw HTTP: the DocSpace.API.SDK 4.1.0 does not carry them yet. Switch these
/// calls to the SDK once a release has them.
/// </summary>
internal static class BuiltinDbApi
{
    public const string ConnectionPath = "api/2.0/files/builtinDb/connectionString";
    public const string PasswordPath = "api/2.0/files/builtinDb/password";

    /// <summary>The database the forms server creates from POSTGRES_DB, shared by the portals not moved yet.</summary>
    public const string SharedDatabase = "forms";

    /// <summary>The owner of the forms database, as DocSpace signs in to it: not a superuser.</summary>
    public const string AdminRole = "forms_admin";

    /// <summary>One <c>GET</c> of the connection details, whatever it answers.</summary>
    public static Task<HttpResponseMessage> GetConnectionAsync(HttpClient client) =>
        new RawApiClient(client).GetAsync(ConnectionPath, TestContext.Current.CancellationToken);

    /// <summary>One <c>PUT</c> of a new password, whatever it answers.</summary>
    public static Task<HttpResponseMessage> ResetPasswordAsync(HttpClient client) =>
        new RawApiClient(client).PutAsync(PasswordPath, null, TestContext.Current.CancellationToken);

    /// <summary>The body of an answer with the connection details.</summary>
    public static Task<BuiltinDbConnection> ReadAsync(HttpClient client, HttpResponseMessage response) =>
        new RawApiClient(client).ReadAsync<BuiltinDbConnection>(response, TestContext.Current.CancellationToken);

    /// <summary>
    /// The connection details once the portal's own database is ready: the first calls answer 202 while it is prepared.
    /// With <paramref name="resetPassword"/> the password is reset by the call that answers 200.
    /// </summary>
    public static async Task<BuiltinDbConnection> GetReadyAsync(HttpClient client, bool resetPassword = false)
    {
        var api = new RawApiClient(client);
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (true)
        {
            using var response = resetPassword
                ? await api.PutAsync(PasswordPath, null, TestContext.Current.CancellationToken)
                : await api.GetAsync(ConnectionPath, TestContext.Current.CancellationToken);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
                return await api.ReadAsync<BuiltinDbConnection>(response, TestContext.Current.CancellationToken);
            }

            DateTime.UtcNow.Should().BeBefore(deadline, "the portal's own database must be ready within a minute");
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    public static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        // No client timeout: a test may wait for the server to end a query (pg_cron) or a session.
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 0 };
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public static async Task<string> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);

        return (string)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }
}

internal sealed record BuiltinDbConnection(string Host, int Port, string Database, string UserName, string Password, string ConnectionString);
