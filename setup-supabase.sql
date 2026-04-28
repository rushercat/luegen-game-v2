-- Lugen game database schema for Supabase
-- Run this once in the Supabase SQL editor (Project -> SQL Editor -> New query).
-- Safe to re-run: every CREATE uses IF NOT EXISTS.

create extension if not exists pgcrypto;

-- ===== Users =====
create table if not exists users (
  id uuid primary key default gen_random_uuid(),
  username text not null,
  username_lower text not null unique,
  password_hash text not null,
  password_salt text not null,
  created_at timestamptz not null default now(),
  -- aggregate stats
  games_played int not null default 0,
  games_won int not null default 0,
  games_lost int not null default 0,
  -- per-mode stats
  classic_played int not null default 0,
  classic_won int not null default 0,
  classic_lost int not null default 0,
  liarsbar_played int not null default 0,
  liarsbar_won int not null default 0,
  liarsbar_lost int not null default 0,
  liarsbar_eliminations int not null default 0
);

-- ===== Per-modifier breakdown =====
create table if not exists modifier_stats (
  user_id uuid not null references users(id) on delete cascade,
  modifier_key text not null,
  games_active int not null default 0,
  games_won int not null default 0,
  primary key (user_id, modifier_key)
);

-- ===== Session tokens =====
create table if not exists sessions (
  token text primary key,
  user_id uuid not null references users(id) on delete cascade,
  created_at timestamptz not null default now(),
  expires_at timestamptz not null
);

create index if not exists idx_sessions_user_id on sessions(user_id);
create index if not exists idx_sessions_expires_at on sessions(expires_at);

-- ===== Optional: a Postgres function for atomic stat increments =====
-- Lets the server call rpc('increment_user_stats', { ... }) instead of
-- read-then-update. Pure SQL is more concurrent-safe.
create or replace function increment_user_stats(
  p_user_id uuid,
  p_won boolean,
  p_lost boolean,
  p_mode text,
  p_eliminated boolean
) returns void as $$
begin
  update users set
    games_played = games_played + 1,
    games_won = games_won + (case when p_won then 1 else 0 end),
    games_lost = games_lost + (case when p_lost then 1 else 0 end),
    classic_played = classic_played + (case when p_mode = 'classic' then 1 else 0 end),
    classic_won = classic_won + (case when p_mode = 'classic' and p_won then 1 else 0 end),
    classic_lost = classic_lost + (case when p_mode = 'classic' and p_lost then 1 else 0 end),
    liarsbar_played = liarsbar_played + (case when p_mode = 'liarsbar' then 1 else 0 end),
    liarsbar_won = liarsbar_won + (case when p_mode = 'liarsbar' and p_won then 1 else 0 end),
    liarsbar_lost = liarsbar_lost + (case when p_mode = 'liarsbar' and p_lost then 1 else 0 end),
    liarsbar_eliminations = liarsbar_eliminations + (case when p_eliminated then 1 else 0 end)
  where id = p_user_id;
end;
$$ language plpgsql;

create or replace function increment_modifier_stats(
  p_user_id uuid,
  p_modifier_key text,
  p_won boolean
) returns void as $$
begin
  insert into modifier_stats (user_id, modifier_key, games_active, games_won)
  values (p_user_id, p_modifier_key, 1, case when p_won then 1 else 0 end)
  on conflict (user_id, modifier_key) do update set
    games_active = modifier_stats.games_active + 1,
    games_won = modifier_stats.games_won + (case when p_won then 1 else 0 end);
end;
$$ language plpgsql;

-- ===== OAuth columns (Google sign-in) =====
-- Re-runnable migration: existing users created before OAuth still work
-- (their oauth_* fields stay null, password fields populated).
alter table users add column if not exists email text;
alter table users add column if not exists oauth_provider text;
alter table users add column if not exists oauth_sub text;

-- ===== Allow OAuth-only accounts =====
-- Drop NOT NULL on the password columns so OAuth users can exist without a
-- password. Keep a CHECK constraint so every row has at least one auth
-- method (a password or an OAuth identity) — otherwise the row is unusable.
alter table users alter column password_hash drop not null;
alter table users alter column password_salt drop not null;
do $$
begin
  if not exists (
    select 1 from pg_constraint where conname = 'users_auth_method_present'
  ) then
    alter table users add constraint users_auth_method_present
      check (
        (password_hash is not null and password_salt is not null)
        or (oauth_provider is not null and oauth_sub is not null)
      );
  end if;
end $$;

-- ===== OAuth uniqueness =====
-- Without this, two rows could share the same (provider, sub) and the
-- find-or-create lookup would silently link an attacker to someone else's
-- account on a hash collision. Partial index so legacy password-only rows
-- (NULL provider/sub) don't get included.
create unique index if not exists users_oauth_provider_sub_unique
  on users (oauth_provider, oauth_sub)
  where oauth_provider is not null and oauth_sub is not null;

-- ===== Lookups by email / oauth_sub =====
-- Speeds up password-reset (when added) and OAuth re-login.
create index if not exists users_email_idx
  on users (email) where email is not null;
create index if not exists users_oauth_sub_idx
  on users (oauth_sub) where oauth_sub is not null;

-- ===== Brute-force protection columns =====
-- The application-level rate limiter is in front of /api/login, but record
-- failed attempts here so the limiter can reset on success and the admin
-- can see anomalies.
alter table users add column if not exists failed_login_attempts int not null default 0;
alter table users add column if not exists last_failed_login_at timestamptz;


-- ===== Beta prototype: roguelike progression + admin flag =====
-- Re-runnable. Track each player's max floor reached and whether they have
-- ever beaten Floor 9. is_admin lets a flagged account unlock everything for
-- testing via the admin button in the beta UI.
alter table users add column if not exists beta_max_floor int not null default 1;
alter table users add column if not exists beta_run_won boolean not null default false;
alter table users add column if not exists is_admin boolean not null default false;

-- Promote a specific user to admin (run this after creating an account):
--   update users set is_admin = true where username = 'YOUR_USERNAME';


-- ===== Phase 6: cosmetics + achievements =====
-- Re-runnable. Cosmetics and achievements are stored as text arrays of
-- IDs the user owns/has earned. Empty by default; populated as the user
-- earns them through play.
alter table users add column if not exists owned_cosmetics text[] not null default '{}';
alter table users add column if not exists earned_achievements text[] not null default '{}';


-- ===== Phase 7: beta run history =====
-- jsonb array of past run summaries for the beta prototype. Capped to ~20
-- most recent entries by the server. Used to render "recent runs" on the
-- beta intro screen.
alter table users add column if not exists beta_run_history jsonb not null default '[]'::jsonb;

-- Defensive cap: if a buggy or malicious client ever bypasses the
-- application's 20-entry cap, this CHECK keeps the column from growing
-- unbounded.
do $$
begin
  if not exists (
    select 1 from pg_constraint where conname = 'beta_run_history_size_cap'
  ) then
    alter table users add constraint beta_run_history_size_cap
      check (jsonb_array_length(beta_run_history) <= 50);
  end if;
end $$;
