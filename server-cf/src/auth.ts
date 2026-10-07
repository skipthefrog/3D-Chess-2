// ─────────────────────────────────────────────────────────────────────────────
// Guest tokens: base64url(JSON payload) + "." + base64url(HMAC-SHA256 signature).
// No accounts yet; a token just gives a player a stable id so they can reconnect
// to their seat. Signed with the TOKEN_SECRET secret.
// ─────────────────────────────────────────────────────────────────────────────

export interface TokenPayload {
  userId: string;
  name: string;
  iat: number; // issued at, ms
}

const TOKEN_LIFETIME_MS = 1000 * 60 * 60 * 24 * 90; // 90 days

function b64url(bytes: Uint8Array): string {
  let s = '';
  for (const b of bytes) s += String.fromCharCode(b);
  return btoa(s).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fromB64url(text: string): Uint8Array {
  const s = atob(text.replace(/-/g, '+').replace(/_/g, '/'));
  return Uint8Array.from(s, c => c.charCodeAt(0));
}

async function key(secret: string): Promise<CryptoKey> {
  return crypto.subtle.importKey(
    'raw', new TextEncoder().encode(secret), { name: 'HMAC', hash: 'SHA-256' }, false, ['sign', 'verify'],
  );
}

export async function issueToken(secret: string, name: string): Promise<{ token: string; payload: TokenPayload }> {
  const payload: TokenPayload = { userId: crypto.randomUUID(), name, iat: Date.now() };
  const body = b64url(new TextEncoder().encode(JSON.stringify(payload)));
  const sig = new Uint8Array(await crypto.subtle.sign('HMAC', await key(secret), new TextEncoder().encode(body)));
  return { token: `${body}.${b64url(sig)}`, payload };
}

export async function verifyToken(secret: string, token: string | null): Promise<TokenPayload | null> {
  if (!token) return null;
  const [body, sig] = token.split('.');
  if (!body || !sig) return null;
  try {
    const ok = await crypto.subtle.verify('HMAC', await key(secret), fromB64url(sig), new TextEncoder().encode(body));
    if (!ok) return null;
    const payload = JSON.parse(new TextDecoder().decode(fromB64url(body))) as TokenPayload;
    if (Date.now() - payload.iat > TOKEN_LIFETIME_MS) return null;
    return payload;
  } catch {
    return null;
  }
}

/** Display names: trimmed, printable, 1–20 characters */
export function cleanName(raw: unknown): string {
  const name = typeof raw === 'string' ? raw.replace(/[^\p{L}\p{N} _.-]/gu, '').trim().slice(0, 20) : '';
  return name || `Player${Math.floor(1000 + Math.random() * 9000)}`;
}
