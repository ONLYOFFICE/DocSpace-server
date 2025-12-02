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

using Microsoft.Extensions.Configuration;

namespace ASC.Api.Documentation;

public class Settings : CommandSettings
{
    [CommandOption("--baseUrl")]
    public required string BaseUrl { get; set; }
    
    [CommandOption("--pathToConf")]
    public required string PathToConf { get; set; }

    [CommandOption("--silent")] 
    public string Silent { get; set; } = "n";

    public Dictionary<string, string> Endpoints { get; set; } = new();
    
    public override ValidationResult Validate()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var silent = Silent == "y";
        
        var baseUrlFromConfig = configuration["baseUrl"]!.TrimEnd('/');
        BaseUrl = silent ? baseUrlFromConfig : AnsiConsole.Ask("Base [link]url[/]:", baseUrlFromConfig);
        
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(BaseUrl);
        var isSuccess = httpClient.GetAsync("").Result.IsSuccessStatusCode;
        if (!isSuccess)
        {
            return ValidationResult.Error("Failed to fetch endpoint: " + BaseUrl);
        }

        var pathFromConf = Path.GetFullPath(Path.Combine([AppContext.BaseDirectory, ..configuration.GetSection("pathToConf").Get<IEnumerable<string>>()!]));
        PathToConf = silent ? pathFromConf : AnsiConsole.Ask("Base [green]path[/]:", pathFromConf);
        
        if (!Path.Exists(PathToConf))
        {
            return ValidationResult.Error("File not found: " + PathToConf);
        }
        
        configuration = new ConfigurationBuilder()
            .AddJsonFile(PathToConf)
            .Build();

        Endpoints = configuration.GetSection("openApi:endpoints").Get<Dictionary<string, string>>()!;
        
        return ValidationResult.Success();
    }
}