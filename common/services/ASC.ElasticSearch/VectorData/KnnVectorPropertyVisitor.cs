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