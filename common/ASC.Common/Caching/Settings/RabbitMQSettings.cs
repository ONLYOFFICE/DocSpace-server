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

namespace ASC.Common.Caching;
public class RabbitMQSettings
{
    private const ushort DefaultChannelMax = ConnectionFactory.DefaultChannelMax * 8;

    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int Port { get; set; }
    public string VirtualHost { get; set; }
    public string Uri { get; set; }
    public bool EnableSsl { get; set; }
    public string SslServerName { get; set; }
    public string SslCertPath { get; set; }
    public string ClientProvidedName { get; set; }
    public ushort RequestedChannelMax { get; set; } = DefaultChannelMax;

    public ConnectionFactory GetConnectionFactory()
    {
        var factory = new ConnectionFactory
        {
            ClientProvidedName = ClientProvidedName,
            RequestedChannelMax = RequestedChannelMax
        };

        if (!string.IsNullOrEmpty(Uri))
        {
            factory.Uri = new Uri(Uri);
        }
        else
        {
            factory.HostName = HostName;
            factory.UserName = UserName;
            factory.Password = Password;
            factory.Port = Port;
            factory.VirtualHost = VirtualHost;
        }

        if (EnableSsl)
        {
            factory.Ssl = new SslOption
            {
                Enabled = EnableSsl,
                Version = SslProtocols.Tls12
            };

            if (!string.IsNullOrEmpty(SslCertPath))
            {
                factory.Ssl.CertPath = SslCertPath;
                factory.Ssl.ServerName = SslServerName;
            }
        }

        return factory;
    }
}