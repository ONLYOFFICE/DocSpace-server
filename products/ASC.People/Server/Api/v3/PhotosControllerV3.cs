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
// the GNU AGPL at: http://creativecommons.org/licenses/agpl-3.0.html
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

namespace ASC.People.Api.V3;

using ASC.People.ApiModels.V3.ResponseDto.Common;
using ASC.People.ApiModels.RequestDto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

/// <summary>
/// RESTful API v3 for managing user photos in the DocSpace system.
/// </summary>
/// <remarks>
/// This controller provides operations for user photo management,
/// following REST best practices and RFC 7231 standards.
///
/// Photos Overview:
/// User photos are profile images that can be uploaded, updated, and deleted.
/// The system supports:
/// - Multiple thumbnail sizes (original, retina, max, big, medium, small)
/// - Custom thumbnail coordinates
/// - Photo upload with size validation
/// - Photo deletion
///
/// Business Rules:
/// - Only the photo owner can upload/update/delete their own photo
/// - System users cannot modify photos
/// - Maximum upload size is enforced
/// - Supported formats: JPG, PNG, GIF
/// - Photos are automatically resized to multiple thumbnails
/// </remarks>
[ApiController]
[Route("api/3.0/users/{id:guid}/photo")]
[Tags("Photos (v3)")]
[Produces("application/json")]
public class PhotosControllerV3 : ApiControllerBaseV3
{
    private readonly PhotoController _photoController;
    private readonly ILogger<PhotosControllerV3> _logger;

    public PhotosControllerV3(
        PhotoController photoController,
        ILogger<PhotosControllerV3> logger)
    {
        _photoController = photoController;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a user's photo with all thumbnail URLs.
    /// </summary>
    /// <remarks>
    /// Returns URLs for all available photo sizes.
    ///
    /// Available Sizes:
    /// - Original: Full resolution uploaded image
    /// - Retina: High-DPI display version
    /// - Max: Maximum size thumbnail
    /// - Big: Large thumbnail
    /// - Medium: Medium thumbnail
    /// - Small: Small thumbnail
    ///
    /// Permissions:
    /// Any authenticated user can view photos.
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <returns>Photo information with thumbnail URLs</returns>
    /// <response code="200">Photo data retrieved successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(ThumbnailsDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhoto([FromRoute] Guid id)
    {
        try
        {
            var v2Request = new GetUserPhotoRequestDto { UserId = id.ToString() };
            var result = await _photoController.GetMemberPhoto(v2Request);

            return Ok(result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ItemNotFoundException)
        {
            return Error("NotFound", $"User with ID {id} not found", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo for user {UserId}", id);
            return Error("InternalError", "An error occurred while retrieving the photo", 500);
        }
    }

    /// <summary>
    /// Uploads a new photo for a user.
    /// </summary>
    /// <remarks>
    /// Uploads a new profile photo for the specified user.
    ///
    /// Side Effects:
    /// - Photo is stored in the system
    /// - Multiple thumbnails are automatically generated
    /// - Previous photo is replaced
    /// - Webhook UserUpdated event is fired
    /// - Audit log entry is created
    ///
    /// Business Rules:
    /// - Only the photo owner can upload
    /// - Maximum file size is enforced (typically 10 MB)
    /// - Supported formats: JPG, PNG, GIF
    /// - Invalid formats return 415 Unsupported Media Type
    /// - Oversized files return 413 Payload Too Large
    ///
    /// Form Data:
    /// - File: The image file to upload
    /// - Autosave: Boolean indicating whether to auto-save thumbnails
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <param name="formCollection">Form data with file upload</param>
    /// <returns>Upload result with success status and message</returns>
    /// <response code="200">Photo uploaded successfully</response>
    /// <response code="400">Invalid file or request data</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    /// <response code="413">File size exceeds maximum allowed</response>
    /// <response code="415">Unsupported image format</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FileUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> UploadPhoto(
        [FromRoute] Guid id,
        [FromForm] IFormCollection formCollection)
    {
        try
        {
            var v2Request = new UploadMemberPhotoRequestDto
            {
                UserId = id.ToString(),
                FormCollection = formCollection
            };

            var result = await _photoController.UploadMemberPhoto(v2Request);

            if (!result.Success)
            {
                return Error("UploadFailed", result.Message, 400);
            }

            return Ok(result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ItemNotFoundException)
        {
            return Error("NotFound", $"User with ID {id} not found", 404);
        }
        catch (Exception ex) when (ex.Message.Contains("size"))
        {
            return Error("PayloadTooLarge", "Image size exceeds maximum allowed", 413);
        }
        catch (Exception ex) when (ex.Message.Contains("format"))
        {
            return Error("UnsupportedMediaType", "Unknown or unsupported image format", 415);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo for user {UserId}", id);
            return Error("InternalError", "An error occurred while uploading the photo", 500);
        }
    }

    /// <summary>
    /// Updates a user's photo from a URL.
    /// </summary>
    /// <remarks>
    /// Updates the user's photo by downloading from a URL.
    ///
    /// Side Effects:
    /// - Photo is downloaded and stored
    /// - Multiple thumbnails are generated
    /// - Previous photo is replaced
    /// - Webhook UserUpdated event is fired
    /// - Audit log entry is created
    ///
    /// Permissions:
    /// Only the photo owner can update their photo.
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <param name="request">Update request with photo URL</param>
    /// <returns>Updated photo information</returns>
    /// <response code="200">Photo updated successfully</response>
    /// <response code="400">Invalid URL or request data</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpPut]
    [ProducesResponseType(typeof(ThumbnailsDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePhoto(
        [FromRoute] Guid id,
        [FromBody] UpdatePhotoMemberRequestDto request)
    {
        try
        {
            request.UserId = id.ToString();
            var result = await _photoController.UpdateMemberPhoto(request);

            return Ok(result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ItemNotFoundException)
        {
            return Error("NotFound", $"User with ID {id} not found", 404);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating photo for user {UserId}", id);
            return Error("InternalError", "An error occurred while updating the photo", 500);
        }
    }

    /// <summary>
    /// Deletes a user's photo.
    /// </summary>
    /// <remarks>
    /// Permanently removes the user's profile photo.
    ///
    /// Side Effects:
    /// - Photo and all thumbnails are deleted
    /// - User reverts to default avatar
    /// - Webhook UserUpdated event is fired
    /// - Audit log entry is created
    ///
    /// Permissions:
    /// Only the photo owner can delete their photo.
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <returns>Updated photo information (default avatar)</returns>
    /// <response code="200">Photo deleted successfully</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpDelete]
    [ProducesResponseType(typeof(ThumbnailsDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto([FromRoute] Guid id)
    {
        try
        {
            var v2Request = new GetUserPhotoRequestDto { UserId = id.ToString() };
            var result = await _photoController.DeleteMemberPhoto(v2Request);

            return Ok(result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ItemNotFoundException)
        {
            return Error("NotFound", $"User with ID {id} not found", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo for user {UserId}", id);
            return Error("InternalError", "An error occurred while deleting the photo", 500);
        }
    }

    /// <summary>
    /// Creates custom photo thumbnails using specified coordinates.
    /// </summary>
    /// <remarks>
    /// Allows cropping the photo to specific coordinates and dimensions.
    ///
    /// Side Effects:
    /// - Custom thumbnails are generated
    /// - Previous thumbnails are replaced
    /// - Webhook UserUpdated event is fired
    /// - Audit log entry is created
    ///
    /// Coordinates:
    /// - X, Y: Top-left corner of crop area
    /// - Width, Height: Dimensions of crop area
    /// - If Width/Height are 0, uses full image dimensions
    ///
    /// Permissions:
    /// Only the photo owner can create thumbnails.
    /// </remarks>
    /// <param name="id">User ID</param>
    /// <param name="request">Thumbnail coordinates and dimensions</param>
    /// <returns>Thumbnail information with new URLs</returns>
    /// <response code="200">Thumbnails created successfully</response>
    /// <response code="400">Invalid coordinates or dimensions</response>
    /// <response code="403">Insufficient permissions</response>
    /// <response code="404">User not found</response>
    [HttpPost("thumbnails")]
    [ProducesResponseType(typeof(ThumbnailsDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDtoV3), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateThumbnails(
        [FromRoute] Guid id,
        [FromBody] ThumbnailsRequestDto request)
    {
        try
        {
            request.UserId = id.ToString();
            var result = await _photoController.CreateMemberPhotoThumbnails(request);

            return Ok(result);
        }
        catch (SecurityException ex)
        {
            return Error("Forbidden", ex.Message, 403);
        }
        catch (ItemNotFoundException)
        {
            return Error("NotFound", $"User with ID {id} not found", 404);
        }
        catch (ArgumentException ex)
        {
            return Error("ValidationFailed", ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating thumbnails for user {UserId}", id);
            return Error("InternalError", "An error occurred while creating thumbnails", 500);
        }
    }
}
