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

extern alias ASCWebApi;
extern alias ASCPeople;
using System.Net.Http.Json;
using System.Reflection;

using ASC.Files.Tests.ApiFactories;

namespace ASC.Files.Tests.Tests._08_Privacy;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class PrivacyRoomTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task CRUDUserPrivateKey()
    {
        await _filesClient.Authenticate(Initializer.Owner);

        var (publicKey, privateKey, _) = ExportPublicAndPrivateKeys();

        var keys = (await _privacyroomApi.SetKeysAsync(new EncryptionKeyRequestDto(publicKey, privateKey), cancellationToken: TestContext.Current.CancellationToken)).Response;

        keys.Should().NotBeNull();
        keys.Should().HaveCount(1);
        keys[0].PublicKey.Should().BeEquivalentTo(publicKey);
        keys[0].PrivateKeyEnc.Should().BeEquivalentTo(privateKey);

        keys = (await _privacyroomApi.GetUserKeysAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        keys.Should().NotBeNull();
        keys.Should().HaveCount(1);
        keys[0].PublicKey.Should().BeEquivalentTo(publicKey);
        keys[0].PrivateKeyEnc.Should().BeEquivalentTo(privateKey);

        keys = (await _privacyroomApi.DeleteKeysAsync(keys[0].Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        keys.Should().NotBeNull();
        keys.Should().BeEmpty();
    }

    [Fact]
    public async Task SetFileAccess()
    {
        await _filesClient.Authenticate(Initializer.Owner);

        var (userPublicKey, userPrivateKeyEnc, userPassword) = ExportPublicAndPrivateKeys();
        
        var userKeys = (await _privacyroomApi.SetKeysAsync(new EncryptionKeyRequestDto(userPublicKey, userPrivateKeyEnc), cancellationToken: TestContext.Current.CancellationToken)).Response;
        var userKey = userKeys[0];
        
        var createRequest = new CreateRoomRequestDto(
            title: "private room",
            roomType: RoomType.CustomRoom,
            @private: true
        );
        
        var createdRoom = (await _roomsApi.CreateRoomAsync(createRequest, TestContext.Current.CancellationToken)).Response;
        var roomKeys = (await _privacyroomApi.GetUserKeysForRoomAsync(createdRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        roomKeys.Should().NotBeNull();
        roomKeys.Should().HaveCount(1);
        
        var aesKeyForEncrypt = RandomNumberGenerator.GetBytes(32);
        var filePrivateKeyEnc = EncryptFilePassword(userPublicKey, aesKeyForEncrypt);

        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.docx")!;
        await using var encryptTempStream = new MemoryStream();
        await FileEncryptionStream.EncryptFileAsync(stream, encryptTempStream, aesKeyForEncrypt, TestContext.Current.CancellationToken);

        var uploadSession = (await _filesOperationsApi.CreateUploadSessionAsync(createdRoom.Id, new SessionRequest("new.docx", encryptTempStream.Length), TestContext.Current.CancellationToken)).Response;
        var uploadSessionData = JsonSerializer.Deserialize<SessionData>(((JsonElement)uploadSession).ToString(), JsonSerializerOptions.Web)!;
        
        encryptTempStream.Position = 0;
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StreamContent(encryptTempStream), "file", "new.docx");
        await _filesClient.PostAsync(uploadSessionData.Data.Location + "&chunkNumber=1&upload=true", formContent, TestContext.Current.CancellationToken);
        var uploadResponse = await _filesClient.PostAsync(uploadSessionData.Data.Location + "&finalize=true", null, TestContext.Current.CancellationToken);
        var fileId = (await uploadResponse.Content.ReadFromJsonAsync<FileData>(TestContext.Current.CancellationToken))!.Data.Id;
        
        List<AccessRequestKeyDto> keys = [new(roomKeys[0].UserId, roomKeys[0].Id, filePrivateKeyEnc)];
        await _filesApi.SetEncryptionInfoAsync(fileId, keys, cancellationToken: TestContext.Current.CancellationToken);
        
        var result = (await _filesApi.GetEncryptionInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        result.Should().NotBeNull();
        result.UserKeys.Should().NotBeNullOrEmpty();
        result.UserKeys[0].UserId.Should().Be(userKey.UserId);
        result.UserKeys[0].Id.Should().Be(userKey.Id);
        
        result.FileKeys[0].FileId.Should().Be(fileId);
        result.FileKeys[0].UserId.Should().Be(userKey.UserId);
        result.FileKeys[0].PublicKeyId.Should().Be(userKey.Id);
        result.FileKeys[0].PrivateKeyEnc.Should().Be(filePrivateKeyEnc);

        var aesKeyForDecrypt = DecryptFilePassword(result.FileKeys[0].PrivateKeyEnc, DecryptPrivateEncKey(result.UserKeys[0].PrivateKeyEnc, userPassword));
        aesKeyForDecrypt.Should().BeEquivalentTo(aesKeyForEncrypt);
        
        var configuration = (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var fileStream = await _filesClient.GetStreamAsync(configuration.ViewUrl, TestContext.Current.CancellationToken);
        
        await using var fileTempStream = new MemoryStream();
        await fileStream.CopyToAsync(fileTempStream, TestContext.Current.CancellationToken);

        fileTempStream.Position = 0;
        await using var decryptTempStream = new MemoryStream();
        await FileEncryptionStream.DecryptFileAsync(fileTempStream, decryptTempStream, aesKeyForEncrypt, TestContext.Current.CancellationToken);

        AreStreamsEqual(decryptTempStream, stream).Should().BeTrue();
    }
    
    private static Key ExportPublicAndPrivateKeys()
    {
        using var rsa = RSA.Create(2048);
        var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
        var pbeParameters = new PbeParameters(
            PbeEncryptionAlgorithm.Aes256Cbc,
            HashAlgorithmName.SHA256,
            iterationCount: 100000);
        
        var password = Initializer.FakerMember.Generate().Password;
        
        var privateKey = Convert.ToBase64String(rsa.ExportEncryptedPkcs8PrivateKey(password, pbeParameters));
        return new Key(publicKey, privateKey, password);
    }

    private static string DecryptPrivateEncKey(string encryptedPrivateKey, string password)
    {
        using var rsa = RSA.Create();
        var privateKeyBytes = Convert.FromBase64String(encryptedPrivateKey);
        rsa.ImportEncryptedPkcs8PrivateKey(password, privateKeyBytes, out _);
        return Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
    }

    private static string EncryptFilePassword(string userPublicKey, byte[] filePassword)
    {
        using var rsaPublic = RSA.Create();
        rsaPublic.ImportSubjectPublicKeyInfo(Convert.FromBase64String(userPublicKey), out _);
        var encryptedFilePassword = rsaPublic.Encrypt(filePassword, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encryptedFilePassword);
    }

    private static byte[] DecryptFilePassword(string encryptedFilePassword, string userPrivateKey)
    {
        using var rsa = RSA.Create();
        var privateKeyBytes = Convert.FromBase64String(userPrivateKey);
        rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
        var decryptedBytes = rsa.Decrypt(Convert.FromBase64String(encryptedFilePassword), RSAEncryptionPadding.OaepSHA256);
        return decryptedBytes;
    }

    private static bool AreStreamsEqual(Stream stream1, Stream stream2)
    {
        stream1.Position = 0;
        stream2.Position = 0;

        using var sha256 = SHA256.Create();
        var hash1 = sha256.ComputeHash(stream1);
    
        stream2.Position = 0;
        var hash2 = sha256.ComputeHash(stream2);

        return hash1.SequenceEqual(hash2);
    }
}

public record Key(string PublicKey, string PrivateKey, string Password);
public record SessionData(Session Data);
public record Session(string Location);
public record FileData(File Data);
public record File(int Id);