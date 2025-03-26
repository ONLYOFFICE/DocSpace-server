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

using ASC.Files.Tests.Data;
using ASC.Files.Tests.Models;
using ASC.Web.Files.Services.WCFService.FileOperations;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class BaseTest(
    FilesApiFactory filesFactory, 
    WebApplicationFactory<WebApiProgram> apiFactory, 
    WebApplicationFactory<PeopleProgram> peopleFactory,
    WebApplicationFactory<FilesServiceProgram> filesServiceProgram
    ) : IAsyncLifetime
{
    protected readonly HttpClient _filesClient = filesFactory.HttpClient;
    private readonly Func<Task> _resetDatabase = filesFactory.ResetDatabaseAsync;
    protected readonly FilesApiFactory _filesFactory = filesFactory;

    public async ValueTask InitializeAsync()
    {
        await Initializer.InitializeAsync(_filesFactory, apiFactory, peopleFactory, filesServiceProgram);
    }

    public async ValueTask DisposeAsync()
    {
        await _resetDatabase();
    }
    
    protected async Task<FileDto<int>> CreateFile(string? fileName, FolderType folderType, User user)
    {
        await _filesClient.Authenticate(user);
        
        var response = await _filesClient.GetAsync("@root", TestContext.Current.CancellationToken);
        var rootFolder = await HttpClientHelper.ReadFromJson<IEnumerable<FolderContentDto>>(response);
        var folderId = rootFolder.FirstOrDefault(r => r.Current.RootFolderType == folderType).Current.Id;
        
        return await CreateFile(fileName, folderId);
    }
    
    protected async Task<FileDto<int>> CreateFile(string? fileName, int folderId)
    {
        var file = new CreateFile<JsonElement> { Title = fileName };
        
        var response = await _filesClient.PostAsJsonAsync($"{folderId}/file", file, _filesFactory.JsonRequestSerializerOptions);
        var createdFile = await HttpClientHelper.ReadFromJson<FileDto<int>>(response);
        
        return createdFile;
    }
    
    protected async Task<List<FileOperationResult>?> WaitLongOperation()
    {
        List<FileOperationResult>? statuses;

        while (true)
        {
            var response = await _filesClient.GetAsync("fileops", TestContext.Current.CancellationToken);
            statuses = await HttpClientHelper.ReadFromJson<List<FileOperationResult>>(response);

            if (statuses.TrueForAll(r => r.Finished))
            {
                break;
            }
            await Task.Delay(100);
        }

        return statuses;
    }
}