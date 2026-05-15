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

#nullable enable

namespace ASC.Files.Core.ApiModels;

public static class ModelBindingContextExtension
{
    extension(ModelBindingContext bindingContext)
    {
        internal bool GetFirstValue(string modelName, out string? firstValue)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
                bindingContext.ModelState.MarkFieldValid(modelName);
                firstValue = valueProviderResult.FirstValue;

                return true;
            }

            firstValue = null;

            return false;
        }

        internal bool GetBoolValue(string modelName, out bool firstValue)
        {
            if (GetFirstValue(bindingContext, modelName, out var deleteAfterValue) &&
                bool.TryParse(deleteAfterValue, out var deleteAfter))
            {
                firstValue = deleteAfter;

                return true;
            }

            firstValue = false;

            return false;
        }

        internal List<JsonElement> ParseQuery(string modelName)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

                return valueProviderResult.Select(ParseQueryParam).ToList();
            }

            if (modelName.EndsWith("[]", StringComparison.Ordinal))
            {
                return [];
            }

            return ParseQuery(bindingContext, $"{modelName}[]");
        }

        internal List<DownloadRequestItemDto> ParseDictionary(string modelName)
        {
            var result = new List<DownloadRequestItemDto>();

            for (var i = 0; ; i++)
            {
                var keyProviderResult = bindingContext.ValueProvider.GetValue($"{modelName}[{i}][key]");
                var valueProviderResult = bindingContext.ValueProvider.GetValue($"{modelName}[{i}][value]");
                var passwordProviderResult = bindingContext.ValueProvider.GetValue($"{modelName}[{i}][password]");

                if (keyProviderResult != ValueProviderResult.None && valueProviderResult != ValueProviderResult.None)
                {
                    bindingContext.ModelState.SetModelValue(modelName, keyProviderResult);
                    bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
                    bindingContext.ModelState.SetModelValue(modelName, passwordProviderResult);

                    if (!string.IsNullOrEmpty(keyProviderResult.FirstValue) && !string.IsNullOrEmpty(valueProviderResult.FirstValue))
                    {
                        result.Add(new DownloadRequestItemDto { Key = ParseQueryParam(keyProviderResult.FirstValue), Value = valueProviderResult.FirstValue, Password = passwordProviderResult.FirstValue });
                    }
                }
                else
                {
                    break;
                }
            }

            return result;
        }
    }

    public static JsonElement ParseQueryParam(string? data)
    {
        if (int.TryParse(data, out _))
        {
            return JsonSerializer.Deserialize<JsonElement>(data);
        }

        return JsonSerializer.Deserialize<JsonElement>($"\"{data}\"");
    }
}


public class BaseBatchModelBinder : IModelBinder
{
    public virtual Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var result = new BaseBatchRequestDto();

        result.FileIds = bindingContext.ParseQuery(nameof(result.FileIds));
        result.FolderIds = bindingContext.ParseQuery(nameof(result.FolderIds));

        bindingContext.Result = ModelBindingResult.Success(result);

        return Task.CompletedTask;
    }
}

public class DeleteBatchModelBinder : BaseBatchModelBinder
{
    public override Task BindModelAsync(ModelBindingContext bindingContext)
    {
        base.BindModelAsync(bindingContext);
        ArgumentNullException.ThrowIfNull(bindingContext);

        var result = new DeleteBatchRequestDto();

        if (bindingContext.Result.Model is not BaseBatchRequestDto baseResult)
        {
            bindingContext.Result = ModelBindingResult.Success(result);

            return Task.CompletedTask;
        }

        result.FileIds = baseResult.FileIds;
        result.FolderIds = baseResult.FolderIds;

        if (bindingContext.GetBoolValue(nameof(result.DeleteAfter), out var deleteAfter))
        {
            result.DeleteAfter = deleteAfter;
        }

        if (bindingContext.GetBoolValue(nameof(result.Immediately), out var immediately))
        {
            result.Immediately = immediately;
        }

        bindingContext.Result = ModelBindingResult.Success(result);

        return Task.CompletedTask;
    }
}

public class DownloadModelBinder : BaseBatchModelBinder
{
    public override Task BindModelAsync(ModelBindingContext bindingContext)
    {
        base.BindModelAsync(bindingContext);
        ArgumentNullException.ThrowIfNull(bindingContext);

        var result = new DownloadRequestDto();

        if (bindingContext.Result.Model is not BaseBatchRequestDto baseResult)
        {
            bindingContext.Result = ModelBindingResult.Success(result);

            return Task.CompletedTask;
        }

        result.FileIds = baseResult.FileIds;
        result.FolderIds = baseResult.FolderIds;
        result.FileConvertIds = bindingContext.ParseDictionary(nameof(result.FileConvertIds));

        bindingContext.Result = ModelBindingResult.Success(result);

        return Task.CompletedTask;
    }
}

public class BatchModelBinder : BaseBatchModelBinder
{
    public override Task BindModelAsync(ModelBindingContext bindingContext)
    {
        base.BindModelAsync(bindingContext);
        ArgumentNullException.ThrowIfNull(bindingContext);

        var result = new BatchRequestDto();

        if (bindingContext.Result.Model is not BaseBatchRequestDto baseResult)
        {
            bindingContext.Result = ModelBindingResult.Success(result);

            return Task.CompletedTask;
        }

        result.FileIds = baseResult.FileIds;
        result.FolderIds = baseResult.FolderIds;

        if (bindingContext.GetBoolValue(nameof(result.DeleteAfter), out var deleteAfter))
        {
            result.DeleteAfter = deleteAfter;
        }

        if (bindingContext.GetFirstValue(nameof(result.ConflictResolveType), out var conflictResolveTypeValue) && FileConflictResolveTypeExtensions.TryParse(conflictResolveTypeValue, out var conflictResolveType))
        {
            result.ConflictResolveType = conflictResolveType;
        }

        if (bindingContext.GetFirstValue(nameof(result.DestFolderId), out var firstValue))
        {
            result.DestFolderId = ModelBindingContextExtension.ParseQueryParam(firstValue);
        }

        bindingContext.Result = ModelBindingResult.Success(result);

        return Task.CompletedTask;
    }
}

public class InsertFileModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        if (bindingContext is DefaultModelBindingContext defaultBindingContext &&
            bindingContext.ValueProvider is CompositeValueProvider { Count: 0 })
        {
            bindingContext.ValueProvider = defaultBindingContext.OriginalValueProvider;
        }

#pragma warning disable CA2000 // DTO ownership transferred to model binding
        var result = new InsertFileRequestDto();
#pragma warning restore CA2000

        if (bindingContext.GetBoolValue(nameof(result.CreateNewIfExist), out var createNewIfExist))
        {
            result.CreateNewIfExist = createNewIfExist;
        }

        if (bindingContext.GetBoolValue(nameof(result.KeepConvertStatus), out var keepConvertStatus))
        {
            result.KeepConvertStatus = keepConvertStatus;
        }

        if (bindingContext.GetFirstValue(nameof(result.Title), out var firstValue))
        {
            result.Title = firstValue;
        }

        if (bindingContext.HttpContext.Request.HasFormContentType)
        {
            result.File = bindingContext.HttpContext.Request.Form.Files.FirstOrDefault();
        }

        bindingContext.HttpContext.Request.EnableBuffering();

        bindingContext.HttpContext.Request.Body.Position = 0;

        result.Stream = new MemoryStream();
        await bindingContext.HttpContext.Request.Body.CopyToAsync(result.Stream);
        result.Stream.Position = 0;

        bindingContext.Result = ModelBindingResult.Success(result);
    }
}

public class UploadModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        if (bindingContext is DefaultModelBindingContext defaultBindingContext && bindingContext.ValueProvider is CompositeValueProvider { Count: 0 })
        {
            bindingContext.ValueProvider = defaultBindingContext.OriginalValueProvider;
        }

#pragma warning disable CA2000 // DTO ownership transferred to model binding
        var result = new UploadRequestDto();
#pragma warning restore CA2000

        if (bindingContext.GetBoolValue(nameof(result.CreateNewIfExist), out var createNewIfExist))
        {
            result.CreateNewIfExist = createNewIfExist;
        }

        if (bindingContext.GetBoolValue(nameof(result.KeepConvertStatus), out var keepConvertStatus))
        {
            result.KeepConvertStatus = keepConvertStatus;
        }

        if (bindingContext.GetBoolValue(nameof(result.StoreOriginalFileFlag), out var storeOriginalFileFlag))
        {
            result.StoreOriginalFileFlag = storeOriginalFileFlag;
        }

        if (bindingContext.GetFirstValue(nameof(result.ContentType), out var contentType) && !string.IsNullOrEmpty(contentType))
        {
            result.ContentType = new ContentType(contentType);
        }

        if (bindingContext.GetFirstValue(nameof(result.ContentDisposition), out var contentDisposition) && !string.IsNullOrEmpty(contentDisposition))
        {
            result.ContentDisposition = new ContentDisposition(contentDisposition);
        }

        if (bindingContext.HttpContext.Request.HasFormContentType)
        {
            result.File = bindingContext.HttpContext.Request.Form.Files.FirstOrDefault();
        }

        bindingContext.HttpContext.Request.EnableBuffering();

        bindingContext.HttpContext.Request.Body.Position = 0;

        result.Stream = new MemoryStream();
        await bindingContext.HttpContext.Request.Body.CopyToAsync(result.Stream);
        result.Stream.Position = 0;

        bindingContext.Result = ModelBindingResult.Success(result);
    }
}