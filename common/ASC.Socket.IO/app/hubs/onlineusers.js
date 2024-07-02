var uap = require('ua-parser-js');
const { json } = require("express/lib/response");

module.exports = async (io) => {
    const logger = require("../log.js");
    const onlineIO = io;
    const portalUsers =[];
    const roomUsers =[];
    const mapperIds = [];

    onlineIO.on("connection", async (socket) => {
      if (socket.handshake.session.system) {
        return;
      }
      const ipAddress = getCleanIP(socket.handshake.headers['x-forwarded-for']);
      const parser = uap(socket.request.headers['user-agent']);
      const operationSystem = parser.os.version !== undefined ?  `${parser.os.name} ${parser.os.version}` : `${parser.os.name}`;  
      const browserVersion = parser.browser.version ? parser.browser.version : '';
  
      const userId = socket.handshake.session?.user?.id;
      const userName = (socket.handshake.session?.user?.userName || "").toLowerCase();
      const tenantId = socket.handshake.session?.portal?.tenantId;
      var _roomId;
      let id;
      let sessionId = socket.handshake.session?.user?.connection;
      let idInRoom;
      
      var user = getUser(portalUsers, userId, tenantId);
      var session;
      if (!user) 
      {
        var sessions = new Map();
        sessions.set(
          1,
          {
            id: sessionId,
            platform: operationSystem,
            browser: parser.browser.name + " " + browserVersion,
            ip: ipAddress,
            status: "online"
          });
        id = 1;
        user = {
          id: userId,
          displayName: userName,
          page: session?.user?.profileUrl,
          sessions: sessions,
          status: "online",
          offlineSessions: new Map()
        };
      }
      else
      {
        user.status = "online";

        var keys = Array.from(user.sessions.keys());
        id = keys.length == 0 ? 1 : keys[keys.length - 1] + 1;
        session = {
          id: sessionId,
          platform: operationSystem,
          browser: parser.browser.name + " " + browserVersion,
          ip: ipAddress
        };
        user.sessions.set(
          id,
          session
        );
      }
      
      user.offlineSessions.delete(sessionId);
      if(user.sessions.size == 1)
      {
        var stringUser = serialize(user);
        onlineIO.to(`p-${tenantId}`).emit("enter-in-portal", stringUser );
      }
      else
      {
        onlineIO.to(`p-${tenantId}`).emit("enter-session-in-portal", {userId, session} );
      }

      updateUser(portalUsers, user, userId, tenantId);

      socket.on("disconnect", async (reason) => {
        var user = getUser(portalUsers, userId, tenantId);
        if (user) 
        {
          var session = user.sessions.get(id);
          user.sessions.delete(id);
          var array = Array.from(user.sessions, ([name, value]) => {
            return value;
          })
          if(!array.find(e=> e.id == session.id))
          {
            user.offlineSessions.set(session.id,
              {
                id: session.id,
                platform: session.platform,
                browser: session.browser,
                ip: session.ip,
                status: "offline",
                date: new Date().toString()
            });

            var date = new Date().toString();
            if(user.sessions.size <= 0)
            {
              user.status = "offline";
              updateUser(portalUsers, user, userId, tenantId);
              onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
            }
            else
            {
              updateUser(portalUsers, user, userId, tenantId);
              onlineIO.to(`p-${tenantId}`).emit("leave-session-in-portal",  {userId, sessionId, date} );
            }
          }
          id = -1;
          sessionId = -1;
        }

        if(!_roomId)
        {
          return;
        }

        /*****************************/

        var user = getUser(roomUsers, userId, _roomId);
        if (user) 
        {
          user.sessions.delete(idInRoom);
          idInRoom = -1;

          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            updateUser(roomUsers, user, userId, _roomId);
            onlineIO.to(_roomId).emit("leave-in-room",  userId );
          }
          else
          { 
            updateUser(roomUsers, user, userId, _roomId);
          }
        }
      });
  
      socket.on("enterInRoom", async ({ roomPart, status }) => {
        const roomId = getRoom(roomPart);
        var user = getUser(roomUsers, userId, roomId);
        if (!user) 
        {
          var sessions = new Map();
          sessions.set(
            1,
            {
              id: sessionId,
              platform: operationSystem,
              browser: parser.browser.name + " " + browserVersion,
              ip: ipAddress,
              status: status
            });
          idInRoom = 1;
          user = {
            id: userId,
            displayName: userName,
            page: session?.user?.profileUrl,
            sessions: sessions,
            status: "online"
          };
        }
        else
        {
          user.status = "online";
          var keys = user.sessions.keys();
          idInRoom = keys.length == 0 ? 1 : Array.from(keys).pop() + 1;
          sessions.set(
            idInRoom,
            {
              id: sessionId,
              platform: operationSystem,
              browser: parser.browser.name + " " + browserVersion,
              ip: ipAddress,
              status: status
            });
        }
        _roomId = roomId
        if(user.sessions.size == 1)
        {
          var stringUser = serialize(user);
          onlineIO.to(roomId).emit("enter-in-room", stringUser );
        }
        updateUser(roomUsers, user, userId, roomId);
      });

      socket.on("changeStatus", async ({ roomPart, status }) => {
        const roomId = getRoom(roomPart);
        var user = getUser(roomUsers, userId, roomId);
        if (!user) 
        {
          return;
        }
        
        user.sessions[idInRoom].status = status;
        updateUser(roomUsers, user, userId, roomId);
        var stringUser = serialize(user);
        onlineIO.to(roomId).emit("user-status",  stringUser );
      });

      socket.on("removeStatus", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = getUser(roomUsers, userId, roomId);
        if (!user) 
        {
          return;
        }

        user.sessions[idInRoom].status = null;
        updateUser(roomUsers, user, userId, roomId);
        var stringUser = serialize(user);
        onlineIO.to(roomId).emit("user-status",  stringUser );
      });

      socket.on("getSessionsInRoom", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var users = [];
        Object.values(roomUsers[roomId]).forEach(function(entry) {
          users.push(serialize(entry));
        });
        onlineIO.to(socket.id).emit("statuses-in-room",  users );
      });

      socket.on("getSessionsInPortal", async () => {
        var users = [];
        Object.values(portalUsers[tenantId]).forEach(function(entry) {
          users.push(serialize(entry));
        });
        onlineIO.to(socket.id).emit("statuses-in-portal",  users );
      });

      socket.on("leaveRoom", async ({ roomPart }) => {
        const roomId = getRoom(roomPart);
        var user = getUser(roomUsers, userId, roomId);
        if (user) 
        {
          user.sessions.delete(idInRoom);
          idInRoom = -1;

          if(user.sessions.length <= 0)
          {
            updateUser(roomUsers, user, userId, roomId);
            _roomId = undefined;
            onlineIO.to(roomId).emit("leave-in-room", userId );
          }
          else
          { 
            updateUser(roomUsers, user, userId, roomId);
            _roomId = undefined;
          }
        }
      });

      socket.on("subscribeToRoom", ({ roomPart }) => {
        if (!roomPart) return;
  
        const room = getRoom(roomPart);
        logger.info(`client ${socket.id} subscribe room ${room}`);
        socket.join(room);
      });
  
      socket.on("unsubscribeToRoom", ({ roomPart }) => {
        if (!roomPart) return;
  
        const room = getRoom(roomPart);
        logger.info(`client ${socket.id} unsubscribe room ${room}`);
        socket.leave(room);
      });

      socket.on("subscribeToPortal", () => {
        logger.info(`client ${socket.id} subscribe portal ${tenantId}`);
        socket.join(`p-${tenantId}`);
      });
  
      socket.on("unsubscribeToPortal", () => {
        logger.info(`client ${socket.id} unsubscribe room ${tenantId}`);
        socket.leave(`p-${tenantId}`);
      });

      getRoom = (roomPart) => {
        return `${tenantId}-${roomPart}`;
      };

      function getCleanIP (ipAddress) {
        if(typeof(ipAddress) == "undefined"){
          return "127.0.0.1";
        }
              const indexOfColon = ipAddress.indexOf(':');
              if (indexOfColon === -1){
                  return ipAddress;
              } else if (indexOfColon > 3){
                  return ipAddress.substring(0, indexOfColon);
              }
              else {
                  return "127.0.0.1";
              }
      }

      function serialize(user)
      {
        var serUser = {
          id: user.id,
          displayName: user.displayName,
          page: user.page,
          sessions: Array.from(user.sessions, ([name, value]) => {
            return value;
          }),
          status: user.status
        };
        serUser.sessions = serUser.sessions.concat(Array.from(user.offlineSessions, ([name, value]) => {
          return value;
        }));
        return serUser;
      }
    });

    function leaveSessionInPortal({id, userId, tenantId} = {}) {

      var user = getUser(portalUsers, userId, tenantId);
        if (user) 
        {
          var array = Array.from(user.sessions, ([name, value]) => {
            value.innerId = name;
            return value;
          });
          var sessions = array.filter(e=> e.id == id);

          Object.values(sessions).forEach(function(entry) {
            user.sessions.delete(entry.innerId);
          });

          user.offlineSessions.delete(id);
          var date = new Date().toString();
          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            updateUser(portalUsers, user, userId, tenantId);
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
          }
          else
          {
            updateUser(portalUsers, user, userId, tenantId);
            var session = array.find(e=> e.id == id);
            if(session)
            {
              var sessionId = session.Id;
              onlineIO.to(`p-${tenantId}`).emit("leave-session-in-portal",  {userId, sessionId, date} );
            }
          }
        }
    }

    function leaveInPortal({userId, tenantId} = {}) {

      var user = getUser(portalUsers, userId, tenantId);
        if (user) 
        {
          user.offlineSessions = new Map();
          user.sessions = new Map();

          var date = new Date().toString();
          user.status = "offline";
          updateUser(portalUsers, user, userId, tenantId);
          onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
        }
    }

    function leaveExceptThisInPortal({id, userId, tenantId} = {}) {

      var user = getUser(portalUsers, userId, tenantId);
        if (user) 
        {
          var array = Array.from(user.sessions, ([name, value]) => {
            value.innerId = name;
            return value;
          });
          var sessions = array.filter(e=> e.id != id);

          var setIds= new Set();
          Object.values(sessions).forEach(function(entry) {
            user.sessions.delete(entry.innerId);
            setIds.add(entry.id);
          });

          array = Array.from(user.offlineSessions, ([name, value]) => {
            return value;
          });
          Object.values(array.filter(e=> e.id != id)).forEach(function(entry) {
            user.offlineSessions.delete(entry.id);
          });
          var date = new Date().toString();
          if(user.sessions.size <= 0)
          {
            user.status = "offline";
            updateUser(portalUsers, user, userId, tenantId);
            onlineIO.to(`p-${tenantId}`).emit("leave-in-portal",  {userId, date} );
          }
          else
          {
            updateUser(portalUsers, user, userId, tenantId);
            Object.values(setIds).forEach(function(entry) {
              onlineIO.to(`p-${tenantId}`).emit("leave-session-in-portal",  {userId, entry, date} );
            });
          }
        }
    }

    function getUser(list, userId, id){
      if(!list[id])
      {
        list[id] = [];
        return null;
      }
      return list[id][userId];
    }

    function updateUser(list, user, userId, id){
      if(!list[id])
      {
        list[id] = [];
      }
      list[id][userId] = user;
    }

    return {
      leaveSessionInPortal,
      leaveInPortal,
      leaveExceptThisInPortal
    };
};
  