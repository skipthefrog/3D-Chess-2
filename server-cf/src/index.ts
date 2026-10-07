import { cleanName, issueToken, verifyToken } from './auth.js';
import type { GameConfig } from './types/game.js';

export { Room } from './room.js';
export { Matchmaker } from './matchmaker.js';

// ─────────────────────────────────────────────────────────────────────────────
// 3D Chess game server (Cloudflare Worker)
//
//   GET  /health                        → { ok: true }
//   POST /auth/guest   { name }         → { token, userId, name }
//   POST /rooms        { config, isPublic? }   (Authorization: Bearer <token>)
//                                       → { code, config }
//   GET  /rooms/:code                   → room summary (players, open seats)
//   GET  /rooms/:code/connect?token=…   → WebSocket into the room
//   GET  /matchmaking/connect?token=…   → WebSocket into the Find Match queue
//
// The token is passed as a query parameter for WebSockets because neither
// browsers nor Unity's WebGL WebSocket can set headers on the upgrade request.
// ─────────────────────────────────────────────────────────────────────────────

export interface Env {
  ROOM: DurableObjectNamespace;
  MATCHMAKER: DurableObjectNamespace;
  TOKEN_SECRET: string;
}

const CODE_CHARS = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // no 0/O or 1/I

function newCode(): string {
  const bytes = crypto.getRandomValues(new Uint8Array(6));
  return Array.from(bytes, b => CODE_CHARS[b % CODE_CHARS.length]).join('');
}

/** Create a room with a fresh code (retrying on the rare collision) */
export async function createRoom(env: Env, hostUserId: string, config: Partial<GameConfig>, isPublic: boolean): Promise<{ code: string; config: GameConfig }> {
  for (let attempt = 0; attempt < 5; attempt++) {
    const code = newCode();
    const stub = env.ROOM.get(env.ROOM.idFromName(code));
    const res = await stub.fetch('https://room/init', {
      method: 'POST',
      body: JSON.stringify({ code, hostUserId, isPublic, config }),
    });
    if (res.ok) return await res.json() as { code: string; config: GameConfig };
  }
  throw new Error('Could not allocate a room code');
}

const CORS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
  'Access-Control-Allow-Headers': 'Content-Type, Authorization',
};

function json(body: unknown, status = 200): Response {
  return Response.json(body, { status, headers: CORS });
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    const path = url.pathname.replace(/\/+$/, '');

    if (request.method === 'OPTIONS') return new Response(null, { headers: CORS });

    if (path === '/health' || path === '') return json({ ok: true, service: '3d-chess' });

    if (path === '/auth/guest' && request.method === 'POST') {
      const body = await request.json().catch(() => ({})) as { name?: unknown };
      const { token, payload } = await issueToken(env.TOKEN_SECRET, cleanName(body.name));
      return json({ token, userId: payload.userId, name: payload.name });
    }

    const bearer = request.headers.get('Authorization')?.replace(/^Bearer\s+/i, '') ?? null;
    const user = await verifyToken(env.TOKEN_SECRET, bearer ?? url.searchParams.get('token'));

    if (path === '/rooms' && request.method === 'POST') {
      if (!user) return json({ error: 'Sign in first' }, 401);
      const body = await request.json().catch(() => ({})) as { config?: Partial<GameConfig>; isPublic?: boolean };
      const room = await createRoom(env, user.userId, body.config ?? {}, body.isPublic === true);
      return json(room);
    }

    const roomMatch = path.match(/^\/rooms\/([A-Za-z0-9]{6})(\/connect)?$/);
    if (roomMatch) {
      const code = roomMatch[1].toUpperCase();
      const stub = env.ROOM.get(env.ROOM.idFromName(code));
      if (!roomMatch[2]) {
        const res = await stub.fetch('https://room/info');
        return json(await res.json(), res.status);
      }
      if (!user) return new Response('Sign in first', { status: 401 });
      return stub.fetch('https://room/connect', {
        headers: { Upgrade: 'websocket', 'X-User-Id': user.userId, 'X-User-Name': user.name },
      });
    }

    if (path === '/matchmaking/connect') {
      if (!user) return new Response('Sign in first', { status: 401 });
      const stub = env.MATCHMAKER.get(env.MATCHMAKER.idFromName('global'));
      return stub.fetch('https://matchmaker/connect', {
        headers: { Upgrade: 'websocket', 'X-User-Id': user.userId, 'X-User-Name': user.name },
      });
    }

    return json({ error: 'Not found' }, 404);
  },
};
