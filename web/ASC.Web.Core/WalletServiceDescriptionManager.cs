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

namespace ASC.Web.Core;

public class WalletServiceDescriptionManager
{
    private static readonly Dictionary<string, string> _mapping = new()
    {
        { "chat", "total_tokens" },
        { "embedding", "prompt_tokens" },
        { "search", "num_results" }
    };

    public static (string, string, int) GetServiceDescriptionAndUom(Operation operation, string filterServiceName, Dictionary<string, string> metadata, string logoText)
    {
        if (operation == null)
        {
            return (string.Empty, string.Empty, 0);
        }

        var serviceName = operation.Service;
        var quantity = operation.Quantity;

        // for testing purposes
        if (serviceName != null && serviceName.StartsWith("disk-storage"))
        {
            serviceName = "disk-storage";
        }

        if (operation.Type == OperationType.AiServicePayment)
        {
            if (string.IsNullOrEmpty(serviceName) && !string.IsNullOrEmpty(filterServiceName))
            {
                serviceName = filterServiceName;
            }

            if (metadata != null && metadata.TryGetValue(BillingClient.MetadataType, out var type))
            {
                serviceName = type;

                if (_mapping.TryGetValue(type, out var quantityField) &&
                    metadata.TryGetValue(quantityField, out var quantityStr) &&
                    int.TryParse(quantityStr, out var quantityValue))
                {
                    quantity = quantityValue;
                }
            }
        }

        if (string.IsNullOrEmpty(serviceName))
        {
            switch (operation.Type)
            {
                case OperationType.Deposit:
                    serviceName = "top-up";
                    quantity = 0;
                    break;
                case OperationType.AiCredit:
                    serviceName = "ai-tools";
                    quantity = (int)operation.Debit; // truncate
                    break;
            }
        }

        var description = (Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceDesc_{serviceName}") ?? "").Replace("{LogoText}", logoText);
        var uom = Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceUOM_{serviceName}");

        return (description, uom, quantity);
    }

    public static string GetServiceDetails(Dictionary<string, string> metadata)
    {
        if (metadata == null)
        {
            return string.Empty;
        }

        if (metadata.TryGetValue(BillingClient.MetadataDetails, out var details))
        {
            return details;
        }

        return metadata.TryGetValue(BillingClient.MetadataModel, out var model) ? model : string.Empty;
    }

    public static (string, string) GetAgentInfo(Dictionary<string, string> metadata)
    {
        if (metadata == null)
        {
            return (null, null);
        }

        metadata.TryGetValue(BillingClient.MetadataAgentId, out var agentId);
        metadata.TryGetValue(BillingClient.MetadataAgentTitle, out var agentTitle);

        return (agentId, agentTitle);
    }
}
