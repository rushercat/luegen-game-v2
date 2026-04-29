// beta-topnav.js — Phase 1 in-round top app bar wiring + Legend hub modal.
// Moved out of an inline <script> in index.html to avoid any chance of HTML
// parsing weirdness (the inline copy was leaking onto the page in some
// browsers due to a stale build / parse mismatch).

(function () {
  // Click handler: any element with [data-trigger] forwards a click to the
  // (possibly hidden) catalog button with that ID. Lets the new top app bar
  // and the Legend hub fire the existing beta.js handlers without duplication.
  document.addEventListener('click', function (e) {
    // 1) Open the Legend hub modal when the topnav "Legend" item is clicked.
    var topnav = e.target.closest('[data-topnav]');
    if (topnav) {
      var which = topnav.getAttribute('data-topnav');
      if (which === 'legend') {
        e.preventDefault();
        var hub = document.getElementById('betaLegendHub');
        if (hub) hub.classList.remove('hidden');
        return;
      }
    }
    // 2) Forward [data-trigger] clicks to the corresponding hidden catalog button.
    var trigger = e.target.closest('[data-trigger]');
    if (trigger) {
      var id = trigger.getAttribute('data-trigger');
      var tgt = id && document.getElementById(id);
      if (tgt) {
        e.preventDefault();
        tgt.click();
        // If we're inside the Legend hub, dismiss it now that a category is opening.
        var hub2 = document.getElementById('betaLegendHub');
        if (hub2 && !hub2.classList.contains('hidden') && hub2.contains(trigger)) {
          hub2.classList.add('hidden');
        }
      }
    }
  });
  // Close button + backdrop click for the Legend hub.
  var hub = document.getElementById('betaLegendHub');
  var closeBtn = document.getElementById('betaLegendHubClose');
  function closeHub() { if (hub) hub.classList.add('hidden'); }
  if (closeBtn) closeBtn.addEventListener('click', closeHub);
  if (hub) hub.addEventListener('click', function (e) {
    if (e.target === hub) closeHub();
  });
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && hub && !hub.classList.contains('hidden')) closeHub();
  });
})();
