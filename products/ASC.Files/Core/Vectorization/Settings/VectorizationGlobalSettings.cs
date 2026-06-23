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

namespace ASC.Files.Core.Vectorization.Settings;

[Singleton]
public class VectorizationGlobalSettings
{
    public const string OpenAiBaseUrl = "https://api.openai.com/v1";
    public const string OpenRouterBaseUrl = "https://openrouter.ai/api/v1";
    public int ChunkSize { get; init; }
    public float ChunkOverlap { get; init; }
    public int ChunksBatchSize { get; init; }
    public long MaxContentLength { get; init; }
    public HashSet<string> SupportedFormats { get; init; }
    public EmbeddingModel Model { get; init; }
    
    public VectorizationGlobalSettings(IConfiguration configuration)
    {
        var settings = configuration.GetSection("ai:vectorization").Get<Settings>();
        ChunkSize = settings.ChunkSize;
        ChunkOverlap = settings.ChunkOverlap;
        ChunksBatchSize = settings.ChunksBatchSize;
        MaxContentLength = settings.MaxContentLengthBytes;
        SupportedFormats = settings.SupportedFormats;
        Model = new EmbeddingModel
        {
            Id = settings.ModelId,
            Dimension = settings.Dimension
        };
    }

    public bool IsSupportedContentExtraction(string fileTitle)
    {
        var ext = FileUtility.GetFileExtension(fileTitle);
        return SupportedFormats.Contains(ext);
    }
    
    private class Settings
    {
        public int ChunkSize { get; init; }
        public float ChunkOverlap { get; init; }
        public int ChunksBatchSize { get; init; }
        public long MaxContentLengthBytes { get; init; }
        public HashSet<string> SupportedFormats { get; init; }
        public string ModelId { get; init; }
        public int Dimension { get; init; }
    }
}