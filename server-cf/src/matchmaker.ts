import { DurableObject } from 'cloudflare:workers';
import { sanitizeConfig } from './room.js';
import { createRoom, type Env } from './index.js';
import type { GameConfig } from './types/game.js';

// ─────────────────────────────────────────────────────────────────────────────
// Matchmaker — a single Durable Object holding the "Find Match" queue.
//
// Players connect over WebSocket and send { type: 'matchmaking:join', config }.
// Players asking for the same board size and player count are grouped; once a
// group is full a room is created and everyone gets { type: 'matchmaking:found',
// roomCode }, then connects to that room as usual.
// ─────────────────────────────────────────────────────────────────────────────

interface Waiting {
  userId: string;
  name: string;
  key: string | null;      // "4x4x4|2" once they've joined the queue
  config: Partial<GameConfig> | null;
  since: number;
}

export class Matchmaker extends DurableObject<Env> {
  async fetch(request: Request): Promise<Response> {
    if (request.headers.get('Upgrade') !== 'websocket') return new Response('Expected WebSocket', { status: 426 });
    const userId = request.headers.get('X-User-Id')!;
    const name = request.headers.get('X-User-Name') ?? 'Player';

    for (const old of this.ctx.getWebSockets(userId)) {
      try { old.close(4000, 'Replaced by a new connection'); } catch { /* already closed */ }
    }
    const pair = new WebSocketPair();
    this.ctx.acceptWebSocket(pair[1], [userId]);
    pair[1].serializeAttachment({ userId, name, key: null, config: null, since: Date.now() } satisfies Waiting);
    return new Response(null, { status: 101, webSocket: pair[0] });
  }

  async webSocketMessage(ws: WebSocket, raw: string | ArrayBuffer): Promise<void> {
    let msg: Record<string, unknown>;
    try {
      msg = JSON.parse(typeof raw === 'string' ? raw : new TextDecoder().decode(raw));
    } catch {
      return;
    }
    const me = ws.deserializeAttachment() as Waiting;

    if (msg.type === 'matchmaking:join') {
      const config = { boardSize: '4x4x4', playerCount: 2, ...sanitizeConfig(msg.config as Partial<GameConfig>) } as Partial<GameConfig>;
      me.config = config;
      me.key = `${config.boardSize}|${config.playerCount}`;
      me.since = Date.now();
      ws.serializeAttachment(me);
      ws.send(JSON.stringify({ type: 'matchmaking:queued', key: me.key, waiting: this.group(me.key).length }));
      await this.tryMatch(me.key);
    } else if (msg.type === 'matchmaking:leave') {
      ws.close(1000, 'Left queue');
    } else if (msg.type === 'ping') {
      ws.send(JSON.stringify({ type: 'pong', t: Date.now() }));
    }
  }

  private group(key: string): WebSocket[] {
    return this.ctx.getWebSockets()
      .filter(ws => (ws.deserializeAttachment() as Waiting | null)?.key === key)
      .sort((a, b) => (a.deserializeAttachment() as Waiting).since - (b.deserializeAttachment() as Waiting).since);
  }

  private async tryMatch(key: string): Promise<void> {
    const queue = this.group(key);
    const first = queue[0]?.deserializeAttachment() as Waiting | undefined;
    if (!first?.config) return;
    const needed = first.config.playerCount ?? 2;
    if (queue.length < needed) return;

    const players = queue.slice(0, needed);
    const host = players[0].deserializeAttachment() as Waiting;
    const { code } = await createRoom(this.env, host.userId, first.config, false);
    for (const ws of players) {
      ws.send(JSON.stringify({ type: 'matchmaking:found', roomCode: code }));
      ws.close(1000, 'Matched');
    }
  }
}
