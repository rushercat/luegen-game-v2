// beta-mp.js — Client-side beta multiplayer. Connects via socket.io to
// server-beta-rooms.js, drives the multiplayer beta UI from server broadcasts.
// Re-uses the existing #betaGame DOM where possible.

(function () {
  'use strict';

  // ==================== State ====================
  let socket = null;
  let myRoomId = null;
  let myPlayerId = null;
  let lastState = null;
  const selected = new Set();

  // Character roster (mirror of server) — for the lobby picker
  const CHARACTERS = [
    { id: 'ace',       name: 'The Ace',       passive: 'No special ability.' },
    { id: 'trickster', name: 'The Trickster', passive: 'No special ability.' },
    { id: 'hoarder',   name: 'The Hoarder',   passive: 'Hand size +1 (6 cards).' },
    { id: 'banker',    name: 'The Banker',    passive: 'Start with 150g + a Gilded Ace.' },
    { id: 'bait',      name: 'The Bait',      passive: 'No special ability (yet).' },
    { id: 'gambler',   name: 'The Gambler',   passive: '+50% gold from all sources.' },
  ];

  function affixRingClass(affix) {
    switch (affix) {
      case 'gilded': return 'ring-2 ring-yellow-400';
      case 'glass':  return 'ring-2 ring-cyan-400';
      case 'spiked': return 'ring-2 ring-red-400';
      case 'cursed': return 'ring-2 ring-purple-500';
      case 'steel':  return 'ring-2 ring-gray-300';
      case 'mirage': return 'ring-2 ring-pink-400';
      case 'hollow': return 'ring-2 ring-indigo-400';
      case 'echo':   return 'ring-2 ring-fuchsia-400';
      default: return null;
    }
  }

  function escapeHtml(s) {
    return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }

  function getAuthToken() {
    try { return localStorage.getItem('lugen-auth-token') || null; }
    catch (e) { return null; }
  }

  // ==================== Connect ====================
  function ensureSocket() {
    if (socket) return socket;
    const token = getAuthToken();
    socket = io({ auth: { token: token || undefined } });

    socket.on('beta:joined', ({ roomId, playerId }) => {
      myRoomId = roomId;
      myPlayerId = playerId;
      const codeEl = document.getElementById('betaMpRoomCode');
      if (codeEl) codeEl.textContent = roomId;
      showLobby();
    });

    socket.on('beta:state', (state) => {
      lastState = state;
      render();
    });

    socket.on('beta:error', ({ message }) => {
      const errEl = document.getElementById('betaMpError');
      if (errEl) {
        errEl.textContent = message || 'Server error.';
        errEl.classList.remove('hidden');
        setTimeout(() => errEl.classList.add('hidden'), 5000);
      } else {
        alert(message || 'Server error.');
      }
    });
  }

  // ==================== Screen visibility ====================
  function hideAllMpScreens() {
    const ids = ['betaMpEntry', 'betaMpLobby', 'betaMpGame', 'betaMpResult'];
    for (const id of ids) {
      const el = document.getElementById(id);
      if (el) el.classList.add('hidden');
    }
  }

  function showEntry() {
    hideAllMpScreens();
    const el = document.getElementById('betaMpEntry');
    if (el) el.classList.remove('hidden');
  }

  function showLobby() {
    hideAllMpScreens();
    const el = document.getElementById('betaMpLobby');
    if (el) el.classList.remove('hidden');
  }

  function showGame() {
    hideAllMpScreens();
    const el = document.getElementById('betaMpGame');
    if (el) el.classList.remove('hidden');
  }

  function showResult() {
    hideAllMpScreens();
    const el = document.getElementById('betaMpResult');
    if (el) el.classList.remove('hidden');
  }

  // ==================== Render ====================
  function render() {
    if (!lastState) return;
    if (lastState.runOver) {
      renderResult();
      return;
    }
    if (!lastState.runStarted) {
      renderLobby();
      showLobby();
      return;
    }
    showGame();
    renderGame();
  }

  function renderLobby() {
    const codeEl = document.getElementById('betaMpRoomCode');
    if (codeEl) codeEl.textContent = lastState.id;

    // Player list
    const listEl = document.getElementById('betaMpPlayerList');
    if (listEl) {
      listEl.innerHTML = '';
      for (const p of lastState.players) {
        const isMe = p.id === myPlayerId;
        const isHost = p.id === lastState.hostId;
        const row = document.createElement('div');
        row.className = 'flex items-center justify-between bg-black/30 px-3 py-2 rounded';
        row.innerHTML =
          '<div>' +
            '<span class="font-bold">' + escapeHtml(p.name) + '</span>' +
            (isMe ? ' <span class="text-xs text-emerald-300">(you)</span>' : '') +
            (isHost ? ' <span class="text-xs text-yellow-300">[host]</span>' : '') +
            (!p.connected ? ' <span class="text-xs text-rose-300">[disconnected]</span>' : '') +
          '</div>' +
          '<div class="text-xs text-emerald-200">' +
            (p.characterName ? escapeHtml(p.characterName) : '<i class="text-white/40">picking...</i>') +
          '</div>';
        listEl.appendChild(row);
      }
    }

    // Character picker (only show options if I haven't picked, or to change)
    const me = (lastState.players || []).find(p => p.id === myPlayerId);
    const myPickEl = document.getElementById('betaMpMyCharacter');
    if (myPickEl) {
      myPickEl.innerHTML = '';
      for (const c of CHARACTERS) {
        const btn = document.createElement('button');
        const picked = me && me.characterId === c.id;
        btn.className = 'p-3 rounded-lg border-2 text-left transition w-full ' +
          (picked ? 'bg-purple-700 border-yellow-300' : 'bg-black/40 border-white/10 hover:border-white/30');
        btn.innerHTML =
          '<div class="font-bold">' + escapeHtml(c.name) + '</div>' +
          '<div class="text-xs text-emerald-200 mt-1">' + escapeHtml(c.passive) + '</div>';
        btn.addEventListener('click', () => {
          socket.emit('beta:pickCharacter', { characterId: c.id });
        });
        myPickEl.appendChild(btn);
      }
    }

    // Host controls
    const startBtn = document.getElementById('betaMpStartBtn');
    const startHint = document.getElementById('betaMpStartHint');
    if (startBtn && startHint) {
      const allPicked = lastState.players.every(p => p.characterId);
      const hasMin = lastState.players.length >= 2;
      const isHost = lastState.hostId === myPlayerId;
      startBtn.classList.toggle('hidden', !isHost);
      startBtn.disabled = !(allPicked && hasMin);
      if (!isHost) {
        startHint.textContent = 'Waiting for the host to start the run...';
      } else if (!hasMin) {
        startHint.textContent = 'Need at least 2 players.';
      } else if (!allPicked) {
        startHint.textContent = 'Waiting for everyone to pick a character.';
      } else {
        startHint.textContent = 'Ready to start!';
      }
    }
  }

  function renderGame() {
    const s = lastState;
    document.getElementById('betaMpFloor').textContent = s.currentFloor + '/' + s.totalFloors;
    document.getElementById('betaMpTarget').textContent = s.targetRank || '—';
    document.getElementById('betaMpPileSize').textContent = s.pileSize;
    document.getElementById('betaMpDrawSize').textContent = s.drawSize;

    // Players row
    const playersEl = document.getElementById('betaMpPlayers');
    playersEl.innerHTML = '';
    for (let i = 0; i < s.players.length; i++) {
      const p = s.players[i];
      const isMe = p.id === myPlayerId;
      const isTurn = i === s.currentTurnIdx && !p.eliminated && !p.finishedThisRound;
      const isChallenger = s.challengeOpen && i === s.challengerIdx;
      const card = document.createElement('div');
      let cls = 'bg-black/40 px-3 py-2 rounded text-sm flex flex-col items-center min-w-[140px]';
      if (isTurn) cls += ' ring-2 ring-yellow-400';
      if (isChallenger) cls += ' ring-2 ring-rose-400';
      if (p.eliminated) cls += ' opacity-50';
      card.className = cls;
      card.innerHTML =
        '<div class="font-bold">' + escapeHtml(p.name) + (isMe ? ' (you)' : '') + '</div>' +
        '<div class="text-xs text-emerald-200">' + escapeHtml(p.characterName || '?') + '</div>' +
        '<div class="text-xs mt-1">' +
          '<span class="text-red-400">♥' + p.hearts + '</span> · ' +
          '<span class="text-yellow-300">' + p.gold + 'g</span> · ' +
          '<span class="text-cyan-300">' + p.handCount + ' cards</span>' +
        '</div>' +
        '<div class="text-xs text-white/60">Round wins: ' + p.roundsWon + '</div>';
      playersEl.appendChild(card);
    }

    // My hand
    const handEl = document.getElementById('betaMpHand');
    handEl.innerHTML = '';
    if (s.mine && s.mine.hand) {
      const myIdx = s.players.findIndex(p => p.id === myPlayerId);
      const myTurn = myIdx === s.currentTurnIdx && !s.challengeOpen &&
                     !s.players[myIdx].eliminated && !s.players[myIdx].finishedThisRound;
      for (const c of s.mine.hand) {
        const btn = document.createElement('button');
        let cls = 'card card-face flex items-center justify-center text-2xl font-bold text-black rounded transition';
        const ring = affixRingClass(c.affix);
        if (ring) cls += ' ' + ring;
        else if (c.owner === 0 || c.owner === myIdx) cls += ' ring-2 ring-emerald-400';
        if (selected.has(c.id)) cls += ' ring-4 ring-yellow-300 scale-110';
        if (myTurn) cls += ' cursor-pointer hover:scale-105';
        else cls += ' opacity-70 cursor-not-allowed';
        btn.className = cls;
        btn.textContent = c.rank;
        if (c.affix) btn.title = 'Affix: ' + c.affix;
        if (myTurn) {
          btn.addEventListener('click', () => {
            if (selected.has(c.id)) selected.delete(c.id);
            else if (selected.size < 3) selected.add(c.id);
            renderGame();
          });
        }
        handEl.appendChild(btn);
      }
    }

    // Action buttons
    const myIdx = s.players.findIndex(p => p.id === myPlayerId);
    const me = s.players[myIdx];
    const myTurn = myIdx === s.currentTurnIdx && !s.challengeOpen &&
                   me && !me.eliminated && !me.finishedThisRound;
    const myCall = s.challengeOpen && myIdx === s.challengerIdx;

    const playBtn = document.getElementById('betaMpPlayBtn');
    const passBtn = document.getElementById('betaMpPassBtn');
    const liarBtn = document.getElementById('betaMpLiarBtn');
    if (playBtn) {
      playBtn.disabled = !myTurn || selected.size === 0 || selected.size > 3;
      playBtn.textContent = myTurn ? `Play ${selected.size || ''} as ${s.targetRank || '?'}`.trim() : 'Wait...';
    }
    if (passBtn) passBtn.disabled = !myCall;
    if (liarBtn) liarBtn.disabled = !myCall;

    // Status text
    const statusEl = document.getElementById('betaMpStatus');
    if (statusEl) {
      let txt = '';
      if (me && me.eliminated) txt = 'You are eliminated. Watching the run...';
      else if (myCall) txt = 'Challenge window: PASS or call LIAR.';
      else if (myTurn) txt = `Your turn. Play 1–3 cards as ${s.targetRank}.`;
      else if (s.challengeOpen) {
        const cp = s.players[s.challengerIdx];
        txt = `Waiting for ${cp ? cp.name : '?'} to call or pass...`;
      } else {
        const cp = s.players[s.currentTurnIdx];
        txt = `Waiting for ${cp ? cp.name : '?'} to play...`;
      }
      statusEl.textContent = txt;
    }

    // Last play summary
    const lastEl = document.getElementById('betaMpLastPlay');
    if (lastEl) {
      if (s.lastPlay) {
        const lp = s.players[s.lastPlay.playerIdx];
        lastEl.textContent =
          (lp ? lp.name : '?') + ' played ' + s.lastPlay.count + ' as ' + s.lastPlay.claim + '.';
      } else {
        lastEl.textContent = '';
      }
    }

    // Log
    const logEl = document.getElementById('betaMpLog');
    if (logEl) {
      logEl.innerHTML = (s.log || []).slice(-15).map(l => '<div>' + escapeHtml(l) + '</div>').join('');
      logEl.scrollTop = logEl.scrollHeight;
    }
  }

  function renderResult() {
    showResult();
    const titleEl = document.getElementById('betaMpResultTitle');
    const textEl = document.getElementById('betaMpResultText');
    const winnerName = lastState.runWinnerId
      ? (lastState.players.find(p => p.id === lastState.runWinnerId) || {}).name
      : null;
    const iWon = winnerName && lastState.runWinnerId === myPlayerId;
    if (titleEl) titleEl.textContent = winnerName
      ? (iWon ? 'You won the run!' : `${winnerName} won the run`)
      : 'Run over';
    if (textEl) {
      textEl.innerHTML = lastState.players.map(p =>
        '<div>' + escapeHtml(p.name) + ' — Floor ' + lastState.currentFloor +
        ' (♥' + p.hearts + ', ' + p.gold + 'g)</div>'
      ).join('');
    }

    // Report progression for the local player (re-uses solo endpoint)
    reportProgression(winnerName, lastState.currentFloor, iWon);
  }

  async function reportProgression(winnerName, maxFloor, won) {
    const token = getAuthToken();
    if (!token) return;
    try {
      const me = lastState.players.find(p => p.id === myPlayerId) || {};
      await fetch('/api/beta/run-history', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token },
        body: JSON.stringify({
          characterId: me.characterId || null,
          characterName: me.characterName || null,
          result: won ? 'won' : 'lost',
          maxFloor: maxFloor || 1,
          hearts: me.hearts || 0,
          gold: me.gold || 0,
          mode: 'pvp',
        }),
      });
      await fetch('/api/beta/progression', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token },
        body: JSON.stringify({ maxFloor: maxFloor || 1, runWon: !!won }),
      });
    } catch (e) { /* ignore */ }
  }

  // ==================== UI wiring ====================
  function init() {
    // Open multiplayer entry from beta intro
    const openBtn = document.getElementById('betaMpOpenBtn');
    if (openBtn) {
      openBtn.addEventListener('click', () => {
        const beta = document.getElementById('betaTesting');
        if (beta) beta.classList.remove('hidden');
        const intro = document.getElementById('betaIntro');
        if (intro) intro.classList.add('hidden');
        const game = document.getElementById('betaGame');
        if (game) game.classList.add('hidden');
        ensureSocket();
        showEntry();
      });
    }

    const createBtn = document.getElementById('betaMpCreateBtn');
    if (createBtn) {
      createBtn.addEventListener('click', () => {
        ensureSocket();
        const nameEl = document.getElementById('betaMpName');
        socket.emit('beta:createRoom', { name: nameEl ? nameEl.value : '' });
      });
    }

    const joinBtn = document.getElementById('betaMpJoinBtn');
    if (joinBtn) {
      joinBtn.addEventListener('click', () => {
        ensureSocket();
        const codeEl = document.getElementById('betaMpJoinCode');
        const nameEl = document.getElementById('betaMpName');
        if (!codeEl || !codeEl.value.trim()) {
          alert('Enter a room code.');
          return;
        }
        socket.emit('beta:joinRoom', {
          roomId: codeEl.value.trim().toUpperCase(),
          name: nameEl ? nameEl.value : '',
        });
      });
    }

    const startBtn = document.getElementById('betaMpStartBtn');
    if (startBtn) startBtn.addEventListener('click', () => socket && socket.emit('beta:startRun'));

    const playBtn = document.getElementById('betaMpPlayBtn');
    if (playBtn) playBtn.addEventListener('click', () => {
      if (!socket || selected.size === 0) return;
      socket.emit('beta:play', { cardIds: Array.from(selected) });
      selected.clear();
    });

    const passBtn = document.getElementById('betaMpPassBtn');
    if (passBtn) passBtn.addEventListener('click', () => socket && socket.emit('beta:pass'));

    const liarBtn = document.getElementById('betaMpLiarBtn');
    if (liarBtn) liarBtn.addEventListener('click', () => socket && socket.emit('beta:liar'));

    const leaveBtn = document.getElementById('betaMpLeaveBtn');
    if (leaveBtn) leaveBtn.addEventListener('click', () => {
      if (socket) socket.emit('beta:leave');
      myRoomId = null;
      myPlayerId = null;
      lastState = null;
      hideAllMpScreens();
      const intro = document.getElementById('betaIntro');
      if (intro) intro.classList.remove('hidden');
    });

    const resultBackBtn = document.getElementById('betaMpResultBackBtn');
    if (resultBackBtn) resultBackBtn.addEventListener('click', () => {
      if (socket) socket.emit('beta:leave');
      myRoomId = null;
      myPlayerId = null;
      lastState = null;
      hideAllMpScreens();
      const intro = document.getElementById('betaIntro');
      if (intro) intro.classList.remove('hidden');
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
