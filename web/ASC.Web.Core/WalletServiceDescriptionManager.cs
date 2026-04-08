// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Web.Core;

public class WalletServiceDescriptionManager()
{
    private static readonly Dictionary<string, string> _mapping = new()
    {
        { "chat", "total_tokens" },
        { "embedding", "prompt_tokens" },
        { "search", "num_results" }
    };

    public static (string, string, int) GetServiceDescriptionAndUom(Operation operation, string filterServiceName, Dictionary<string, string> metadata)
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

        if (string.IsNullOrEmpty(serviceName))
        {
            if (!string.IsNullOrEmpty(filterServiceName))
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
            serviceName = "top-up";
            quantity = 0;
        }

        return (Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceDesc_{serviceName}"),
            Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceUOM_{serviceName}"),
            quantity);
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
        metadata.TryGetValue(BillingClient.MetadataAgentId, out var agentTitle);

        return (agentId, agentTitle);
    }
}
