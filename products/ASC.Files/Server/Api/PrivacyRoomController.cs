// (c) Copyright Ascensio System SIA 2010-2023
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


[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("privacyroom")]
public class PrivacyRoomControllerCommon(AuthContext authContext,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
        MessageService messageService,
        IMapper mapper)
    : ControllerBase
{
    [HttpPost("keys")]
    public Task<IEnumerable<EncryptionKeyDto>> SetKeysAsync(IEnumerable<EncryptionKeyRequestDto> inDto)
    {
        return CreateKeysAsync(inDto, false);
    }
    
    [HttpPut("keys")]
    public Task<IEnumerable<EncryptionKeyDto>> ReplaceKeyAsync(EncryptionKeyRequestDto inDto)
    {
        return CreateKeysAsync([inDto], true);
    }
    
    /// <summary>
    /// Returns a key pair for the current user.
    /// </summary>
    /// <short>Get encryption keys</short>
    /// <returns type="ASC.Web.Files.Core.Entries.EncryptionKeyPairDto, ASC.Files.Core">Encryption key pair: private key, public key, user ID</returns>
    /// <path>api/2.0/privacyroom/keys</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet("keys/filter")]
    public async Task<EncryptionKeyDto> GetKeysAsync(string id, EncryptionKeyType? type, string version, string publicKey, string privateKey)
    {
        await Demand();

        return (await encryptionKeyPairHelper.GetKeyPairAsync()).FirstOrDefault(r =>
        {
            var result = false;
            
            if (!string.IsNullOrEmpty(id))
            {
                if (!r.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                result = true;
            }
            
            if (type.HasValue)
            {                
                if (r.Type != type.Value)
                {
                    return false;
                }
                
                result = true;
            }
            
            if (!string.IsNullOrEmpty(version))
            {
                if (!r.Version.Equals(version, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                
                result = true;
            }
            
            if (!string.IsNullOrEmpty(publicKey))
            {
                if (!r.PublicKey.Equals(publicKey, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                
                result = true;
            }
            
            if (!string.IsNullOrEmpty(privateKey))
            {
                if (!r.PrivateKey.Equals(privateKey, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                
                result = true;
            }

            return result;
        });
    }
    
    [HttpGet("keys")]
    public async Task<IEnumerable<EncryptionKeyDto>> GetKeysAsync()
    {
        await Demand();

        return await encryptionKeyPairHelper.GetKeyPairAsync();
    }
    
    [HttpDelete("keys/{id}")]
    public async Task<IEnumerable<EncryptionKeyDto>> DeleteKeysAsync(string id)
    {
        await Demand();

        return await encryptionKeyPairHelper.DeleteAsync(id);
    }
    
    /// <summary>
    /// Checks if the Private Room settings are enabled or not.
    /// </summary>
    /// <short>Check the Private Room settings</short>
    /// <returns type="System.Boolean, System">Boolean value: true - the Private Room settings are enabled, false - the Private Room settings are disabled</returns>
    /// <path>api/2.0/privacyroom</path>
    /// <httpMethod>GET</httpMethod>
    /// <visible>false</visible>
    [HttpGet]
    public async Task<bool> PrivacyRoomAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await PrivacyRoomSettings.GetEnabledAsync(settingsManager);
    }
    
    [HttpPut]
    public async Task<bool> SetPrivacyRoomAsync(PrivacyRoomRequestDto inDto)
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
    
    private async Task Demand()
    {
        await permissionContext.DemandPermissionsAsync(new UserSecurityProvider(authContext.CurrentAccount.ID), Constants.Action_EditUser);

        if (!await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            throw new SecurityException();
        }
    }
    
    private async Task<IEnumerable<EncryptionKeyDto>> CreateKeysAsync(IEnumerable<EncryptionKeyRequestDto> inDto, bool replace)
    {
        await Demand();

        return await encryptionKeyPairHelper.SetKeyPairAsync(mapper.Map<IEnumerable<EncryptionKeyDto>>(inDto), replace);
    }
}
