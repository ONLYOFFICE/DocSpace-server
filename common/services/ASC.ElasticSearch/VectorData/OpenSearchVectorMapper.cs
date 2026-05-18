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
        return type == typeof(ReadOnlyMemory<float>) ||
               type == typeof(ReadOnlyMemory<float>?) ||
               type == typeof(IEnumerable<float>) ||
               type == typeof(IReadOnlyCollection<float>) ||
               type == typeof(ICollection<float>) ||
               type == typeof(IReadOnlyList<float>) ||
               type == typeof(IList<float>) ||
               type == typeof(float[]);
    }
}