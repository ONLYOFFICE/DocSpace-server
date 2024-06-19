// (c) Copyright Ascensio System SIA 2009-2024
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

using ASC.Api.Core.Extensions;
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


public class RedisProtobufSerializer: ISerializer
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
        var aasemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x =>
        {
            var name = x.GetName().Name;
            return !string.IsNullOrEmpty(name) && name.StartsWith("ASC.");
        });

        var redisGeneric = typeof(RedisCacheNotify<>.RedisCachePubSubItem<>);
        var integrationEvent = typeof(IntegrationEvent);
        var types = aasemblies.SelectMany(r => r.GetTypes().Where(t => t.GetCustomAttribute<ProtoContractAttribute>() != null));
        var methodInfo = typeof(Serializer).GetMethod("PrepareSerializer");
        if (methodInfo == null)
        {
            return Task.CompletedTask;
        }
        
        foreach (var t in types)
        {
            try
            {
                if (t == redisGeneric || t == integrationEvent)
                {
                    continue;
                }

                var genericMethod = methodInfo.MakeGenericMethod(t);
                genericMethod.Invoke(null, null);
                    
                if (!t.IsSubclassOf(integrationEvent))
                {
                    var redis = redisGeneric.MakeGenericType(t, t);
                    genericMethod = methodInfo.MakeGenericMethod(redis);
                    genericMethod.Invoke(null, null);
                }
                logger.LogTrace("PrepareSerializer:{ProtoBufFullName}", t.FullName);
            }
            catch (Exception e)
            {
                logger.LogTrace(e, t.FullName);
            }
        }

        return Task.CompletedTask;
    }
}