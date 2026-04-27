// server-beta-rooms.js — server-authoritative game state for beta multiplayer
// rooms. 2–4 humans share the same 9-floor run, each with their own personal
// run deck, hearts, and gold. MVP scope: core round/floor loop, hearts,
// best-of-3 floors, run end. Jokers / consumables / affixes / forks / shop /
// modifiers are NOT yet wired and can layer on top in later phases.

const crypto = require('crypto');

// ---------- Constants ----------
const RANKS_PLAYABLE = ['A', 'K', 'Q', '10'];        // valid target ranks
const ALL_RANKS = ['A', 'K', 'Q', '10', 'J'];        // includes Jack (wild bluff)
const HAND_SIZE = 5;
const RUN_DECK_PER_RANK = 3;                          // each player: 3 of A/K/Q/10 = 12 cards
const ROUND_DECK_RANK_CAP = 8;                        // max cards of any rank in a round
const BASE_JACKS_PER_ROUND = 6;                       // base J's added by the table
const STARTING_HEARTS = 3;
const ROUNDS_TO_WIN_FLOOR = 2;                        // best-of-3 → 2 wins clears the floor
const TOTAL_FLOORS = 9;
const MAX_PLAYERS = 4;
const MIN_PLAYERS = 2;
const GOLD_PLACE_1 = 20;
const GOLD_PLACE_2 = 10;
const GOLD_PER_FLOOR_WIN = 50;
const STARTING_GOLD = 50;
const CHALLENGE_WINDOW_MS = 8 * 1000;
const HEART_SHARDS_REQUIRED = 3;

// Character roster — kept in sync with public/beta.js characters. Server only
// honors the run-level effects (gold, run-deck Gilded), not jokers, since MVP
// doesn't run joker logic.
const CHARACTERS = {
  ace: { id: 'ace', name: 'The Ace' },
  trickster: { id: 'trickster', name: 'The Trickster' },
  hoarder: { id: 'hoarder', name: 'The Hoarder', handSizeBonus: 1 },
  banker: { id: 'banker', name: 'The Banker', startingGold: 150, startingGildedA: true },
  bait: { id: 'bait', name: 'The Bait' },
  gambler: { id: 'gambler', name: 'The Gambler', goldMultiplier: 1.5 },
};
const FLOOR_AFFIX_POOL = ['gilded', 'glass', 'spiked', 'cursed',
                          'steel', 'mirage', 'hollow', 'echo'];

// ---------- Helpers ----------
function newId() { return crypto.randomBytes(8).toString('hex'); }

function makeRoomId() {
  const chars = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
  let id = '';
  for (let i = 0; i < 5; i++) id += chars[Math.floor(Math.random() * chars.length)];
  return id;
}

function shuffle(arr) {
  const a = arr.slice();
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    const t = a[i]; a[i] = a[j]; a[j] = t;
  }
  return a;
}

// ---------- Module state ----------
const betaRooms = {};

function newBetaRoom(id) {
  return {
    id,
    type: 'beta',
    hostId: null,
    players: [],     // { id, socketId, userId, username, name, character, runDeck,
                     //   hand, hearts, gold, roundsWon, heartShards, eliminated,
                     //   finishedThisRound, connected, removalTimer }
    started: false,
    runStarted: false,
    currentFloor: 1,
    targetRank: null,
    currentTurnIdx: 0,
    pile: [],
    drawPile: [],
    lastPlay: null,         // { playerIdx, claim, count, cardIds }
    challengeOpen: false,
    challengerIdx: -1,
    challengeDeadline: null,
    challengeTimer: null,
    placements: [],         // round-level: order in which players emptied hands
    log: [],
    runOver: false,
    runWinnerId: null,
    createdAt: Date.now(),
  };
}

function findPlayerById(room, id) { return room.players.find(p => p.id === id); }
function findPlayerBySocket(room, sid) { return room.players.find(p => p.socketId === sid); }

// ---------- Public state shape (sent to clients) ----------
function publicBetaState(room, requestingPlayerId) {
  const me = requestingPlayerId ? findPlayerById(room, requestingPlayerId) : null;
  return {
    id: room.id,
    type: 'beta',
    hostId: room.hostId,
    started: room.started,
    runStarted: room.runStarted,
    runOver: room.runOver,
    runWinnerId: room.runWinnerId,
    currentFloor: room.currentFloor,
    totalFloors: TOTAL_FLOORS,
    targetRank: room.targetRank,
    currentTurnIdx: room.currentTurnIdx,
    pileSize: room.pile.length,
    drawSize: room.drawPile.length,
    lastPlay: room.lastPlay
      ? { playerIdx: room.lastPlay.playerIdx, claim: room.lastPlay.claim, count: room.lastPlay.count }
      : null,
    challengeOpen: room.challengeOpen,
    challengerIdx: room.challengerIdx,
    challengeDeadline: room.challengeDeadline,
    placements: room.placements.slice(),
    log: room.log.slice(-40),
    players: room.players.map(p => ({
      id: p.id,
      name: p.name,
      username: p.username || null,
      characterId: p.character ? p.character.id : null,
      characterName: p.character ? p.character.name : null,
      hearts: p.hearts,
      gold: p.gold,
      roundsWon: p.roundsWon,
      heartShards: p.heartShards || 0,
      handCount: p.hand ? p.hand.length : 0,
      runDeckCount: p.runDeck ? p.runDeck.length : 0,
      eliminated: !!p.eliminated,
      finishedThisRound: !!p.finishedThisRound,
      connected: !!p.connected,
    })),
    // Private state — only the requesting player sees their own hand
    mine: me
      ? {
          id: me.id,
          hand: (me.hand || []).map(c => ({ rank: c.rank, id: c.id, owner: c.owner, affix: c.affix || null })),
          runDeck: (me.runDeck || []).map(c => ({ rank: c.rank, id: c.id, affix: c.affix || null })),
        }
      : null,
  };
}

function broadcast(io, room) {
  // Each connected player gets a personalized snapshot (different `mine` data)
  for (const p of room.players) {
    if (!p.connected || !p.socketId) continue;
    io.to(p.socketId).emit('beta:state', publicBetaState(room, p.id));
  }
}

function log(room, msg) {
  room.log.push(msg);
  if (room.log.length > 200) room.log.splice(0, room.log.length - 200);
}

// ---------- Run setup ----------
function buildInitialRunDeck(playerIdx) {
  const deck = [];
  for (const r of ['A', 'K', 'Q', '10']) {
    for (let i = 0; i < RUN_DECK_PER_RANK; i++) {
      deck.push({
        rank: r,
        id: 'p' + playerIdx + '_' + r + '_' + i + '_' + newId().slice(0, 4),
        owner: playerIdx,
        affix: null,
      });
    }
  }
  return deck;
}

function applyCharacter(player) {
  const ch = player.character;
  if (!ch) return;
  if (ch.startingGold) player.gold = ch.startingGold;
  if (ch.startingGildedA) {
    const aCard = player.runDeck.find(c => c.rank === 'A' && !c.affix);
    if (aCard) aCard.affix = 'gilded';
  }
}

function startRun(room) {
  room.runStarted = true;
  room.currentFloor = 1;
  room.runOver = false;
  room.runWinnerId = null;
  for (let i = 0; i < room.players.length; i++) {
    const p = room.players[i];
    p.runDeck = buildInitialRunDeck(i);
    p.hearts = STARTING_HEARTS;
    p.gold = STARTING_GOLD;
    p.roundsWon = 0;
    p.heartShards = 0;
    p.eliminated = false;
    applyCharacter(p);
  }
  log(room, `Run started — Floor 1 of ${TOTAL_FLOORS}.`);
  startRound(room);
}

// ---------- Round dealer ----------
function buildRoundDeck(room) {
  const deck = [];
  for (let i = 0; i < BASE_JACKS_PER_ROUND; i++) {
    deck.push({ rank: 'J', id: 'rd_J_' + i, owner: -1, affix: null });
  }
  const buckets = { 'A': [], 'K': [], 'Q': [], '10': [], 'J': [] };
  for (const p of room.players) {
    for (const c of (p.runDeck || [])) {
      if (buckets[c.rank]) buckets[c.rank].push({ ...c });
    }
  }
  for (const r of Object.keys(buckets)) {
    const cards = buckets[r];
    const cap = (r === 'J')
      ? Math.max(0, ROUND_DECK_RANK_CAP - BASE_JACKS_PER_ROUND)
      : ROUND_DECK_RANK_CAP;
    if (cards.length <= cap) {
      for (const c of cards) deck.push(c);
      continue;
    }
    const affixed = shuffle(cards.filter(c => c.affix));
    const plain = shuffle(cards.filter(c => !c.affix));
    const ordered = affixed.concat(plain);
    for (let i = 0; i < cap; i++) deck.push(ordered[i]);
  }
  return shuffle(deck);
}

function applyFloorAffixesToDrawPile(drawPile, floor) {
  const target = Math.max(0, Math.min(9, floor | 0));
  if (target <= 0 || !drawPile || drawPile.length === 0) return 0;
  const candidates = drawPile.filter(c => !c.affix);
  if (candidates.length === 0) return 0;
  const sh = shuffle(candidates);
  const n = Math.min(target, sh.length);
  for (let i = 0; i < n; i++) {
    sh[i].affix = FLOOR_AFFIX_POOL[Math.floor(Math.random() * FLOOR_AFFIX_POOL.length)];
  }
  return n;
}

function startRound(room) {
  // Reset round-level state
  for (const p of room.players) {
    p.hand = [];
    p.finishedThisRound = false;
  }
  room.pile = [];
  room.lastPlay = null;
  room.placements = [];
  room.challengeOpen = false;
  room.challengerIdx = -1;
  if (room.challengeTimer) { clearTimeout(room.challengeTimer); room.challengeTimer = null; }

  // Build deck and deal
  const deck = buildRoundDeck(room);
  const hands = room.players.map(() => []);
  // Hoarder gets +1 hand size
  const handSizeFor = (p) => {
    const bonus = (p.character && p.character.handSizeBonus) || 0;
    return HAND_SIZE + bonus;
  };
  // Deal round-robin up to per-player hand size
  let dealIdx = 0;
  let round = 0;
  while (round < 10) {
    let dealt = 0;
    for (let i = 0; i < room.players.length; i++) {
      const p = room.players[i];
      if (p.eliminated) continue;
      if (hands[i].length < handSizeFor(p) && deck.length > 0) {
        hands[i].push(deck.pop());
        dealt++;
      }
    }
    if (dealt === 0) break;
    round++;
  }
  for (let i = 0; i < room.players.length; i++) {
    room.players[i].hand = hands[i];
  }
  room.drawPile = deck;

  // Per-floor random-affix infusion on the draw pile
  const infused = applyFloorAffixesToDrawPile(room.drawPile, room.currentFloor);
  if (infused > 0) {
    log(room, `Floor ${room.currentFloor} static: ${infused} draw-pile card${infused === 1 ? '' : 's'} carry random affixes.`);
  }

  // Pick a target rank
  room.targetRank = RANKS_PLAYABLE[Math.floor(Math.random() * RANKS_PLAYABLE.length)];

  // Pick first turn — first non-eliminated player
  room.currentTurnIdx = 0;
  for (let i = 0; i < room.players.length; i++) {
    if (!room.players[i].eliminated) { room.currentTurnIdx = i; break; }
  }
  log(room, `Floor ${room.currentFloor} round — target rank: ${room.targetRank}.`);
}

// ---------- Action handling ----------
function isPlayerActive(p) {
  return !p.eliminated && !p.finishedThisRound;
}

function activeCount(room) {
  return room.players.filter(isPlayerActive).length;
}

function findNextActiveIdx(room, fromIdx) {
  if (room.players.length === 0) return 0;
  let i = fromIdx;
  for (let n = 0; n < room.players.length; n++) {
    i = (i + 1) % room.players.length;
    if (isPlayerActive(room.players[i])) return i;
  }
  return fromIdx;
}

function handlePlay(room, playerId, cardIds) {
  const p = findPlayerById(room, playerId);
  if (!p) return { error: 'Not in room.' };
  const idx = room.players.indexOf(p);
  if (idx !== room.currentTurnIdx) return { error: 'Not your turn.' };
  if (room.challengeOpen) return { error: 'Challenge is open — wait.' };
  if (!isPlayerActive(p)) return { error: 'You are not active this round.' };
  if (!Array.isArray(cardIds) || cardIds.length === 0 || cardIds.length > 3) {
    return { error: 'Play 1–3 cards.' };
  }
  const cards = [];
  for (const id of cardIds) {
    const c = p.hand.find(x => x.id === id);
    if (!c) return { error: 'You do not have that card.' };
    cards.push(c);
  }
  // Remove from hand
  p.hand = p.hand.filter(c => !cardIds.includes(c.id));
  // Add to pile
  for (const c of cards) room.pile.push(c);

  room.lastPlay = {
    playerIdx: idx,
    claim: room.targetRank,
    count: cards.length,
    cardIds: cards.map(c => c.id),
  };
  log(room, `${p.name} plays ${cards.length} card${cards.length === 1 ? '' : 's'} as ${room.targetRank}.`);

  // Check finish
  if (p.hand.length === 0) {
    p.finishedThisRound = true;
    room.placements.push(idx);
    log(room, `${p.name} finished their hand.`);
  }

  // Open challenge window for next active player
  openChallengeWindow(room);

  return { ok: true };
}

function openChallengeWindow(room) {
  room.challengeOpen = true;
  const nextIdx = findNextActiveIdx(room, room.currentTurnIdx);
  room.challengerIdx = nextIdx;
  room.challengeDeadline = Date.now() + CHALLENGE_WINDOW_MS;
  if (room.challengeTimer) clearTimeout(room.challengeTimer);
  room.challengeTimer = setTimeout(() => {
    if (!betaRooms[room.id]) return;
    if (!room.challengeOpen) return;
    // Treat as pass (no challenge)
    handlePassNoChallengeInternal(room);
    if (room._io) broadcast(room._io, room);
  }, CHALLENGE_WINDOW_MS + 50);
}

function handlePass(room, playerId) {
  const p = findPlayerById(room, playerId);
  if (!p) return { error: 'Not in room.' };
  const idx = room.players.indexOf(p);
  if (!room.challengeOpen) return { error: 'Nothing to pass on.' };
  if (idx !== room.challengerIdx) return { error: 'Not your call.' };
  return handlePassNoChallengeInternal(room);
}

function handlePassNoChallengeInternal(room) {
  room.challengeOpen = false;
  if (room.challengeTimer) { clearTimeout(room.challengeTimer); room.challengeTimer = null; }
  room.challengeDeadline = null;
  // Round may end if too few active
  if (activeCount(room) <= 1) {
    return endRoundIfDone(room);
  }
  // Advance turn to next active player AFTER the challenger position
  room.currentTurnIdx = findNextActiveIdx(room, room.challengerIdx - 1);
  return { ok: true };
}

function handleLiar(room, playerId) {
  const p = findPlayerById(room, playerId);
  if (!p) return { error: 'Not in room.' };
  const idx = room.players.indexOf(p);
  if (!room.challengeOpen) return { error: 'No claim to challenge.' };
  if (idx !== room.challengerIdx) return { error: 'Not your call.' };
  if (!room.lastPlay) return { error: 'No play to challenge.' };

  const lastIds = room.lastPlay.cardIds || [];
  const claim = room.lastPlay.claim;
  const playedCards = room.pile.filter(c => lastIds.includes(c.id));
  const wasLie = playedCards.some(c => c.rank !== claim && c.rank !== 'J');

  room.challengeOpen = false;
  if (room.challengeTimer) { clearTimeout(room.challengeTimer); room.challengeTimer = null; }
  room.challengeDeadline = null;

  const liarIdx = room.lastPlay.playerIdx;
  const liarP = room.players[liarIdx];
  const challengerP = p;

  if (wasLie) {
    // Liar was lying → liar takes the pile back to hand
    log(room, `${challengerP.name} called LIAR — caught! ${liarP.name} takes the pile back.`);
    for (const c of room.pile) liarP.hand.push(c);
    room.pile = [];
    if (liarP.finishedThisRound) {
      // They had finished but now have cards again — back to active
      liarP.finishedThisRound = false;
      const pos = room.placements.indexOf(liarIdx);
      if (pos !== -1) room.placements.splice(pos, 1);
    }
    room.currentTurnIdx = liarIdx;
  } else {
    // Truth-teller — challenger takes the pile
    log(room, `${challengerP.name} called LIAR — wrong! ${challengerP.name} takes the pile.`);
    for (const c of room.pile) challengerP.hand.push(c);
    room.pile = [];
    if (challengerP.finishedThisRound) {
      challengerP.finishedThisRound = false;
      const pos = room.placements.indexOf(idx);
      if (pos !== -1) room.placements.splice(pos, 1);
    }
    room.currentTurnIdx = idx;
  }

  // After taking pile, check round end
  if (activeCount(room) <= 1) {
    return endRoundIfDone(room);
  }
  return { ok: true };
}

// ---------- Round / floor / run resolution ----------
function pickClosestActivePlayer(room) {
  let best = -1, bestSize = Infinity;
  for (let i = 0; i < room.players.length; i++) {
    const p = room.players[i];
    if (!isPlayerActive(p)) continue;
    if (p.hand.length < bestSize) {
      best = i;
      bestSize = p.hand.length;
    }
  }
  if (best === -1) {
    // No active — pick the latest finisher
    best = room.placements[room.placements.length - 1] || 0;
  }
  return best;
}

function endRoundIfDone(room) {
  // If 0 or 1 active player remains, resolve round
  let winnerIdx;
  let message;
  if (room.placements.length === 0) {
    winnerIdx = pickClosestActivePlayer(room);
    message = `Nobody emptied their hand. ${room.players[winnerIdx].name} was closest.`;
  } else {
    winnerIdx = room.placements[0];
    message = `${room.players[winnerIdx].name} finished 1st`;
    if (room.placements.length >= 2) {
      message += `, ${room.players[room.placements[1]].name} 2nd`;
    }
    message += '.';
  }
  endRound(room, winnerIdx, message);
  return { ok: true, roundEnded: true };
}

function endRound(room, winnerIdx, message) {
  const winner = room.players[winnerIdx];
  log(room, `Round end: ${message}`);

  // Placement gold: 1st = +20g, 2nd = +10g
  if (room.placements.length >= 1) {
    const first = room.players[room.placements[0]];
    first.gold = (first.gold || 0) + GOLD_PLACE_1;
  }
  if (room.placements.length >= 2) {
    const second = room.players[room.placements[1]];
    second.gold = (second.gold || 0) + GOLD_PLACE_2;
  }

  // Round wins
  winner.roundsWon = (winner.roundsWon || 0) + 1;

  if (winner.roundsWon >= ROUNDS_TO_WIN_FLOOR) {
    endFloor(room, winnerIdx);
  } else {
    // Next round on same floor
    setTimeout(() => {
      if (!betaRooms[room.id]) return;
      startRound(room);
      if (room._io) broadcast(room._io, room);
    }, 1500);
  }
}

function endFloor(room, winnerIdx) {
  const winner = room.players[winnerIdx];
  log(room, `Floor ${room.currentFloor} cleared by ${winner.name}.`);
  // +Gold for floor winner
  winner.gold = (winner.gold || 0) + GOLD_PER_FLOOR_WIN;

  // Hearts loss for players with 0 round wins this floor (the laggers)
  const survivors = [];
  for (let i = 0; i < room.players.length; i++) {
    const p = room.players[i];
    if (p.eliminated) continue;
    if ((p.roundsWon || 0) === 0) {
      p.hearts = Math.max(0, (p.hearts || 0) - 1);
      log(room, `${p.name} lost a Heart (no round wins this floor) — ${p.hearts} left.`);
      // Heart shards (if won floor at 1 heart — doesn't apply to laggers)
      if (p.hearts <= 0) {
        p.eliminated = true;
        log(room, `${p.name} ran out of Hearts and is eliminated.`);
      } else {
        survivors.push(p);
      }
    } else {
      // Floor winner: heart-shard logic — winning at 1 Heart gives a shard
      if (i === winnerIdx && p.hearts === 1) {
        p.heartShards = (p.heartShards || 0) + 1;
        log(room, `${p.name} earned a Heart shard (${p.heartShards}/${HEART_SHARDS_REQUIRED}).`);
        if (p.heartShards >= HEART_SHARDS_REQUIRED) {
          p.hearts++;
          p.heartShards = 0;
          log(room, `${p.name} restored a Heart from shards!`);
        }
      }
      survivors.push(p);
    }
  }

  // Reset round wins for next floor
  for (const p of room.players) p.roundsWon = 0;

  // Check run end conditions
  const aliveCount = room.players.filter(p => !p.eliminated).length;
  if (aliveCount === 0) {
    endRun(room, null);
    return;
  }
  if (aliveCount === 1 && room.players.length > 1) {
    // Last player standing wins
    const sole = room.players.find(p => !p.eliminated);
    endRun(room, sole.id);
    return;
  }
  if (room.currentFloor >= TOTAL_FLOORS) {
    // Floor 9 cleared — winner clears the run; run ends for everyone
    endRun(room, winner.id);
    return;
  }

  // Advance floor
  room.currentFloor++;
  log(room, `Advancing to Floor ${room.currentFloor}.`);
  setTimeout(() => {
    if (!betaRooms[room.id]) return;
    startRound(room);
    if (room._io) broadcast(room._io, room);
  }, 2000);
}

function endRun(room, winnerId) {
  room.runOver = true;
  room.runWinnerId = winnerId;
  if (winnerId) {
    const w = room.players.find(p => p.id === winnerId);
    log(room, `Run over — ${w ? w.name : 'someone'} wins!`);
  } else {
    log(room, 'Run over — everyone is eliminated.');
  }
  // Progression report happens client-side (server fires a 'beta:runEnded' event
  // and clients POST to /api/beta/run-history etc.)
}

// ---------- Player management ----------
function addPlayer(room, socket, name, user) {
  if (room.players.length >= MAX_PLAYERS) return { error: `Room is full (${MAX_PLAYERS} max).` };
  if (room.runStarted) return { error: 'Run already started — cannot join mid-run.' };
  const displayName = (user && user.username) ||
                      ((name || '').trim().slice(0, 20) || `Player${room.players.length + 1}`);
  const player = {
    id: newId(),
    socketId: socket.id,
    userId: user ? user.id : null,
    username: user ? user.username : null,
    name: displayName,
    character: null,
    runDeck: [],
    hand: [],
    hearts: STARTING_HEARTS,
    gold: STARTING_GOLD,
    roundsWon: 0,
    heartShards: 0,
    eliminated: false,
    finishedThisRound: false,
    connected: true,
    removalTimer: null,
  };
  room.players.push(player);
  socket.join(room.id);
  if (!room.hostId) room.hostId = player.id;
  log(room, `${displayName} joined the room.`);
  return { ok: true, player };
}

function pickCharacter(room, playerId, characterId) {
  const p = findPlayerById(room, playerId);
  if (!p) return { error: 'Not in room.' };
  if (room.runStarted) return { error: 'Run already in progress.' };
  const ch = CHARACTERS[characterId];
  if (!ch) return { error: 'Unknown character.' };
  p.character = ch;
  log(room, `${p.name} picked ${ch.name}.`);
  return { ok: true };
}

function removePlayer(room, playerId) {
  const idx = room.players.findIndex(p => p.id === playerId);
  if (idx === -1) return false;
  const wasHost = room.hostId === playerId;
  const removed = room.players[idx];
  room.players.splice(idx, 1);
  if (room.players.length === 0) {
    delete betaRooms[room.id];
    return true;
  }
  if (wasHost) {
    const nh = room.players.find(p => p.connected) || room.players[0];
    room.hostId = nh.id;
  }
  log(room, `${removed.name} left.`);
  // Adjust current turn if needed
  if (room.runStarted) {
    if (room.currentTurnIdx >= room.players.length) room.currentTurnIdx = 0;
    if (activeCount(room) <= 1) {
      // Force round end
      endRoundIfDone(room);
    }
  }
  return true;
}

// ---------- Module exports ----------
module.exports = {
  betaRooms,
  CHARACTERS,
  MAX_PLAYERS,
  MIN_PLAYERS,
  // Constructors / lifecycle
  newBetaRoom,
  makeRoomId,
  addPlayer,
  removePlayer,
  pickCharacter,
  startRun,
  // Action handlers
  handlePlay,
  handlePass,
  handleLiar,
  // State + broadcasting
  publicBetaState,
  broadcast,
  // Lookup
  findPlayerById,
  findPlayerBySocket,
};
