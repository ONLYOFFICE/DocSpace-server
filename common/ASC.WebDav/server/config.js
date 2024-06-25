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

/* Config */

module.exports = {
  // Port listener WebDav Server
  port: 1900,  
  appsettings: "../../../../buildtools/config",
  environment: "Development",
  // Path to pfx key
  pfxKeyPath: null,
  // Pass phrase for pfx key
  pfxPassPhrase: null,
  // Path to .crt
  certPath: null,
  // Path to .key
  keyPath: null,
  // Enable secure connection
  isHttps: false,
  // root virtual directory
  virtualPath: "webdav",
  // Logging level
  logLevel: "debug",
  // Maximum execution time of long-running operations
  maxExecutionTime: 600000,
  // User cache storage time (msec)
  userLifeTime : 3600000,
  // Cleanup interval of expired users (msec)
  usersCleanupInterval: 600000,
  // Port of community server OnlyOffice */
  onlyOfficePort: ":80",
  // Maximum chunk size
  maxChunkSize: 10485760,
  // Api constant
  api: "/api/2.0",
  // Api authentication method
  apiAuth: "authentication.json",
  // Sub-method for files/folders operations
  apiFiles: "/files",
  // Path to read the file
  fileHandlerPath: "/Products/Files/HttpHandlers/filehandler.ashx?action=stream&fileid={0}",

  method: {
    // Get root directory in "Root"
    pathRootDirectory: "@root",
    // Operations with folders
    folder: "/folder",
    // Operations with files
    file: "/file",
    // Create new file "*.txt"
    text: "/text",
    // Create new file "*.html"
    html: "/html",
    // Get all active operations
    fileops: "/fileops",
    // File saving method
    saveediting: "/saveediting",
    // Method copy for files or folders
    copy: "/copy",
    // Method move for files or folders
    move: "/move",
    // Method for getting a link to download a file
    presigneduri: "/presigneduri",
    // Submethod to create a session
    upload: "/upload",
    // Method for creating a session
    createSession: "/create_session",
    // Method to create session to edit existing file with multiple chunks
    editSession: "/edit_session"
  }
};
