// auth.js - Authentication and stats backed by Supabase.
//
// All Supabase calls go through this module so the rest of the codebase
// doesn't need to know whether auth is enabled. If SUPABASE_URL or
// SUPABASE_SERVICE_KEY is missing, every operation no-ops and `enabled`
// stays false — the game still runs anonymously.
//
// Email/password signups use Node's built-in scrypt with a per-user salt.
// Google sign-in uses Supabase's hosted OAuth, then we map the resulting
// Supabase user onto our own users table by (provider, sub).
const crypto = require('crypto');
let createClient = null;
try { ({ createClient } = require('@supabase/supabase-js')); } catch (_) { /* package not installed */ }

const SUPABASE_URL = process.env.SUPABASE_URL;
const SUPABASE_SERVICE_KEY = process.env.SUPABASE_SERVICE_KEY;
const SUPABASE_ANON_KEY = process.env.SUPABASE_ANON_KEY;
const SESSION_DAYS = 30;
const MIN_PASSWORD_LEN = 6;

const enabled = !!(createClient && SUPABASE_URL && SUPABASE_SERVICE_KEY);
const oauthEnabled = !!(enabled && SUPABASE_ANON_KEY);
const supabase = enabled
  ? createClient(SUPABASE_URL, SUPABASE_SERVICE_KEY, { auth: { persistSession: false } })
  : null;

if (!enabled) {
  // eslint-disable-next-line no-console
  console.warn('[auth] Supabase not configured — running without accounts/stats. Set SUPABASE_URL + SUPABASE_SERVICE_KEY in .env to enable.');
} else if (!oauthEnabled) {
  // eslint-disable-next-line no-console
  console.warn('[auth] SUPABASE_ANON_KEY not set — Google sign-in disabled. Username/password still works.');
}

// ---- Helpers ----
function hashPassword(password, salt) {
  if (!salt) salt = crypto.randomBytes(16).toString('hex');
  const hash = crypto.scryptSync(password, salt, 64).toString('hex');
  return { hash, salt };
}

function verifyPassword(password, hash, salt) {
  if (!hash || !salt) return false;
  let computed;
  try { computed = crypto.scryptSync(password, salt, 64).toString('hex'); }
  catch (_) { return false; }
  const a = Buffer.from(computed, 'hex');
  const b = Buffer.from(hash, 'hex');
  if (a.length !== b.length) return false;
  return crypto.timingSafeEqual(a, b);
}

function makeToken() {
  return crypto.randomBytes(32).toString('hex');
}

function isValidUsername(name) {
  return typeof name === 'string' && /^[a-zA-Z0-9_-]{3,20}$/.test(name.trim());
}

function sanitizeUsername(s) {
  return String(s || '').replace(/[^a-zA-Z0-9_-]/g, '').slice(0, 20);
}

function publicUser(u) {
  if (!u) return null;
  return {
    id: u.id,
    username: u.username,
    created_at: u.created_at,
    games_played: u.games_played || 0,
    games_won: u.games_won || 0,
    games_lost: u.games_lost || 0,
    classic_played: u.classic_played || 0,
    classic_won: u.classic_won || 0,
    classic_lost: u.classic_lost || 0,
    liarsbar_played: u.liarsbar_played || 0,
    liarsbar_won: u.liarsbar_won || 0,
    liarsbar_lost: u.liarsbar_lost || 0,
    liarsbar_eliminations: u.liarsbar_eliminations || 0,
    has_password: !!u.password_hash,
    oauth_provider: u.oauth_provider || null
  };
}

// ---- Email / password ----
async function signup(username, password) {
  if (!enabled) throw new Error('Accounts are disabled on this server.');
  username = String(username || '').trim();
  password = String(password || '');
  if (!isValidUsername(username)) throw new Error('Username must be 3–20 chars: letters, digits, _ or -.');
  if (password.length < MIN_PASSWORD_LEN) throw new Error(`Password must be at least ${MIN_PASSWORD_LEN} characters.`);
  const lower = username.toLowerCase();
  const { data: existing } = await supabase
    .from('users')
    .select('id')
    .eq('username_lower', lower)
    .maybeSingle();
  if (existing) throw new Error('Username already taken.');
  const { hash, salt } = hashPassword(password);
  const { data: user, error } = await supabase
    .from('users')
    .insert({ username, username_lower: lower, password_hash: hash, password_salt: salt })
    .select()
    .single();
  if (error) throw new Error('Signup failed: ' + error.message);
  return user;
}

async function login(username, password) {
  if (!enabled) throw new Error('Accounts are disabled on this server.');
  const lower = String(username || '').trim().toLowerCase();
  const { data: user } = await supabase
    .from('users')
    .select('*')
    .eq('username_lower', lower)
    .maybeSingle();
  if (!user) throw new Error('Wrong username or password.');
  if (!user.password_hash) throw new Error('This account uses Google sign-in, not a password.');
  if (!verifyPassword(password, user.password_hash, user.password_salt)) {
    throw new Error('Wrong username or password.');
  }
  return user;
}

// ---- Sessions ----
async function createSession(userId) {
  if (!enabled) return null;
  const token = makeToken();
  const expires = new Date(Date.now() + SESSION_DAYS * 24 * 60 * 60 * 1000).toISOString();
  const { error } = await supabase.from('sessions').insert({ token, user_id: userId, expires_at: expires });
  if (error) throw new Error('Could not create session: ' + error.message);
  return token;
}

async function deleteSession(token) {
  if (!enabled || !token) return;
  await supabase.from('sessions').delete().eq('token', token);
}

async function getUserByToken(token) {
  if (!enabled || !token) return null;
  const { data: session } = await supabase
    .from('sessions')
    .select('user_id, expires_at')
    .eq('token', token)
    .maybeSingle();
  if (!session) return null;
  if (new Date(session.expires_at).getTime() < Date.now()) {
    await supabase.from('sessions').delete().eq('token', token);
    return null;
  }
  const { data: user } = await supabase
    .from('users')
    .select('*')
    .eq('id', session.user_id)
    .maybeSingle();
  return user || null;
}

// ---- Stats ----
async function recordGameStats(entries, activeModifiers) {
  if (!enabled) return;
  const mods = Array.isArray(activeModifiers) ? activeModifiers : [];
  for (const e of entries || []) {
    if (!e || !e.userId) continue;
    try {
      await supabase.rpc('increment_user_stats', {
        p_user_id: e.userId,
        p_won: !!e.won,
        p_lost: !!e.lost,
        p_mode: e.mode === 'liarsbar' ? 'liarsbar' : 'classic',
        p_eliminated: !!e.eliminated
      });
      for (const mod of mods) {
        await supabase.rpc('increment_modifier_stats', {
          p_user_id: e.userId,
          p_modifier_key: String(mod),
          p_won: !!e.won
        });
      }
    } catch (err) {
      // eslint-disable-next-line no-console
      console.error('[auth] stats update failed for', e.userId, err && err.message);
    }
  }
}

async function leaderboard(limit) {
  if (!enabled) return [];
  const max = Math.max(1, Math.min(100, parseInt(limit, 10) || 20));
  const { data, error } = await supabase
    .from('users')
    .select('id, username, games_played, games_won, games_lost, classic_won, liarsbar_won, liarsbar_eliminations')
    .gt('games_played', 0)
    .order('games_won', { ascending: false })
    .order('games_played', { ascending: true })
    .limit(max);
  if (error) {
    // eslint-disable-next-line no-console
    console.error('[auth] leaderboard query failed', error.message);
    return [];
  }
  return (data || []).map(u => ({
    ...u,
    win_rate: u.games_played > 0 ? Math.round((u.games_won / u.games_played) * 1000) / 10 : 0
  }));
}

async function userModifierStats(userId) {
  if (!enabled || !userId) return [];
  const { data } = await supabase
    .from('modifier_stats')
    .select('modifier_key, games_active, games_won')
    .eq('user_id', userId)
    .order('games_active', { ascending: false });
  return data || [];
}

// ---- Google OAuth bridge ----
//
// The browser does the Google OAuth dance with Supabase's JS client. After
// the redirect, the browser posts the resulting Supabase JWT to our backend.
// We verify it by calling Supabase's /auth/v1/user (which only answers if
// the token is valid), then look up or create a row in our users table
// keyed on (oauth_provider, oauth_sub).
async function verifySupabaseUser(jwt) {
  if (!oauthEnabled || !jwt) return null;
  try {
    const r = await fetch(`${SUPABASE_URL}/auth/v1/user`, {
      headers: {
        'Authorization': `Bearer ${jwt}`,
        'apikey': SUPABASE_ANON_KEY
      }
    });
    if (!r.ok) return null;
    return await r.json();
  } catch (_) {
    return null;
  }
}

async function findOrCreateOAuthUser({ provider, sub, email, username }) {
  if (!enabled) throw new Error('Accounts are disabled on this server.');
  if (!provider || !sub) throw new Error('OAuth payload is incomplete.');

  // 1) Existing link by (provider, sub).
  const { data: byOauth } = await supabase
    .from('users')
    .select('*')
    .eq('oauth_provider', provider)
    .eq('oauth_sub', sub)
    .maybeSingle();
  if (byOauth) return { user: byOauth, isNew: false };

  // 2) New user — must have a username.
  const trimmed = (username || '').trim();
  if (!trimmed) {
    const suggested = email ? sanitizeUsername(email.split('@')[0]) : '';
    return { needsUsername: true, suggested };
  }
  if (!isValidUsername(trimmed)) throw new Error('Username must be 3–20 chars: letters, digits, _ or -.');

  const lower = trimmed.toLowerCase();
  const { data: dup } = await supabase
    .from('users')
    .select('id')
    .eq('username_lower', lower)
    .maybeSingle();
  if (dup) throw new Error('Username already taken.');

  const { data: user, error } = await supabase
    .from('users')
    .insert({
      username: trimmed,
      username_lower: lower,
      email: email || null,
      oauth_provider: provider,
      oauth_sub: sub
    })
    .select()
    .single();
  if (error) throw new Error('Could not create account: ' + error.message);
  return { user, isNew: true };
}

module.exports = {
  enabled,
  oauthEnabled,
  supabaseUrl: SUPABASE_URL || '',
  supabaseAnonKey: SUPABASE_ANON_KEY || '',
  signup,
  login,
  createSession,
  deleteSession,
  getUserByToken,
  publicUser,
  recordGameStats,
  leaderboard,
  userModifierStats,
  isValidUsername,
  verifySupabaseUser,
  findOrCreateOAuthUser,
  MIN_PASSWORD_LEN
};
