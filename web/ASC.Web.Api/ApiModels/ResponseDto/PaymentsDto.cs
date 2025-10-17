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
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// Represents a report containing a collection of operations.
/// </summary>
public class ReportDto
{
    /// <summary>
    /// A collection of operations.
    /// </summary>
    public List<OperationDto> Collection { get; set; }
    /// <summary>
    /// The report data offset.
    /// </summary>
    public int Offset { get; set; }
    /// <summary>
    /// The report data limit.
    /// </summary>
    public int Limit { get; set; }
    /// <summary>
    /// The total quantity of operations in the report.
    /// </summary>
    public int TotalQuantity { get; set; }
    /// <summary>
    /// The total number of pages in the report.
    /// </summary>
    public int TotalPage { get; set; }
    /// <summary>
    /// The current page number of the report.
    /// </summary>
    public int CurrentPage { get; set; }

    public ReportDto(Report report, ApiDateTimeHelper apiDateTimeHelper, Dictionary<string, string> participantDisplayNames)
    {
        Offset = report.Offset;
        Limit = report.Limit;
        TotalQuantity = report.TotalQuantity;
        TotalPage = report.TotalPage;
        CurrentPage = report.CurrentPage;

        Collection = [];

        if (report.Collection != null)
        {
            foreach (var operation in report.Collection)
            {
                Collection.Add(new OperationDto(operation, apiDateTimeHelper, participantDisplayNames));
            }
        }
    }
}

/// <summary>
/// Represents an operation.
/// </summary>
public class OperationDto
{
    /// <summary>
    /// The date when the operation took place.
    /// </summary>
    public ApiDateTime Date { get; set; }
    /// <summary>
    /// The service related to the operation.
    /// </summary>
    public string Service { get; set; }
    /// <summary>
    /// The brief operation description.
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// The detailed information about the operation.
    /// </summary>
    public string Details { get; set; }
    /// <summary>
    /// The service unit.
    /// </summary>
    public string ServiceUnit { get; set; }
    /// <summary>
    /// The quantity of the service used.
    /// </summary>
    public int Quantity { get; set; }
    /// <summary>
    /// The three-character ISO 4217 currency symbol of the operation.
    /// </summary>
    public string Currency { get; set; }
    /// <summary>
    /// The credit amount of the operation.
    /// </summary>
    public decimal Credit { get; set; }
    /// <summary>
    /// The debit amount of the operation.
    /// </summary>
    public decimal Debit { get; set; }
    /// <summary>
    /// The participant original name.
    /// </summary>
    public string ParticipantName { get; set; }
    /// <summary>
    /// The participant display name.
    /// </summary>
    public string ParticipantDisplayName { get; set; }

    public OperationDto(Operation operation, ApiDateTimeHelper apiDateTimeHelper, Dictionary<string, string> participantDisplayNames)
    {
        var (description, unitOfMeasurement) = GetServiceDescAndUOM(operation.Service);

        Date = apiDateTimeHelper.Get(operation.Date);
        Service = operation.Service;
        Description = description;
        Details = operation.Metadata != null && operation.Metadata.TryGetValue(BillingClient.MetadataDetails, out var details) ? details : string.Empty;
        ServiceUnit = unitOfMeasurement;
        Quantity = operation.Quantity;
        Currency = operation.Currency;
        Credit = operation.Credit;
        Debit = operation.Debit;
        ParticipantName = operation.ParticipantName;
        ParticipantDisplayName = operation.ParticipantName != null && participantDisplayNames.TryGetValue(operation.ParticipantName, out var value)
            ? value
            : operation.ParticipantName;
    }

    private static (string, string) GetServiceDescAndUOM(string serviceName)
    {
        // for testing purposes
        if (serviceName != null && serviceName.StartsWith("disk-storage"))
        {
            serviceName = "disk-storage";
        }

        if (string.IsNullOrEmpty(serviceName))
        {
            serviceName = "top-up";
        }

        return (Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceDesc_{serviceName}"),
            Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceUOM_{serviceName}"));
    }
}

/// <summary>
/// The customer information.
/// </summary>
public class CustomerInfoDto(CustomerInfo customerInfo, EmployeeDto employeeDto)
{
    /// <summary>
    /// The portal ID.
    /// </summary>
    public string PortalId { get; private set; } = customerInfo.PortalId;

    /// <summary>
    /// The customer's payment method.
    /// </summary>
    public PaymentMethodStatus PaymentMethodStatus { get; private set; } = customerInfo.PaymentMethodStatus;

    /// <summary>
    /// The customer email address.
    /// </summary>
    public string Email { get; private set; } = customerInfo.Email;

    /// <summary>
    /// The paying user.
    /// </summary>
    public EmployeeDto Payer { get; private set; } = employeeDto;
}