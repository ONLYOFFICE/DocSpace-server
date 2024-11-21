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

namespace ASC.Api.Documents;

[ConstraintRoute("int")]
public class PrivacyRoomControllerInternal(SettingsManager settingsManager,
        EncryptionKeyPairDtoHelper encryptionKeyPairHelper, FileStorageService fileStorageService)
    : PrivacyRoomController<int>(settingsManager, encryptionKeyPairHelper, fileStorageService);

public class PrivacyRoomControllerThirdparty(SettingsManager settingsManager,
        EncryptionKeyPairDtoHelper encryptionKeyPairHelper, FileStorageService fileStorageService)
    : PrivacyRoomController<string>(settingsManager, encryptionKeyPairHelper, fileStorageService);

/// <summary>
/// Provides access to Private Room.
/// </summary>
/// <name>privacyroom</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("privacyroom")]
public abstract class PrivacyRoomController<T>(SettingsManager settingsManager,
        EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
        FileStorageService fileStorageService)
    : ControllerBase
{
    /// <summary>
    /// Returns all the key pairs of the users who have access to the file with the ID specified in the request.
    /// </summary>
    /// <short>Get file key pairs</short>
    /// <path>api/2.0/privacyroom/access/{fileId}</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Private room")]
    [SwaggerResponse(200, "List of encryption key pairs", typeof(EncryptionKeyPairDto))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [HttpGet("access/{fileId}")]
    public async Task<IEnumerable<EncryptionKeyPairDto>> GetPublicKeysWithAccess(FileIdRequestDto<T> inDto)
    {
        if (!await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            throw new SecurityException();
        }

        return await encryptionKeyPairHelper.GetKeyPairAsync(inDto.FileId, fileStorageService);
    }
}

[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("privacyroom")]
public class PrivacyRoomControllerCommon(AuthContext authContext,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
        MessageService messageService,
        ILoggerProvider option)
    : ControllerBase
{
    private readonly ILogger _logger = option.CreateLogger("ASC.Api.Documents");

    /// <summary>
    /// Returns a key pair for the current user.
    /// </summary>
    /// <short>Get encryption keys</short>
    /// <path>api/2.0/privacyroom/keys</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Private room")]
    [SwaggerResponse(200, "Encryption key pair: private key, public key, user ID", typeof(EncryptionKeyPairDto))]
    [SwaggerResponse(403, "You don't have enough permission to this operation")]
    [HttpGet("keys")]
    public async Task<EncryptionKeyPairDto> GetKeysAsync()
    {
        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(authContext.CurrentAccount.ID), Constants.Action_EditUser);

        if (!await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            throw new SecurityException();
        }

        return await encryptionKeyPairHelper.GetKeyPairAsync();
    }


    /// <summary>
    /// Checks if the Private Room settings are enabled or not.
    /// </summary>
    /// <short>Check the Private Room settings</short>
    /// <path>api/2.0/privacyroom</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Private room")]
    [SwaggerResponse(200, "Boolean value: true - the Private Room settings are enabled, false - the Private Room settings are disabled", typeof(bool))]
    [HttpGet("")]
    public async Task<bool> PrivacyRoomAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await PrivacyRoomSettings.GetEnabledAsync(settingsManager);
    }

    /// <summary>
    /// Sets the key pair for the current user.
    /// </summary>
    /// <short>Set encryption keys</short>
    /// <path>api/2.0/privacyroom/keys</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Private room")]
    [SwaggerResponse(200, "Boolean value: true - the key pair is set", typeof(object))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpPut("keys")]
    public async Task<object> SetKeysAsync(PrivacyRoomRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(authContext.CurrentAccount.ID), Constants.Action_EditUser);

        if (!await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            throw new SecurityException();
        }

        var keyPair = await encryptionKeyPairHelper.GetKeyPairAsync();
        if (keyPair != null)
        {
            if (!string.IsNullOrEmpty(keyPair.PublicKey) && !inDto.Update)
            {
                return new { isset = true };
            }

            _logger.InformationUpdateAddress(authContext.CurrentAccount.ID);
        }

        await encryptionKeyPairHelper.SetKeyPairAsync(inDto.PublicKey, inDto.PrivateKeyEnc);

        return new
        {
            isset = true
        };
    }

    /// <summary>
    /// Enables the Private Room settings.
    /// </summary>
    /// <short>Enable the Private Room settings</short>
    /// <path>api/2.0/privacyroom</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Private room")]
    [SwaggerResponse(200, "Boolean value: true - the Private Room settings are enabled, false - the Private Room settings are disabled", typeof(bool))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPut("")]
    public async Task<bool> SetPrivacyRoomAsync(PrivacyRoomEnableRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inDto.Enable)
        {
            if (!PrivacyRoomSettings.IsAvailable())
            {
                throw new BillingException(Resource.ErrorNotAllowedOption, "PrivacyRoom");
            }
        }

        await PrivacyRoomSettings.SetEnabledAsync(settingsManager, inDto.Enable);

        await messageService.SendAsync(inDto.Enable ? MessageAction.PrivacyRoomEnable : MessageAction.PrivacyRoomDisable);

        return inDto.Enable;
    }
}
