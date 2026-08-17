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

using System;
using System.Collections.Generic;
using System.Linq;
using ASC.Web.Core.ModuleManagement.Common;
using ASC.Web.Core.Utility.Skins;

namespace ASC.Web.Core.Utility
{
    public static class SearchHandlerManager
    {
        private static readonly List<ISearchHandlerEx> handlers = new List<ISearchHandlerEx>();

        public static void Registry(ISearchHandlerEx handler)
        {
            lock (handlers)
            {
                if (handler != null && handlers.All(h => h.GetType() != handler.GetType()))
                {
                    handlers.Add(handler);
                }
            }
        }

        public static void UnRegistry(ISearchHandlerEx handler)
        {
            lock (handlers)
            {
                if (handler != null)
                {
                    handlers.RemoveAll(h => h.GetType() == handler.GetType());
                }
            }
        }

        public static List<ISearchHandlerEx> GetAllHandlersEx()
        {
            lock (handlers)
            {
                return handlers.ToList();
            }
        }

        public static List<ISearchHandlerEx> GetHandlersExForProduct(Guid productID)
        {
            lock (handlers)
            {
                return handlers
                    .Where(h => h.ProductID.Equals(productID) || h.ProductID.Equals(Guid.Empty))
                    .ToList();
            }
        }

        public static List<ISearchHandlerEx> GetHandlersExForProducts(Guid[] productIDs)
        {
            lock (handlers)
            {
                return handlers
                    .Where(h => productIDs.Contains(h.ProductID) || h.ProductID.Equals(Guid.Empty))
                    .ToList();
            }
        }

        public static List<ISearchHandlerEx> GetHandlersExForProductModule(Guid productID, Guid moduleID)
        {
            var searchHandlers = productID.Equals(Guid.Empty)
                                     ? GetAllHandlersEx()
                                     : GetHandlersExForProduct(productID);

            lock (searchHandlers)
            {
                return moduleID.Equals(Guid.Empty)
                           ? searchHandlers.ToList()
                           : searchHandlers.FindAll(x => x.ModuleID == moduleID).ToList();
            }
        }

        public static List<ISearchHandlerEx> GetHandlersExForProductModule(Guid[] productIDs)
        {
            return GetHandlersExForProducts(productIDs);
        }
    }

    public abstract class BaseSearchHandlerEx : ISearchHandlerEx
    {
        public abstract ImageOptions Logo { get; }

        public abstract string SearchName { get; }

        public virtual IItemControl Control
        {
            get { return null; }
        }

        public virtual Guid ProductID
        {
            get { return Guid.Empty; }
        }

        public virtual Guid ModuleID
        {
            get { return Guid.Empty; }
        }

        public abstract SearchResultItem[] Search(string text);
    }
}