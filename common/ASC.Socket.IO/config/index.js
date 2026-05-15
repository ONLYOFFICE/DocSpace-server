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

const nconf = require('nconf');
const path = require('path');
const fs = require("fs");

nconf.argv()
    .env()
    .file("config", path.join(__dirname, 'config.json'));

getAndSaveAppsettings();

module.exports = nconf;

function getAndSaveAppsettings(){
    var appsettings = nconf.get("app").appsettings;
    if(!path.isAbsolute(appsettings)){
        appsettings = path.join(__dirname, appsettings);
    }

    var env = nconf.get("app").environment;

    console.log('environment: ' + env);
    nconf.file("appsettingsWithEnv", path.join(appsettings, 'appsettings.' + env + '.json'));
    nconf.file("appsettings", path.join(appsettings, 'appsettings.json'));

    nconf.file("appsettingsServices", path.join(appsettings, 'appsettings.services.json'));

    nconf.file("redisWithEnv", path.join(appsettings, 'redis.' + env + '.json'));
    nconf.file("redis", path.join(appsettings, 'redis.json'));
    
    var redisEnabled = nconf.get("REDIS_ENABLED");
    var redis = nconf.get("Redis");

    if(redisEnabled !== "false" && redis != null && redis.Hosts && redis.Hosts[0] && redis.Hosts[0].Host)
    {
        redis.connect_timeout = redis.ConnectTimeout;
        redis.database = redis.Database;
        redis.username = redis.User;
        redis.password = redis.Password;
        redis.socket = {
            host: redis.Hosts[0].Host,
            port: redis.Hosts[0].Port
        };

        nconf.set("Redis", redis);
    }
    else
    {
        nconf.set("Redis", null);
    }
}