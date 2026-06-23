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

using DocSpace.API.SDK.Api.Rooms;

namespace ASC.Data.Stress.Command;

public class ExecuteRoomCreationAndSharingCommand : AsyncCommand<ExecuteRoomCreationAndSharingCommand.Settings>, IBaseCommand
{
    public static string Name => "create-room-and-share";
    public static string Description => "Creates room and invites to them";

    public class Settings : CommandSettings
    {
        [CommandOption("--second-user-email")] 
        public required string SecondUserEmail { get; set; }

        public static Settings Default = new()
        {
            Iterations = 100,
            Email = "test@onlyoffice.com",
            Password = "11111111",
            SecondUserEmail = "paul.bannov@mail.ru"
        };
        
        [CommandOption("--iterations")]
        public required int Iterations { get; set; }

        [CommandOption("--email")]
        public required string Email { get; set; }

        [CommandOption("--password")]
        public required string Password { get; set; }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrEmpty(settings.Email))
        {
            settings.Email = AnsiConsole.Ask("Enter user [green]email[/]:", Settings.Default.Email);
        }  
        
        if (string.IsNullOrEmpty(settings.Password))
        {
            settings.Password = AnsiConsole.Ask("Enter user [green]password[/]:", Settings.Default.Password);
        }
        
        if (string.IsNullOrEmpty(settings.SecondUserEmail))
        {
            settings.SecondUserEmail = AnsiConsole.Ask("Enter second user [green]email[/]:", Settings.Default.SecondUserEmail);
        }  
        
        if (settings.Iterations == 0)
        {
            settings.Iterations = AnsiConsole.Ask("Enter number of [green]iterations[/]:", Settings.Default.Iterations);
        }
        
        return ValidationResult.Success();
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var configuration = await ApiHelper.GetConfigurationAsync(settings.Email, settings.Password);
        
        using var roomsApi = new RoomsApi(configuration);
        using var profilesApi = new ProfilesApi(configuration);
        var secondUser = (await profilesApi.GetProfileByEmailAsync(settings.SecondUserEmail, cancellationToken: cancellationToken)).Response;
        
        var system = new Faker().System;
        var token = CancellationToken.None;

        await ExecuteFileCreationAndSharing(roomsApi, system, secondUser.Id, settings.Iterations, token);
        return 0;
    }

    private static async Task ExecuteFileCreationAndSharing(RoomsApi roomsApi,  Bogus.DataSets.System system, Guid user2, int iterations1, CancellationToken token)
    {
        var p = 100.0 / iterations1;
        
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {        
                var ctxTask = ctx.AddTask("[green]Create room and invite[/]");
                
                List<Task> tasks = [];

                var j = 0;
                foreach (var i in Enumerable.Range(1, iterations1))
                {
                    tasks.Add(CreateAndShareRoom());
                    j++;
                    if (j == 100)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                        j = 0;
                    }
                }

                return;

                async Task CreateAndShareRoom()
                {
                    var room = (await roomsApi.CreateRoomAsync(new CreateRoomRequestDto(system.CommonFileName(""), roomType: RoomType.CustomRoom), token)).Response;

                    await roomsApi.SetRoomSecurityAsync(room.Id,
                        new RoomInvitationRequest
                        {
                            Invitations = [new RoomInvitation { Id = user2, Access = FileShare.Read }]
                        }, token);
                    
                    ctxTask.Increment(p);
                }
            });
    }
}