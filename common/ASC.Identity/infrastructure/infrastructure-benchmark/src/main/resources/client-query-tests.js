// (c) Copyright Ascensio System SIA 2009-2025
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

import http from 'k6/http';
import {check, group, sleep} from 'k6';
import {Rate} from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL
const AUTH_COOKIE = __ENV.AUTH_COOKIE;
const PAGE = __ENV.PAGE || 0;
const LIMIT = __ENV.LIMIT || 10;

function extractHostFromUrl(url) {
    const urlParts = url.match(/^https?:\/\/([^/]+)(.*)/);
    return urlParts ? urlParts[1] : '';
}

const HEADERS = {
    headers: {
        'Content-Type': 'application/json',
        'Cookie': `asc_auth_key=${AUTH_COOKIE}`,
        'X-Forwarded-Host': extractHostFromUrl(BASE_URL),
    },
};

export let errorRate = new Rate('errors');

export const options = {
    stages: [
        { duration: '1m', target: 25 },
        { duration: '2m', target: 25 },
        { duration: '1m', target: 75 },
        { duration: '2m', target: 75 },
        { duration: '1m', target: 150 },
        { duration: '3m', target: 150 },
        { duration: '1m', target: 0 },
    ],
};

export default function () {
    group('Client Query Controller Tests', function () {
        group('GET /clients', function () {
            let res = http.get(`${BASE_URL}/api/2.0/clients?page=${PAGE}&limit=${LIMIT}`, HEADERS);
            check(res, {
                'is status 200': (r) => r.status === 200,
                'response body is not empty': (r) => r.body && r.body.length > 0,
            }) || errorRate.add(1);
            if (res.status !== 200) {
                console.error(`Failed to fetch clients: ${res.status} ${res.body}`);
            }
            sleep(1);
        });

        group('GET /clients/info', function () {
            let res = http.get(`${BASE_URL}/api/2.0/clients/info?page=${PAGE}&limit=${LIMIT}`, HEADERS);
            check(res, {
                'is status 200': (r) => r.status === 200,
                'response body is not empty': (r) => r.body && r.body.length > 0,
            }) || errorRate.add(1);
            if (res.status !== 200) {
                console.error(`Failed to fetch clients info: ${res.status} ${res.body}`);
            }
            sleep(1);
        });
    });
}
