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

namespace ASC.Core.Common.Notify.Model;

public class NotifyServiceCfg
{
    public string ConnectionStringName { get; set; }
    public int StoreMessagesDays { get; set; }
    public NotifyServiceCfgProcess Process { get; set; }
    public List<NotifyServiceCfgSender> Senders { get; set; }
    public List<NotifyServiceCfgScheduler> Schedulers { get; set; }
}

public class NotifyServiceCfgProcess
{
    public int MaxThreads { get; set; }
    public int BufferSize { get; set; }
    public int MaxAttempts { get; set; }
    public string AttemptsInterval { get; set; }

    public void Init()
    {
        if (MaxThreads == 0)
        {
            MaxThreads = Environment.ProcessorCount;
        }
    }
}

public class NotifyServiceCfgSender
{
    public string Name { get; set; }
    public string Type { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    public INotifySender NotifySender { get; set; }
}

public class NotifyServiceCfgScheduler
{
    public string Name { get; set; }
    public string Register { get; set; }
    public MethodInfo MethodInfo { get; set; }

    public void Init()
    {
        var typeName = Register[..Register.IndexOf(',')];
        var assemblyName = Register[Register.IndexOf(',')..];
        var type = Type.GetType(string.Concat(typeName.AsSpan(0, typeName.LastIndexOf('.')), assemblyName), true);
        MethodInfo = type.GetMethod(typeName[(typeName.LastIndexOf('.') + 1)..], BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
    }
}