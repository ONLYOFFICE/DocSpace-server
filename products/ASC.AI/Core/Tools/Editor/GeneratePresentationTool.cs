// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Core.Tools.Editor;

[Scope]
public class GeneratePresentationTool(FileStorageService fileService, EditorToolCallStateStore callStateStore)
{
    public const string Name = "docspace_generate_presentation";
    private const string Description = "Generates a complete presentation with custom theme, fonts, and streaming content.";
    private static AIFunctionFactoryOptions FactoryOptions => new()
    {
        Name = Name,
        Description = Description
    };

    public AIFunction Init(int resultStorageId)
    {
        return AIFunctionFactory.Create(Function, FactoryOptions);

        async Task<ToolResponse<GeneratedFileResult>> Function(
            [Description("File name without extension. Use normal spaces, no underscores or special characters")] string fileName,
            [Description("Presentation topic")] string? topic,
            [Description("Number of slides")] string? slideCount,
            [Description("Visual style - modern, classic, minimal, corporate")] string? style)
        {
            try
            {
                var file = await fileService.CreateNewFileAsync(new FileModel<int, int>
                {
                    ParentId = resultStorageId,
                    Title = $"{fileName}.pptx"
                });

                await callStateStore.SetAsync(file.Id, new GeneratePresentationToolCallState
                {
                    ToolName = "generatePresentationWithTheme",
                    Topic = topic,
                    SlideCount = slideCount,
                    Style = style
                });

                return new ToolResponse<GeneratedFileResult>
                {
                    Data = new GeneratedFileResult
                    {
                        Id = file.Id,
                        Title = file.Title
                    }
                };
            }
            catch (Exception e)
            {
                return new ToolResponse<GeneratedFileResult> { Error = e.Message };
            }
        }
    }
}
