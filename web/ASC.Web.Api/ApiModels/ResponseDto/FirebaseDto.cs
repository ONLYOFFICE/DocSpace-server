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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The Firebase parameters.
/// </summary>
public class FirebaseDto
{
    /// <summary>
    /// The Firebase API key.
    /// </summary>
    /// <example>AIzaSyDxK9L3j4H8mN2pQ5rS6tU7vW8xY9zA1bC</example>
    public required string ApiKey { get; set; }

    /// <summary>
    /// The Firebase authentication domain.
    /// </summary>
    /// <example>myapp-12345.firebaseapp.com</example>
    public required string AuthDomain { get; set; }

    /// <summary>
    /// The Firebase project ID.
    /// </summary>
    /// <example>myapp-12345</example>
    public required string ProjectId { get; set; }

    /// <summary>
    /// The Firebase storage bucket.
    /// </summary>
    /// <example>myapp-12345.appspot.com</example>
    public required string StorageBucket { get; set; }

    /// <summary>
    /// The Firebase messaging sender ID.
    /// </summary>
    /// <example>123456789012</example>
    public required string MessagingSenderId { get; set; }

    /// <summary>
    /// The Firebase application ID.
    /// </summary>
    /// <example>1:123456789012:web:a1b2c3d4e5f6g7h8</example>
    public required string AppId { get; set; }

    /// <summary>
    /// The Firebase measurement ID.
    /// </summary>
    /// <example>G-ABCD123456</example>
    public required string MeasurementId { get; set; }

    /// <summary>
    /// The Firebase database URL.
    /// </summary>
    /// <example>https://myapp-12345.firebaseio.com</example>
    public required string DatabaseURL { get; set; }
}