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


namespace ASC.Web.Api.Billing.Tests.Tests.ServiceLimits;

/// <summary>
/// The spending limits accounting refuses, and how the stack passes the refusal on: a second limit of the same kind is a
/// conflict the caller is told about, and a limit somebody else deleted between the stack's own check and the change
/// is answered as missing, the same as one that was never there.
/// </summary>
public class ServiceLimitConflictTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CreateServiceLimit_SecondOfTheSameScope_ThrowsConflict()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        await CreateAsync(new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateAsync(new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 80, period: ServiceLimitPeriod.Week)));

        // Assert
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent!.ToString().Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateServiceLimit_SecondForTheSameUser_ThrowsConflict()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var user = await InviteMember(EmployeeType.User);
        await CreateAsync(new CreateServiceLimitRequestDto(ServiceLimitScope.Custom, userId: user.Id, amountValue: 5, period: ServiceLimitPeriod.Day));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateAsync(new CreateServiceLimitRequestDto(ServiceLimitScope.Custom, userId: user.Id, amountValue: 7, period: ServiceLimitPeriod.Day)));

        // Assert
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent!.ToString().Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateServiceLimit_ForAnotherUser_IsNoConflict()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var first = await InviteMember(EmployeeType.User);
        var second = await InviteMember(EmployeeType.User);
        await CreateAsync(new CreateServiceLimitRequestDto(ServiceLimitScope.Custom, userId: first.Id, amountValue: 5, period: ServiceLimitPeriod.Day));

        // Act
        var created = await CreateAsync(new CreateServiceLimitRequestDto(ServiceLimitScope.Custom, userId: second.Id, amountValue: 5, period: ServiceLimitPeriod.Day));

        // Assert
        created.UserId.Should().Be(second.Id);
    }

    [Fact]
    public async Task UpdateServiceLimit_DeletedBeforeTheUpdateReachedAccounting_ThrowsNotFound()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var created = await CreateAsync(new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day));
        PaymentServices.ServiceLimitGoneBeforeNextChange(created.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.UpdateServiceLimitAsync(new UpdateServiceLimitRequestDto(created.Id, amountValue: 75),
                TestContext.Current.CancellationToken));

        // Assert: accounting answers a change of a missing limit with 400, which is no threshold of the caller's.
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Service limit could not be found");
    }

    [Fact]
    public async Task DeleteServiceLimit_DeletedBeforeTheDeletionReachedAccounting_ThrowsNotFound()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var created = await CreateAsync(new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day));
        PaymentServices.ServiceLimitGoneBeforeNextChange(created.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.DeleteServiceLimitAsync(created.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Service limit could not be found");
    }

    private async Task<ServiceLimitDto> CreateAsync(CreateServiceLimitRequestDto request)
    {
        return (await _paymentApi.CreateServiceLimitAsync(request, TestContext.Current.CancellationToken)).Response;
    }
}
