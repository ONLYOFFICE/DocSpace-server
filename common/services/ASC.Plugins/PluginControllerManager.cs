﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using System.Reflection;
using System.Windows.Input;

using ASC.Common;
using ASC.PluginLibrary;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace ASC.Plugins;

[Transient]
public class PluginControllerManager(ApplicationPartManager applicationPartManager, PluginServiceProvider pluginService)
{

    public void AddControllers(Assembly assembly)
    {
        var assemblyPart = new AssemblyPart(assembly);
        applicationPartManager.ApplicationParts.Add(assemblyPart);

        foreach (var type in assembly.GetTypes())
        {
            if (typeof(IPluginStartup).IsAssignableFrom(type))
            {
                var startup = Activator.CreateInstance(type) as IPluginStartup;
                if (startup != null)
                {
                    startup.Configure(pluginService);
                }
            }
        }
        ResetControllActions();
    }

    public void RemoveControllers(string pluginId)
    {
        var last = applicationPartManager.ApplicationParts.FirstOrDefault(m => m.Name == pluginId);
        applicationPartManager.ApplicationParts.Remove(last);

        ResetControllActions();
    }

    private void ResetControllActions()
    {
        PluginActionDescriptorChangeProvider.Instance.HasChanged = true;
       
        if (PluginActionDescriptorChangeProvider.Instance.TokenSource != null)
        {
            PluginActionDescriptorChangeProvider.Instance.TokenSource.Cancel();
        }

    }
}
public class PluginActionDescriptorChangeProvider : IActionDescriptorChangeProvider
{
    public static PluginActionDescriptorChangeProvider Instance { get; } = new PluginActionDescriptorChangeProvider();

    public CancellationTokenSource TokenSource { get; private set; }

    public bool HasChanged { get; set; }

    public IChangeToken GetChangeToken()
    {
        TokenSource = new CancellationTokenSource();
        return new CancellationChangeToken(TokenSource.Token);
    }
}