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

const request = require("../requestManager.js");
const check = require("./authService.js");
const portalManager = require("../portalManager.js");
const logger = require("../log.js");

module.exports = (socket, next) => {
  const req = socket.client.request;
  const session = socket.handshake.session;

  const cookie = req?.cookies?.authorization || req?.cookies?.asc_auth_key;
  const token = req?.headers?.authorization;
  const share = socket.handshake.query?.share;
  const fileId = socket.handshake.query?.openFileId;

  if (!cookie && !token && !share) {
    const err = new Error(
      "Authentication error (not token or cookie or share key)"
    );
    logger.error(err);
    socket.disconnect("unauthorized");
    next(err);
    return;
  }

  if (token) {
    if (!check(token)) {
      const err = new Error("Authentication error (token check)");
      logger.error(err);
      next(err);
    } else {
      session.system = true;
      session.save();
      next();
    }
    return;
  }

  const basePath = portalManager(req)?.replace(/\/$/g, "");
  let headers = {};

  const validateExternalLink = () => {
    return request({
      method: "get",
      url: `/files/share/${share}`,
      headers,
      basePath
    })
  }

  if (cookie) {
    headers.Authorization = cookie;

    const getUser = () => {
      return request({
        method: "get",
        url: "/people/@self?fields=id,userName,displayName",
        headers,
        basePath,
      });
    };

    const getPortal = () => {
      return request({
        method: "get",
        url: "/portal?fields=tenantId,tenantDomain",
        headers,
        basePath,
      });
    };

    const getConnection = () => {
      return request({
        method: "get",
        url: "/security/activeconnections/getthisconnection",
        headers,
        basePath,
      });
    };

    const getFile = () => {
      if(fileId){
        return request({
          method: "get",
          url: `/files/file/${fileId}`,
          headers,
          basePath,
        });
      }
    };

    const getRoomId = () => {
      if(fileId){
        return request({
          method: "get",
          url: `/files/file/${fileId}/room`,
          headers,
          basePath,
        });
      }
    };
    
    const validateLink = () => {
      if (!share) {
        return Promise.resolve({ status: 1 });
      }
      
      return validateExternalLink();
    }

    return Promise.all([getUser(), getPortal(), getConnection(), validateLink(), getFile(), getRoomId()])
      .then(([user, portal, connection, { status, linkId }, file, roomId = { }]) => {
        logger.info(`WS: save account info in sessionId='sess:${session.id}'`, { user, portal });
        session.user = user;
        session.portal = portal;
        session.user.connection = connection;
        if (status === 0){
          session.linkId = linkId;
        }
        if(file && roomId != -1)
        {
          session.file = file;
          session.file.roomId = roomId;
        }
        
        session.save(function (err){
          if(err) {
            logger.error(err);
            next(err);
          }else{
            next();
          }
        });
      })
      .catch((err) => {
        logger.error("Error of getting account info", err);
        socket.disconnect("Unauthorized");
        next(err);
      });
  }

  if (share) {
    if (req?.cookies) {
      const pairs = Object.entries(req.cookies).map(
        ([key, value]) => `${key}=${value}`
      );

      if (pairs.length > 0) {
        let cookie = pairs.join(";");
        cookie += ";";
        headers.Cookie = cookie;
      }
    }

    return validateExternalLink()
      .then(({ status, tenantId, linkId } = { }) => {
        if (status !== 0) {
          const err = new Error("Invalid share key");
          logger.error("WS: share key validation failure:", err);
          return next(err);
        }

        logger.info(`WS: share key validation successful: key='${share}' sessionId='sess:${session.id}'`);
        session.anonymous = true;
        session.portal = { tenantId };
        session.user = { id: linkId }
        session.save();
        next();
      })
      .catch((err) => {
        logger.error(err);
        socket.disconnect("Unauthorized");
        next(err);
      });
  }
};
