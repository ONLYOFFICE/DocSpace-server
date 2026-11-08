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


namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// Stands in for the event bus the sending code publishes a letter on. It keeps what was published and delivers
/// nothing: the point is the letter as it leaves for the notify service - its recipients, object and tags - not
/// its delivery.
/// </summary>
internal sealed class RecordingEventBus : IEventBus
{
    // the services' own serializer, so a member the bus does not carry is missing here as well
    private static readonly Lazy<ProtobufSerializer> _serializer = new(() =>
    {
        _ = typeof(NotifyItemIntegrationEvent).Assembly;

        return new ProtobufSerializer();
    });

    public List<IntegrationEvent> Published { get; } = [];

    /// <summary>How many of the next publications fail, for a case about what a failure leaves behind.</summary>
    public int FailuresLeft { get; set; }

    /// <summary>Whether a publication fails after the letter was kept, as when a broker drops the connection late.</summary>
    public bool FailAfterRecording { get; set; }

    public Task PublishAsync(IntegrationEvent @event)
    {
        if (FailuresLeft > 0)
        {
            FailuresLeft--;

            throw new InvalidOperationException("Publishing failed, as the case asked.");
        }

        // what the notify service receives is the event as it comes off the wire, not the sender's object
        var wire = _serializer.Value.Serialize(@event);
        Published.Add((IntegrationEvent)_serializer.Value.Deserialize(wire, @event.GetType()));

        if (FailAfterRecording)
        {
            throw new InvalidOperationException("Publishing failed after the letter left, as the case asked.");
        }

        return Task.CompletedTask;
    }

    // The sending code only publishes. A call here means it changed, and this stand-in stopped standing in for it.
    public Task SubscribeAsync<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T> => throw NotUsed();

    public Task SubscribeDynamicAsync<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler => throw NotUsed();

    public void UnsubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler => throw NotUsed();

    public void Unsubscribe<T, TH>()
        where TH : IIntegrationEventHandler<T>
        where T : IntegrationEvent => throw NotUsed();

    private static NotSupportedException NotUsed([CallerMemberName] string member = "")
    {
        return new NotSupportedException($"The sending code reached IEventBus.{member}, which it never used to.");
    }
}
