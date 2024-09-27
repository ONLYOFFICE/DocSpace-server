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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// </summary>
public class WatermarkDto
{
    /// <summary>Specifies whether to display: username, user email, user ip-adress, current date and room name </summary>
    /// <type>WatermarkAdditions</type>
    public WatermarkAdditions Additions { get; set; }

    /// <summary>Watermark Text</summary>
    /// <type>System.String, System</type>
    public string Text { get; set; }

    /// <summary>Watermark text and image rotate</summary>
    /// <type>System.Int32, System</type>
    public int Rotate { get; set; }

    /// <summary>Watermark image scale</summary>
    /// <type>System.Int32, System</type>
    public int ImageScale { get; set; }

    /// <summary>Watermark image url</summary>
    /// <type>System.String, System</type>
    public string ImageUrl { get; set; }

    /// <summary>Watermark image height</summary>
    /// <type>System.Double, System</type>
    public double ImageHeight { get; set; }

    /// <summary>Watermark image width</summary>
    /// <type>System.Double, System</type>
    public double ImageWidth { get; set; }
}
[Scope]
public class WatermarkDtoHelper
{
    public WatermarkDto Get(WatermarkSettings watermarkSettings)
    {
        if (watermarkSettings == null)
        {
            return null;
        }

        return new WatermarkDto
        {
            Additions = watermarkSettings.Additions,
            Text = watermarkSettings.Text,
            Rotate = watermarkSettings.Rotate,
            ImageScale = watermarkSettings.ImageScale,
            ImageUrl = watermarkSettings.ImageUrl,
            ImageHeight = watermarkSettings.ImageHeight,
            ImageWidth = watermarkSettings.ImageWidth
        };
    }
}
