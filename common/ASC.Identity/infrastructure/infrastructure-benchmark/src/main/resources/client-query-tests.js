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
