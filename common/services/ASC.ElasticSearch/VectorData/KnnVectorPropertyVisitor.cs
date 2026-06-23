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

namespace ASC.ElasticSearch.VectorData;

public class KnnVectorPropertyVisitor : IPropertyVisitor
{
    public void Visit(ITextProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IKeywordProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(INumberProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IBooleanProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IDateProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IDateNanosProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IBinaryProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(INestedProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IObjectProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IGeoPointProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IGeoShapeProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(ICompletionProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IIpProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }
    
    public void Visit(IMurmur3HashProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(ITokenCountProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IPercolatorProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IIntegerRangeProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IFloatRangeProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(ILongRangeProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IDoubleRangeProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IDateRangeProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IIpRangeProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IJoinProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IRankFeatureProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IRankFeaturesProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(ISearchAsYouTypeProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IFieldAliasProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public void Visit(IKnnVectorProperty type, PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute) { }

    public IProperty Visit(PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute)
    {
        return null;
    }

    public bool SkipProperty(PropertyInfo propertyInfo, OpenSearchPropertyAttributeBase attribute)
    {
        return attribute is KnnVector;
    }
}