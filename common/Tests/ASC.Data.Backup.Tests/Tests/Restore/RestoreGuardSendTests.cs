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


namespace ASC.Data.Backup.Tests.Tests.Restore;

/// <summary>
/// A typed DTO is checked as the SDK serializes it, and a refused body never reaches the send delegate -
/// the one that stands for the SDK call - while an allowed one reaches it once. It needs no portal and
/// sends nothing.
/// </summary>
public class RestoreGuardSendTests
{
    public static TheoryData<string> RefusedTypedCases => [.. RestoreGuardData.RefusedTyped.Keys];

    public static TheoryData<string> AllowedTypedCases => [.. RestoreGuardData.AllowedTyped.Keys];

    [Theory]
    [MemberData(nameof(RefusedTypedCases))]
    public void AssertCannotQueue_TypedDtoThatCouldQueue_Throws(string name)
    {
        // Arrange
        var dto = RestoreGuardData.RefusedTyped[name]();

        // Act
        var act = () => RestoreGuard.AssertCannotQueue(dto);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Refusing to send a restore request*");
    }

    [Theory]
    [MemberData(nameof(AllowedTypedCases))]
    public void AssertCannotQueue_TypedDtoThatCannotQueue_DoesNotThrow(string name)
    {
        // Arrange
        var dto = RestoreGuardData.AllowedTyped[name]();

        // Act
        var act = () => RestoreGuard.AssertCannotQueue(dto);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SendAsync_RefusedBodies_NeverReachTheSendDelegate()
    {
        // Arrange
        var sent = 0;
        Func<Task<int>> send = () =>
        {
            sent++;

            return Task.FromResult(sent);
        };

        // Act
        foreach (var json in RestoreGuardData.AllRefused)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => RestoreGuard.SendAsync(json, send));
        }

        foreach (var createDto in RestoreGuardData.RefusedTyped.Values)
        {
            var dto = createDto();

            await Assert.ThrowsAsync<InvalidOperationException>(() => RestoreGuard.SendAsync(dto, send));
        }

        // Assert
        sent.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_AllowedBodies_ReachTheSendDelegateOnceEach()
    {
        // Arrange
        var sent = 0;
        Func<Task<int>> send = () =>
        {
            sent++;

            return Task.FromResult(sent);
        };

        // Act
        await RestoreGuard.SendAsync("""{"backupId":"not-a-guid"}""", send);

        // Assert
        sent.Should().Be(1);

        // Act
        await RestoreGuard.SendAsync(new StartBackupRestoreRequestDto(backupId: "not-a-guid"), send);

        // Assert
        sent.Should().Be(2);
    }
}
