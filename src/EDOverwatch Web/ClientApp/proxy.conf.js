const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:47059';

const PROXY_CONFIG = [
  {
    context: [
      "/api",
	  "/discovery",
   ],
    target: target,
    secure: false,
    headers: {
      Connection: 'Keep-Alive'
    },
    logLevel: 'debug',
  },
  {
    context: [
      "/ws",
   ],
    target: target,
    secure: false,
    ws: true,
    logLevel: 'debug',
  },
]

module.exports = PROXY_CONFIG;
