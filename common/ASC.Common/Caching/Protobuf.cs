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

using ASC.EventBus.Events;

using SerializationContext = Confluent.Kafka.SerializationContext;

namespace ASC.Common.Caching;

public class ProtobufSerializer<T> : ISerializer<T> where T : new()
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return BaseProtobufSerializer.Serialize(data);
    }
}

public class ProtobufDeserializer<T> : IDeserializer<T> where T : new()
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return BaseProtobufSerializer.Deserialize<T>(data);
    }
}


public class RedisProtobufSerializer : ISerializer
{
    public byte[] Serialize<T>(T item)
    {
        return BaseProtobufSerializer.Serialize(item);
    }

    public T Deserialize<T>(byte[] serializedObject)
    {
        return BaseProtobufSerializer.Deserialize<T>(serializedObject);
    }
}

public class BaseProtobufSerializer
{
    public static byte[] Serialize<T>(T data)
    {
        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, data);
        return memoryStream.ToArray();
    }

    public static T Deserialize<T>(byte[] data)
    {
        return Deserialize<T>(new ReadOnlySpan<byte>(data));
    }

    public static T Deserialize<T>(ReadOnlySpan<byte> data)
    {
        return Serializer.Deserialize<T>(data);
    }
}

public class WarmupProtobufStartupTask(ILogger<WarmupProtobufStartupTask> logger) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var startTime = TimeProvider.System.GetTimestamp();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x =>
        {
            var name = x.GetName().Name;
            return !string.IsNullOrEmpty(name) && name.StartsWith("ASC.");
        });

        var redisGeneric = typeof(RedisCacheNotify<>.RedisCachePubSubItem<>);
        var integrationEvent = typeof(IntegrationEvent);
        var types = assemblies.SelectMany(r => r.GetTypes().Where(t => t.GetCustomAttribute<ProtoContractAttribute>() != null));
        var methodInfo = typeof(Serializer).GetMethod("PrepareSerializer");
        if (methodInfo == null)
        {
            return Task.CompletedTask;
        }

        var typesCount = 0;

        foreach (var t in types)
        {
            try
            {
                if (t == redisGeneric || t == integrationEvent || t.IsAbstract)
                {
                    continue;
                }

                var closedType = t.IsGenericType ? t.MakeGenericType(typeof(int)) : t;

                var genericMethod = methodInfo.MakeGenericMethod(closedType);
                genericMethod.Invoke(null, null);

                if (!t.IsSubclassOf(integrationEvent))
                {
                    var redis = redisGeneric.MakeGenericType(closedType, closedType);
                    genericMethod = methodInfo.MakeGenericMethod(redis);
                    genericMethod.Invoke(null, null);
                }

                typesCount++;
                logger.TracePrepareSerializer(t.FullName);
            }
            catch (Exception e)
            {
                logger.TracePrepareSerializerFailed(t.FullName, e);
            }
        }

        logger.TraceWarmupProtobufFinished(typesCount, TimeProvider.System.GetElapsedTime(startTime).TotalMilliseconds);

        return Task.CompletedTask;
    }
}