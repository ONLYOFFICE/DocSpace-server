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

internal static class OpenSearchVectorMapper
{
    private static readonly Type _vectorType = typeof(KnnVector);
    
    private const string SupportedVectorTypes =
        "ReadOnlyMemory<float>, " +
        "IEnumerable<float>, " +
        "IReadOnlyCollection<float>, " +
        "ICollection<float>, " +
        "IReadOnlyList<float>, " +
        "IList<float>, " +
        "or float[]";
    
    public static IProperties BuildPropertyMappings(Type model, int dimension)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dimension), @"The dimension must be greater than 0");
        }
        
        var vectorProperties = model.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttributes(_vectorType, false).Length != 0)
            .ToList();
        
        if (vectorProperties.Count == 0)
        {
            throw new InvalidOperationException("The data type must contain at least one vector type");
        }

        var walker = new PropertyWalker(model, new KnnVectorPropertyVisitor());
        var properties = walker.GetProperties();

        foreach (var property in vectorProperties)
        {
            if (!IsVectorPropertyTypeValid(property.PropertyType))
            {
                throw new NotSupportedException($"{property.PropertyType} is not supported. Supported types: {SupportedVectorTypes}");
            }
            
            var propertyExp = Expression.Property(Expression.Parameter(model), property);
            var name = new PropertyName(propertyExp);

            properties[name] = new KnnVectorProperty
            {
                Dimension = dimension,
                Method = new KnnMethod
                {
                    Name = "hnsw",
                    Engine = "faiss"
                }
            };
        }

        return properties;
    }
    
    private static bool IsVectorPropertyTypeValid(Type type)
    {
        return (type == typeof(ReadOnlyMemory<float>)) ||
               (type == typeof(ReadOnlyMemory<float>?)) ||
               (type == typeof(IEnumerable<float>)) ||
               (type == typeof(IReadOnlyCollection<float>)) ||
               (type == typeof(ICollection<float>)) ||
               (type == typeof(IReadOnlyList<float>)) ||
               (type == typeof(IList<float>)) ||
               (type == typeof(float[]));
    }
}