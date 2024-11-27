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

namespace ASC.Files.Tests1.FilesController;

[Collection("Test Collection")]
public class CreateFileControllerTest(FilesApiFactory filesFactory) : IAsyncLifetime
{
    private readonly HttpClient _filesClient = filesFactory.HttpClient;
    private readonly Func<Task> _resetDatabase = filesFactory.ResetDatabaseAsync;
    
    [Fact]
    public async Task Create_ReturnsOk()
    {
        //Arrange
        var file = new CreateFile<JsonElement> { Title = "test.docx" };
        
        //Act
        var response = await _filesClient.PostAsJsonAsync("api/2.0/files/2/file", file, filesFactory.JsonRequestSerializerOptions);
        var createdFile = await filesFactory.ReadFromJson<FileDto<int>>(response);
        
        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(createdFile);
        Assert.Equal("test.docx", createdFile.Title);
    }
    
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }
    
}