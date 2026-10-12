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

namespace ASC.Site.Core.Controllers;

[Scope]
[ApiController]
[Route("[controller]")]
public class KeyController(
        ValidationKeyProvider validationKeyProvider,
        ILogger<KeyController> logger)
    : ControllerBase
{
    [HttpPost("generate")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<GenerateKeysResponseDto> GenerateKeys(GenerateKeysRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Email))
            {
                return null;
            }

            var encryptedEmail = await validationKeyProvider.EncryptAndEncode(inDto?.Email);

            var linkKey = validationKeyProvider.GetKey(encryptedEmail);

            return new GenerateKeysResponseDto(encryptedEmail, linkKey);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }

    [HttpPost("validate")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<ValideteKeysResponseDto> ValideteKeys(ValideteKeysRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.EmailKey) || string.IsNullOrEmpty(inDto?.LinkKey))
            {
                return null;
            }

            var decryptedEmail = await validationKeyProvider.DecodeAndDecrypt(inDto.EmailKey);

            var cacheKey = $"{inDto.Page}{inDto.LinkKey}";

            var isValid = validationKeyProvider.ValidateKey(cacheKey, inDto.EmailKey, inDto.LinkKey);

            return new ValideteKeysResponseDto(decryptedEmail, isValid);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }

    [HttpPost("generateunsubscribeid")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<GenerateUnsubscribeIdResponseDto> GenerateUnsubscribeId(GenerateUnsubscribeIdRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.Email))
            {
                return null;
            }

            var unsubscribeId = await validationKeyProvider.EncryptAndEncode(inDto.Email);

            return new GenerateUnsubscribeIdResponseDto(unsubscribeId);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }

    [HttpPost("validateunsubscribeid")]
    [Authorize(AuthenticationSchemes = "auth:allowskip:default")]
    public async Task<ValideteUnsubscribeIdResponseDto> ValidateUnsubscribeId(ValideteUnsubscribeIdRequestDto inDto)
    {
        try
        {
            if (string.IsNullOrEmpty(inDto?.UnsubscribeId))
            {
                return null;
            }

            var email = await validationKeyProvider.DecodeAndDecrypt(inDto.UnsubscribeId);

            return new ValideteUnsubscribeIdResponseDto(email);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw;
        }
    }
}
