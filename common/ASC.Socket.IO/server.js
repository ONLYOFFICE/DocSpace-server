// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
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

const express = require("express");
const { Server } = require("socket.io");
const { createAdapter } = require('@socket.io/redis-adapter');
const { createServer } = require("http");
const logger = require("morgan");
const redis = require("redis");
const expressSession = require("express-session");
const cookieParser = require("cookie-parser");
const RedisStore = require("connect-redis").default;
const MemoryStore = require("memorystore")(expressSession);
const sharedsession = require("express-socket.io-session");
const process = require('process');

const config = require("./config");
const auth = require("./app/middleware/auth.js");
const winston = require("./app/log.js");

(async () => {
winston.stream = {
  write: (message) => winston.info(message),
};

const port = config.get("app").port || 9899;
const app = express();

const secret = config.get("core").machinekey + new Date().getTime();
const secretCookieParser = cookieParser(secret);
const baseCookieParser = cookieParser();

const redisEnabled = process.env.REDIS_ENABLED !== "false";
const redisOptions = redisEnabled ? config.get("Redis") : null;

let store;
let redisClient;
if (redisOptions != null) {
  redisClient = redis.createClient(redisOptions);
  redisClient.on('error', err => winston.error('Redis Client Error', err));
  await redisClient.connect();
  winston.info('Redis connect');
  store = new RedisStore({ client: redisClient });
} else {
  store = new MemoryStore();
}

const session = expressSession({
  store: store,
  secret: secret,
  resave: false,
  saveUninitialized: true,
  cookie: {
    path: "/",
    httpOnly: true,
    secure: false,
    maxAge: null,
  },
  cookieParser: secretCookieParser,
  name: "socketio.sid",
});

app.use(logger("tiny", { 
  stream: winston.stream,
  skip: function (req, res) { 
    return req.url.endsWith("/health"); 
  }
}));
app.use(session);

const httpServer = createServer(app);

const options = {
  cors: {
    //origin: "http://localhost:8092",
    methods: ["GET", "POST"],
    allowedHeaders: ["Authorization"],
    //credentials: true,
  },
  allowRequest: (req, cb) => {
    baseCookieParser(req, null, () => {});
    const token =
      req?.headers?.authorization ||
      req?.cookies?.authorization ||
      req?.cookies?.asc_auth_key ||
      req?._query?.share;

    if (!token) {
      winston.info(`not allowed request: empty token`);
      return cb("auth", false);
    }
    return cb("auth", true);
  },
};

const io = new Server(httpServer, options);

io.use(sharedsession(session, secretCookieParser, { autoSave: true }))
  .use((socket, next) => {
    baseCookieParser(socket.client.request, null, next);
  })
  .use((socket, next) => {
    auth(socket, next);
  });

if (redisClient != null) 
{
  const pubClient = redisClient;
  const subClient = pubClient.duplicate();
  subClient.on('error',  err => winston.error('Redis Client Error', err));
  subClient.connect();
  
  io.adapter(createAdapter(pubClient, subClient));
}


app.get("/", (req, res) => {
  res.send("<h1>Invalid Endpoint</h1>");
});

const filesHub = require("./app/hubs/files.js")(io);

app.use("/controller", require("./app/controllers")(filesHub));
app.use("/", require("./app/controllers/healthCheck.js") ());

httpServer.listen(port, () => winston.info(`Server started on port: ${port}`));

process.on('unhandledRejection', (reason, p) => {
  winston.error('Unhandled rejection at:', p, 'reason:', reason);
});

process.on('uncaughtException', (error) => {
  winston.error(`Unhandled exception: ${error}\n` + `Exception origin: ${error.stack}`);
});

module.exports = io;
})();