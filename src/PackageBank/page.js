// Package Bank's web half. Runs in Root.html, the document every game page iframe sits in. Draws the bank panel over
// all of them, puts a button in the HUD's tool column (bottom left, above the phone and bag buttons) built like the
// game's own buttons there, and while the build shop is open adds a Bank button beside each Refund button of its
// package list and the banked packages under that list, and a Bank entry in the click menu of a yard package. Talks to the plugin through
// vuplex.postMessage("slmod|packagebank|<verb>") and is driven by window.__slpkgbank.
(function () {
  if (window.__slpkgbank) return;

  var PREFIX = 'slmod|packagebank|';
  var HUD_CLASS = 'pb-hud-btn';
  // The bank's place in the HUD's tool column when Exiled's Toolkit lays it out in columns of four.
  var DOCK_ORDER = '20';
  var MENU_CLASS = 'pb-menu-btn';
  var SHOP_STYLE_ID = 'pb-shop-style';
  var SHOP_BANK_ID = 'pb-shop-bank';
  var SHOP_BUTTON_CLASS = 'pb-bank-btn';
  var BUSY_TIMEOUT_MS = 5000;
  var TOAST_MS = 3500;
  var SVG_NS = 'http://www.w3.org/2000/svg';
  // A storage box with a lid, drawn on the 24-unit grid the game's HUD icons use.
  var BOX_PATH = 'M3 3h18v5H3V3zm2 2v1h14V5H5zM4 9h16v12H4V9zm2 2v8h12v-8H6zm3 1h6v2H9v-2z';
  // The plugin hands over the words in the game's language before every call (Texts.cs); English stands in until then.
  var ENGLISH = {
    title: 'Package Bank', tip: 'Store packages for later and take them out again', counts: '{0} in the yard, {1} in the bank',
    inYard: 'In the yard', inBankTitle: 'In the bank', inBank: 'In the bank ({0})', bank: 'Bank', takeOut: 'Take out',
    large: 'Large', medium: 'Medium', small: 'Small', story: 'Story Items', defense: 'Defenses', noLimit: 'No limit',
    spotsFree: '{0} free package spots in the yard', spotFree: '1 free package spot in the yard',
    noRefund: 'Not refundable', busy: 'Being moved or installed', notBankable: "Can't be banked",
    full: 'Bank full for this size', shelfFull: 'bank full', noSpot: 'No free package spot',
    noSpotNote: 'No free package spot in the yard. Install, refund or bank a package to take one out.',
    emptyHome: 'No packages in the yard.', emptyHomeHint: 'Packages you buy in the build shop land here.',
    emptyBank: 'The bank is empty.', emptyBankHint: 'Press Bank on a package in the yard to store it here.',
    emptyShelf: 'Nothing from the {0} shelf here.', showing: 'Showing {0} only', showAll: 'Show all',
    escClose: 'Esc to close'
  };
  // Arrows for Bank and Take out, and the warning sign of the no-spot notice, on the same 24-unit grid.
  var ARROW_IN_PATH = 'M4 11h11.2l-4.6-4.6L12 5l7 7-7 7-1.4-1.4 4.6-4.6H4z';
  var ARROW_OUT_PATH = 'M20 11H8.8l4.6-4.6L12 5l-7 7 7 7 1.4-1.4L8.8 13H20z';
  var WARN_PATH = 'M12 2 1 21h22L12 2zm1 15h-2v-2h2v2zm0-4h-2V9h2v4z';
  // A shelf with more places than this draws one bar instead of a pip per place.
  var PIP_LIMIT = 12;

  // filter: the shelf key both lists are narrowed to, or null for all of them.
  var state = { data: emptyData(), isOpen: false, isLive: false, shop: null, menu: null, filter: null };
  var ui = null;
  var toastTimer = 0;
  var isBusy = false;
  var busyTimer = 0;
  var shopVersion = 0;
  var hasReportedPageAccess = false;
  var hasReportedHudPlaced = false;
  var hasReportedShopPatched = false;

  function emptyData() {
    return { limited: true, spots: 0, sizes: [], home: [], bank: [], takeWhy: null, shop: null };
  }

  function say(key) {
    var words = window.__slpkgbankWords || ENGLISH;
    var text = words[key] || ENGLISH[key] || key;

    for (var i = 1; i < arguments.length; i++) text = text.split('{' + (i - 1) + '}').join(String(arguments[i]));

    return text;
  }

  function send(message) {
    try {
      if (window.vuplex) window.vuplex.postMessage(PREFIX + message);
    } catch (e) {}
  }

  // One press at a time: a double click would otherwise bank two packages. Every press is answered by a status line,
  // which frees the buttons; the timer frees them anyway if that answer never comes.
  function sendAction(message) {
    if (isBusy) return;

    isBusy = true;
    clearTimeout(busyTimer);
    busyTimer = setTimeout(clearBusy, BUSY_TIMEOUT_MS);
    send(message);
  }

  function clearBusy() {
    isBusy = false;
    clearTimeout(busyTimer);
  }

  function el(tag, className, text) {
    return make(document, tag, className, text);
  }

  function make(doc, tag, className, text) {
    var node = doc.createElement(tag);

    if (className) node.className = className;
    if (text != null) node.textContent = text;

    return node;
  }

  function button(doc, text, action, args, isEnabled, className) {
    var node = make(doc, 'button', className, text);
    node.setAttribute('data-act', action);

    if (args != null) node.setAttribute('data-args', String(args));
    if (!isEnabled) node.disabled = true;

    return node;
  }

  // The warm gold look the newer mod panels share: a brown plate with a gold hairline, gold section heads and quiet
  // gold-outline buttons.
  var CSS = [
    '#pb-root{position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(10,6,2,.45);font-family:"Microsoft YaHei","PingFang SC",sans-serif;color:#f0e8d8;user-select:none}',
    '#pb-root *{box-sizing:border-box}',
    '.pb-plate{position:relative;width:min(1180px,94vw);height:min(800px,88vh);display:flex;flex-direction:column;gap:16px;padding:22px 24px 16px;border-radius:4px;border:1px solid rgba(220,188,120,.28);background:radial-gradient(ellipse at 15% 0%,rgba(150,100,40,.30),transparent 55%),linear-gradient(180deg,#3a3129 0%,#2b241d 55%,#241e18 100%);box-shadow:0 30px 90px rgba(0,0,0,.7),inset 0 1px 0 rgba(255,230,180,.08)}',
    '.pb-plate::before{content:"";position:absolute;top:0;left:0;right:0;height:2px;background:linear-gradient(90deg,transparent 10%,#FFC83A 50%,transparent 90%);opacity:.6}',
    '.pb-head{display:flex;align-items:center;gap:16px}',
    '.pb-logo{width:44px;height:44px;flex:none;border-radius:4px;display:flex;align-items:center;justify-content:center;background:linear-gradient(180deg,#ffd25a,#e8a91c);box-shadow:0 0 14px rgba(255,200,58,.25)}',
    '.pb-logo svg{width:26px;height:26px;fill:#3a2a10}',
    '.pb-title{flex:1;min-width:0;font-size:26px;letter-spacing:6px;text-transform:uppercase;color:#fff;line-height:1.1}',
    '.pb-sub{font-size:13px;letter-spacing:.5px;text-transform:none;color:#a89a82;margin-top:3px}',
    '.pb-x{width:34px;height:34px;flex:none;padding:0;border-radius:4px;border:1px solid rgba(220,188,120,.20);background:rgba(255,240,210,.05);color:#cbbfa8;font:inherit;font-size:20px;line-height:32px;cursor:pointer}',
    '.pb-x:hover{border-color:#FFC83A;color:#fff}',
    '.pb-shelves{display:flex;gap:10px}',
    '.pb-shelf{flex:1;min-width:0;padding:9px 12px 10px;border-radius:4px;border:1px solid rgba(220,188,120,.11);background:rgba(70,58,44,.55);cursor:pointer;display:flex;flex-direction:column;gap:7px}',
    '.pb-shelf:hover{border-color:rgba(255,200,58,.45)}',
    '.pb-shelf.pb-on{border-color:#FFC83A;background:rgba(255,200,58,.12);box-shadow:0 0 12px rgba(255,200,58,.15)}',
    '.pb-shelf-top{display:flex;align-items:baseline;gap:8px}',
    '.pb-shelf-name{flex:1;min-width:0;font-size:13px;color:#cbbfa8;letter-spacing:1px;text-transform:uppercase;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.pb-count{font-size:15px;font-weight:700;color:#FFC83A}',
    '.pb-cap{font-weight:400;color:#7a6e5a}',
    '.pb-shelf.pb-full .pb-count{color:#ff9d86}',
    '.pb-pips{display:flex;gap:3px;height:6px}',
    '.pb-pip{flex:1;border-radius:2px;background:rgba(255,240,210,.09)}',
    '.pb-pip.pb-fill{background:#FFC83A;box-shadow:0 0 6px rgba(255,200,58,.3)}',
    '.pb-shelf.pb-full .pb-pip.pb-fill{background:#e0674f;box-shadow:none}',
    '.pb-nolimit{font-size:12px;color:#7a6e5a;height:6px;line-height:6px}',
    '.pb-cols{flex:1;min-height:0;display:flex;gap:16px}',
    '.pb-col{flex:1;min-width:0;display:flex;flex-direction:column;gap:8px}',
    '.pb-sect{display:flex;align-items:center;gap:10px;font-size:15px;font-weight:700;letter-spacing:3px;text-transform:uppercase;color:#FFC83A;text-shadow:0 0 12px rgba(255,200,58,.35);padding:2px 2px 0}',
    '.pb-sect::before{content:"";width:4px;height:16px;background:#FFC83A;box-shadow:0 0 8px rgba(255,200,58,.35)}',
    '.pb-sect-n{color:#fff;text-shadow:none}',
    '.pb-spots{margin-left:auto;font-size:13px;font-weight:400;letter-spacing:0;text-transform:none;color:#a89a82;text-shadow:none}',
    '.pb-spots.pb-bad{color:#ff9d86}',
    '.pb-list{flex:1;min-height:0;overflow-y:auto;display:flex;flex-direction:column;gap:5px;padding:8px;border-radius:4px;border:1px solid rgba(220,188,120,.20);background:linear-gradient(180deg,rgba(58,48,38,.75),rgba(44,36,28,.75))}',
    '.pb-list::-webkit-scrollbar{width:6px}',
    '.pb-list::-webkit-scrollbar-thumb{background:rgba(255,200,58,.35);border-radius:3px}',
    '.pb-grp{flex:none;display:flex;align-items:center;gap:8px;padding:6px 4px 1px;font-size:12px;letter-spacing:2px;text-transform:uppercase;color:#a89a82}',
    '.pb-grp::after{content:"";flex:1;height:1px;background:linear-gradient(90deg,rgba(220,188,120,.20),transparent)}',
    '.pb-grp-why{order:2;letter-spacing:0;text-transform:none;color:#ff9d86}',
    '.pb-grp:first-child{padding-top:0}',
    '.pb-row{flex:none;display:flex;align-items:center;gap:12px;padding:5px 10px 5px 6px;border-radius:4px;background:rgba(70,58,44,.55);border:1px solid rgba(220,188,120,.11)}',
    '.pb-row:hover{background:rgba(84,70,52,.7);border-color:rgba(255,200,58,.3)}',
    '.pb-icon{flex:none;width:40px;height:40px;display:flex;align-items:center;justify-content:center;border-radius:4px;background:rgba(20,14,8,.45);border:1px solid rgba(220,188,120,.11);overflow:hidden;color:#cbbfa8;font-weight:600;font-size:17px}',
    '.pb-icon img{width:100%;height:100%;object-fit:contain}',
    '.pb-main{flex:1;min-width:0;display:flex;flex-direction:column;gap:3px}',
    '.pb-name{font-size:15px;font-weight:600;color:#f0e8d8;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    '.pb-meta{display:flex;gap:8px;align-items:center;font-size:12px;color:#7a6e5a}',
    '.pb-why{color:#a89a82}',
    '.pb-tag{padding:1px 6px;border-radius:3px;background:rgba(255,200,58,.12);color:#e9c46a}',
    '.pb-row.pb-off .pb-icon{opacity:.55}',
    '.pb-row.pb-off .pb-name{color:#a89a82}',
    '.pb-btn{flex:none;font:inherit;font-size:13px;font-weight:600;min-width:92px;padding:7px 14px;border-radius:4px;border:1px solid rgba(255,200,58,.55);background:rgba(255,200,58,.1);color:#FFC83A;cursor:pointer;white-space:nowrap;display:inline-flex;gap:6px;align-items:center;justify-content:center}',
    '.pb-btn svg{width:14px;height:14px;fill:currentColor}',
    '.pb-btn:hover:not(:disabled){background:rgba(255,200,58,.2);border-color:#FFC83A}',
    '.pb-btn:disabled{color:#7a6e5a;border-color:rgba(220,188,120,.11);background:rgba(255,240,210,.03);cursor:default}',
    '.pb-notice{flex:none;display:flex;gap:10px;align-items:center;padding:9px 12px;border-radius:4px;border:1px solid rgba(224,103,79,.45);background:rgba(150,52,30,.18);font-size:13px;color:#f3c9b8}',
    '.pb-notice svg{width:18px;height:18px;fill:#ff9d86;flex:none}',
    '.pb-empty{margin:auto;text-align:center;color:#a89a82;font-size:14px;line-height:1.6;padding:30px}',
    '.pb-empty-head{color:#cbbfa8;font-size:16px;margin-bottom:4px}',
    '.pb-foot{display:flex;align-items:center;gap:14px;font-size:13px;color:#a89a82;border-top:1px solid rgba(220,188,120,.11);padding-top:11px}',
    '.pb-status{flex:1;min-width:0;color:#cbbfa8}',
    '.pb-filter{color:#cbbfa8}',
    '.pb-show-all{color:#FFC83A;cursor:pointer;text-decoration:underline;margin-left:8px}',
    '#pb-toast{position:fixed;left:50%;bottom:120px;transform:translateX(-50%);z-index:2147483001;pointer-events:none;display:none;max-width:640px;text-align:center;padding:10px 18px;border-radius:4px;border:1px solid rgba(255,200,58,.55);background:linear-gradient(180deg,#3a3129,#241e18);color:#f0e8d8;font-family:"Microsoft YaHei","PingFang SC",sans-serif;font-size:14px;box-shadow:0 6px 20px rgba(0,0,0,.6)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();

    var root = el('div');
    root.id = 'pb-root';
    var plate = el('div', 'pb-plate');
    var head = el('div', 'pb-head');
    var logo = el('div', 'pb-logo');
    var title = el('div', 'pb-title');
    var titleText = el('div', null, say('title'));
    var sub = el('div', 'pb-sub');
    logo.appendChild(svgIcon(document, null, BOX_PATH));
    title.appendChild(titleText);
    title.appendChild(sub);
    head.appendChild(logo);
    head.appendChild(title);
    head.appendChild(button(document, '\u00d7', 'close', null, true, 'pb-x'));

    var shelves = el('div', 'pb-shelves');
    var cols = el('div', 'pb-cols');
    var yard = column(cols);
    var bank = column(cols);
    var spots = el('span', 'pb-spots');
    bank.head.appendChild(spots);

    var foot = el('div', 'pb-foot');
    var status = el('div', 'pb-status');
    var filter = el('div', 'pb-filter');
    var keys = el('div');
    foot.appendChild(status);
    foot.appendChild(filter);
    foot.appendChild(keys);

    plate.appendChild(head);
    plate.appendChild(shelves);
    plate.appendChild(cols);
    plate.appendChild(foot);
    root.appendChild(plate);

    var toast = el('div');
    toast.id = 'pb-toast';

    document.body.appendChild(root);
    document.body.appendChild(toast);

    // Drawn at the player's UI size (Exiled's Toolkit). The full-screen mask always fills the screen, so the panel on
    // it is what must fit; the short notice is never shrunk.
    uiSize(root, { fit: plate });
    uiSize(toast, { fit: false });

    root.addEventListener('click', onClick);
    // Clicks inside the plate must not reach the game pages underneath; the mask itself closes nothing on purpose.
    root.addEventListener('mousedown', function (e) { e.stopPropagation(); });
    root.addEventListener('error', swapBrokenIcon, true);

    ui = {
      root: root, title: titleText, sub: sub, shelves: shelves, spots: spots, homeTitle: yard.title, homeCount: yard.count,
      bankTitle: bank.title, bankCount: bank.count, homeList: yard.list, bankList: bank.list, status: status,
      filter: filter, keys: keys, toast: toast
    };
  }

  // A column's gold heading (its title and count) over its list.
  function column(cols) {
    var col = el('div', 'pb-col');
    var head = el('div', 'pb-sect');
    var title = el('span');
    var count = el('span', 'pb-sect-n');
    var list = el('div', 'pb-list');
    head.appendChild(title);
    head.appendChild(count);
    col.appendChild(head);
    col.appendChild(list);
    cols.appendChild(col);

    return { head: head, title: title, count: count, list: list };
  }

  // A broken icon path swaps to the first-letter placeholder; error events do not bubble, so this listens in capture.
  function swapBrokenIcon(e) {
    var img = e.target;

    if (img && img.tagName === 'IMG' && img.parentNode) img.parentNode.textContent = img.getAttribute('data-letter') || '?';
  }

  // Hands a panel to Exiled's Toolkit to be drawn at the player's UI size. Before the Toolkit's script has loaded it
  // waits in a list the Toolkit reads; without the Toolkit nothing happens.
  function uiSize(node, options) {
    var toolkit = window.ExiledUi;

    if (toolkit) {
      toolkit.size(node, options);
      return;
    }

    var waiting = window.__exiledUiWait = window.__exiledUiWait || [];
    waiting.push([node, options]);
    if (waiting.length > 32) waiting.shift();
  }

  // The style outlives a lost panel root, so a rebuild must not stack another copy of it.
  function addStyle() {
    if (document.getElementById('pb-style')) return;

    var style = el('style');
    style.id = 'pb-style';
    style.textContent = CSS;
    document.head.appendChild(style);
  }

  function onClick(e) {
    var target = e.target.closest('[data-act]');

    if (!target) return;

    e.stopPropagation();

    if (target.disabled) return;

    var act = target.getAttribute('data-act');
    var args = target.getAttribute('data-args');

    // The shelf filter lives in the page alone; the plugin never hears of it.
    if (act === 'filter' || act === 'showall') {
      state.filter = act === 'filter' && state.filter !== args ? args : null;
      render();

      return;
    }

    // Closing the panel gets no status line back, so it must not hold the buttons busy.
    if (act === 'close') {
      send(act);

      return;
    }

    sendAction(args ? act + '|' + args : act);
  }

  function icon(doc, item, className) {
    var box = make(doc, 'div', className);
    var letter = String(item.name || '?').charAt(0).toUpperCase();

    if (item.icon) {
      var img = make(doc, 'img');
      img.setAttribute('data-letter', letter);
      img.src = fromRoot(item.icon);
      box.appendChild(img);
    } else {
      box.textContent = letter;
    }

    return box;
  }

  // Icon paths are relative to the WebUI folder, where Root.html sits. A game page iframe (the shop is in
  // UI/BuildShop/) would read them from its own folder, so they are made whole against the root page first.
  function fromRoot(path) {
    var href = String(location.href || '').split(/[?#]/)[0];

    return href.slice(0, href.lastIndexOf('/') + 1) + path;
  }

  // Every card, row, heading and note the last render drew, by a key made of everything it was drawn from, so a new
  // view reuses what did not change and builds only what did. New words (a switch of the game's language) drop them all.
  var drawn = { words: null, nodes: {} };
  var drawing = null;

  function keyed(key, build) {
    var node = drawn.nodes[key] || build();

    drawing[key] = node;

    return node;
  }

  // Puts exactly these nodes in the parent, in this order, moving only the ones out of place. The list is never
  // emptied on the way, so its scroll position stays where the player left it.
  function fill(parent, nodes) {
    var kept = new Set(nodes);

    for (var child = parent.firstChild; child; ) {
      var next = child.nextSibling;

      if (!kept.has(child)) parent.removeChild(child);

      child = next;
    }

    var at = parent.firstChild;

    nodes.forEach(function (node) {
      if (node === at) at = at.nextSibling;
      else parent.insertBefore(node, at);
    });
  }

  // Writing the same text again still swaps the text node, so it is left alone when it is already right.
  function setText(node, text) {
    if (node.textContent !== text) node.textContent = text;
  }

  function render() {
    var data = state.data;
    var words = window.__slpkgbankWords || ENGLISH;

    if (state.filter && !sizeOf(state.filter)) state.filter = null;

    if (drawn.words !== words) drawn = { words: words, nodes: {} };

    drawing = {};
    setText(ui.title, say('title'));
    setText(ui.sub, say('counts', data.home.length, data.bank.length));
    renderShelves(data);
    setText(ui.homeTitle, say('inYard'));
    setText(ui.homeCount, String(data.home.length));
    setText(ui.bankTitle, say('inBankTitle'));
    setText(ui.bankCount, String(data.bank.length));
    setText(ui.spots, data.spots === 1 ? say('spotFree') : say('spotsFree', data.spots));
    ui.spots.className = 'pb-spots' + (data.spots > 0 ? '' : ' pb-bad');
    renderList('h', ui.homeList, data.home, homeRow, homeNote, null, 'emptyHome', 'emptyHomeHint');
    renderList('b', ui.bankList, data.bank, bankRow, null, data.takeWhy && data.bank.length > 0 ? data.takeWhy : null, 'emptyBank', 'emptyBankHint');
    renderFilter();
    setText(ui.keys, say('escClose'));
    drawn.nodes = drawing;
    drawing = null;
  }

  function sizeOf(key) {
    return state.data.sizes.filter(function (size) { return size.key === key; })[0] || null;
  }

  function isFull(size) {
    return size != null && size.cap != null && size.used >= size.cap;
  }

  // One card per shelf with a pip for each banked package. A press shows only that shelf in both lists; a second
  // press shows everything again.
  function renderShelves(data) {
    fill(ui.shelves, data.sizes.map(function (size) {
      return keyed('s|' + JSON.stringify(size) + '|' + (state.filter === size.key), function () { return shelfCard(size); });
    }));
  }

  function shelfCard(size) {
    var card = el('div', 'pb-shelf' + (state.filter === size.key ? ' pb-on' : '') + (isFull(size) ? ' pb-full' : ''));
    var top = el('div', 'pb-shelf-top');
    var count = el('span', 'pb-count', String(size.used));
    card.setAttribute('data-act', 'filter');
    card.setAttribute('data-args', size.key);
    top.appendChild(el('span', 'pb-shelf-name', say(size.key)));

    if (size.cap != null) count.appendChild(el('span', 'pb-cap', ' / ' + size.cap));

    top.appendChild(count);
    card.appendChild(top);
    card.appendChild(meter(size));

    return card;
  }

  function meter(size) {
    if (size.cap == null) return el('div', 'pb-nolimit', say('noLimit'));

    var pips = el('div', 'pb-pips');

    if (size.cap > PIP_LIMIT) {
      pips.appendChild(pip(true, size.used));
      pips.appendChild(pip(false, Math.max(size.cap - size.used, 0)));

      return pips;
    }

    for (var i = 0; i < size.cap; i++) pips.appendChild(pip(i < size.used, 1));

    return pips;
  }

  function pip(isFilled, flex) {
    var node = el('span', 'pb-pip' + (isFilled ? ' pb-fill' : ''));
    node.style.flex = String(flex);

    return node;
  }

  function renderFilter() {
    ui.filter.textContent = '';

    if (!state.filter) return;

    var all = el('span', 'pb-show-all', say('showAll'));
    all.setAttribute('data-act', 'showall');
    ui.filter.appendChild(el('span', null, say('showing', say(state.filter))));
    ui.filter.appendChild(all);
  }

  // Both lists are grouped by shelf, in the shelf cards' order, so no row needs its own size badge.
  // A row's key is the list it is in, its item as the view sent it, and what else its look depends on (rowKey).
  function renderList(prefix, list, items, row, groupNote, noticeWhy, emptyKey, hintKey) {
    if (items.length === 0) {
      fill(list, [keyed(prefix + '|empty', function () { return empty(say(emptyKey), say(hintKey)); })]);

      return;
    }

    var nodes = [];

    if (noticeWhy) nodes.push(keyed(prefix + '|notice|' + noticeWhy, function () { return bankNotice(noticeWhy); }));

    groupKeys(items).forEach(function (key) {
      if (state.filter && key !== state.filter) return;

      var group = items.filter(function (item) { return item.size === key; });
      var note = groupNote ? groupNote(key) : '';

      nodes.push(keyed(prefix + '|group|' + key + '|' + note, function () { return groupTitle(key, note); }));
      group.forEach(function (item) {
        nodes.push(keyed(prefix + '|row|' + JSON.stringify(item) + '|' + rowKey(item), function () { return row(item); }));
      });
    });

    if (nodes.length === (noticeWhy ? 1 : 0)) nodes.push(keyed(prefix + '|none|' + state.filter, function () { return empty(say('emptyShelf', say(state.filter)), null); }));

    fill(list, nodes);
  }

  // The rest of what a row is drawn from: a yard row hides its reason under a full shelf's note, a bank row is greyed
  // with no free spot.
  function rowKey(item) {
    return isFull(sizeOf(item.size)) + '|' + state.data.takeWhy;
  }

  function groupTitle(key, note) {
    var title = el('div', 'pb-grp', key ? say(key) : '');

    if (note) title.appendChild(el('span', 'pb-grp-why', note));

    return title;
  }

  // The view's shelves in its order, then any shelf a package names that the view does not list, so nothing is hidden.
  function groupKeys(items) {
    var keys = state.data.sizes.map(function (size) { return size.key; });

    items.forEach(function (item) {
      if (keys.indexOf(item.size) < 0) keys.push(item.size);
    });

    return keys.filter(function (key) { return items.some(function (item) { return item.size === key; }); });
  }

  function empty(head, hint) {
    var node = el('div', 'pb-empty');
    node.appendChild(el('div', 'pb-empty-head', head));

    if (hint) node.appendChild(el('div', null, hint));

    return node;
  }

  // A full shelf greys every yard package on it, so its group says so once instead of under every button.
  function homeNote(key) {
    return isFull(sizeOf(key)) ? say('shelfFull') : '';
  }

  // With no free spot every Take out is greyed for the same reason, so it is said once, above the list.
  function bankNotice(why) {
    var node = el('div', 'pb-notice');
    node.appendChild(svgIcon(document, null, WARN_PATH));
    node.appendChild(el('span', null, say(why === 'noSpot' ? 'noSpotNote' : why)));

    return node;
  }

  // The web view draws no hover tips, so a package refused for a reason of its own says why under its name.
  function homeRow(item) {
    var isGroupReason = item.why === 'full' && isFull(sizeOf(item.size));
    var node = button(document, say('bank'), 'store', item.id, !item.why, 'pb-btn');
    node.appendChild(svgIcon(document, null, ARROW_IN_PATH));

    return packageRow(item, node, isGroupReason ? null : item.why, !!item.why);
  }

  function bankRow(item) {
    var why = state.data.takeWhy;
    var node = make(document, 'button', 'pb-btn');
    node.appendChild(svgIcon(document, null, ARROW_OUT_PATH));
    node.appendChild(el('span', null, say('takeOut')));
    node.setAttribute('data-act', 'takeout');
    node.setAttribute('data-args', String(item.slot));

    if (why) node.disabled = true;

    return packageRow(item, node, null, !!why);
  }

  function packageRow(item, action, why, isOff) {
    var node = el('div', 'pb-row' + (isOff ? ' pb-off' : ''));
    var main = el('div', 'pb-main');
    var meta = el('div', 'pb-meta');
    main.appendChild(el('div', 'pb-name', item.name));

    if (why) meta.appendChild(el('span', 'pb-why', say(why)));
    if (item.looted) meta.appendChild(el('span', 'pb-tag', say('noRefund')));
    if (meta.children.length > 0) main.appendChild(meta);

    node.appendChild(icon(document, item, 'pb-icon'));
    node.appendChild(main);
    node.appendChild(action);

    return node;
  }

  function open(data) {
    build();
    state.data = data || state.data;
    state.isOpen = true;
    state.filter = null;
    ui.status.textContent = '';
    ui.toast.style.display = 'none';
    ui.root.style.display = 'flex';
    render();
  }

  function setData(data) {
    state.data = data || state.data;

    if (state.isOpen) render();
  }

  function close() {
    state.isOpen = false;

    if (ui) ui.root.style.display = 'none';
  }

  function status(text) {
    build();
    clearBusy();

    if (state.isOpen) {
      ui.status.textContent = text;

      return;
    }

    // An empty status only clears the line; as a toast it would be an empty box.
    if (!text) {
      ui.toast.style.display = 'none';

      return;
    }

    ui.toast.textContent = text;
    ui.toast.style.display = 'block';
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { ui.toast.style.display = 'none'; }, TOAST_MS);
  }

  // Whether the player is at home in a loaded game: the HUD button is only shown then.
  function live(isLive) {
    state.isLive = !!isLive;
    patchPagesSafely();
  }

  // The panel's view while the build shop or the click menu is open, null otherwise; the shop's buttons and bank rows,
  // and the menu's Bank entry, are drawn from it.
  function shop(data) {
    state.shop = data && data.shop ? data : null;
    state.menu = data && data.menu ? data.menu : null;
    shopVersion++;
    patchPagesSafely();
  }

  // The game pages the bank adds to. The HUD is CoreUI0 in the countdown before the disaster and CoreUI1 after it; the
  // two draw their tool buttons differently.
  var PAGES = [
    { pattern: /CoreUI0\.html/, patch: function (doc) { placeHudButton(doc, prepHudButton); } },
    { pattern: /CoreUI1\.html/, patch: function (doc) { placeHudButton(doc, hudButton); } },
    { pattern: /BuildShop\.html/, patch: patchShop },
    { pattern: /SelectMenu\.html/, patch: patchMenu }
  ];

  // The root's iframes, kept up to date by the browser, so a check never has to search the root document for them.
  var frames = document.getElementsByTagName('iframe');

  // Which of the bank's pages an iframe shows, worked out again only when its address changes.
  function pageFor(frame) {
    var src = frame.src || '';

    if (frame.pbSrc !== src) {
      frame.pbSrc = src;
      frame.pbPage = PAGES.filter(function (page) { return page.pattern.test(src); })[0] || null;
    }

    return frame.pbPage;
  }

  // The game's pages are same-origin iframes, so their documents can be reached from here; polled because the game
  // creates and drops those iframes whenever it likes, the shop redraws its list on every Buy and Refund, and the click
  // menu redraws its entries every time it opens. Each page's patch returns at once when what it added is still there
  // and nothing it is drawn from changed.
  function patchPages() {
    for (var i = 0; i < frames.length; i++) {
      var frame = frames.item(i);
      var src = frame.src || '';
      var page = pageFor(frame);

      if (!page) continue;

      var doc = null;

      try {
        doc = frame.contentDocument;
      } catch (e) {}

      if (!doc) {
        if (!hasReportedPageAccess) send('diag|cannot reach a game page document (root ' + location.protocol + ', iframe ' + src + ')');
        hasReportedPageAccess = true;
        continue;
      }

      page.patch(doc);
    }
  }

  function patchPagesSafely() {
    try {
      patchPages();
    } catch (e) {}
  }

  // First in the tool column: the column is anchored at the bottom, so the first child sits on top and the game's own
  // buttons do not move. The column hides itself in build mode, and the button with it.
  // The button placed is kept on the page's document, so a check with it still in the column searches nothing.
  function placeHudButton(doc, build) {
    var existing = doc.pbHudButton && doc.pbHudButton.isConnected ? doc.pbHudButton : null;

    if (!state.isLive) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.parentNode && existing.parentNode.classList.contains('vertical-tools')) return;

    var column = doc.querySelector('.vertical-tools');

    if (!column) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.parentNode === column) return;
    if (existing) existing.remove();

    var node = build(doc);

    node.setAttribute('data-etk-dock', DOCK_ORDER);
    column.insertBefore(node, column.firstChild);
    doc.pbHudButton = node;

    if (!hasReportedHudPlaced) send('diag|HUD button placed in the tool column');
    hasReportedHudPlaced = true;
  }

  // The same markup as CoreUI1's bag button: its classes, an svg icon, and the hover tip its stylesheet shows. Every
  // game page watches its body for new buttons and tells the game where they are, so a click on this one reaches it.
  function hudButton(doc) {
    var node = toggleButton(doc, 'ui-btn icon-sq interactive-btn has-entry-tip ' + HUD_CLASS);

    node.appendChild(entryTip(doc));

    return node;
  }

  // CoreUI0's tool buttons sit in a wrapper with their hover tip beside them, to the right.
  function prepHudButton(doc) {
    var wrapper = make(doc, 'div', 'tooltip-wrapper ' + HUD_CLASS);
    var tip = make(doc, 'div', 'btn-tooltip btn-tooltip--right');

    tip.appendChild(make(doc, 'div', 'tip-name', say('title')));
    tip.appendChild(make(doc, 'div', 'tip-desc', say('tip')));
    wrapper.appendChild(toggleButton(doc, 'ui-btn icon-sq interactive-btn'));
    wrapper.appendChild(tip);

    return wrapper;
  }

  function toggleButton(doc, className) {
    var node = make(doc, 'button', className);

    node.setAttribute('data-interactive', '');
    node.appendChild(boxIcon(doc, 'icon-svg'));
    node.addEventListener('click', function (e) {
      e.stopPropagation();
      send('toggle');
    });

    return node;
  }

  // Shown while the game's click menu is open on a yard package the bank lists, last in its main column. Greyed with
  // the reason in the menu's own hover tip when the bank would refuse it. The press names no package: the plugin banks
  // whatever the menu is open on at that moment.
  // With the menu not on a yard package (most of the time) this searches nothing: the entry placed is kept on the
  // page's document.
  function patchMenu(doc) {
    watchMenu(doc);

    var why = state.menu ? state.menu.why || '' : null;
    var existing = doc.pbMenuEntry && doc.pbMenuEntry.isConnected ? doc.pbMenuEntry : null;

    if (why === null) {
      if (existing) existing.remove();

      return;
    }

    var container = doc.getElementById('menu-container');
    var hasGameEntries = container != null && container.querySelectorAll('.menu-btn').length > (existing ? 1 : 0);

    if (!hasGameEntries) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.getAttribute('data-why') === why) return;
    if (existing) existing.remove();

    doc.pbMenuEntry = menuEntry(doc, why);
    (container.querySelector('.menu-col') || container).appendChild(doc.pbMenuEntry);
  }

  // The menu draws its entries a moment after it opens; adding the Bank entry the moment they appear, rather than at
  // the next poll, keeps it from showing up late, after the player has already moved to click.
  function watchMenu(doc) {
    if (doc.pbMenuWatched || !doc.body || typeof MutationObserver === 'undefined') return;

    doc.pbMenuWatched = true;
    new MutationObserver(function () {
      try {
        patchMenu(doc);
      } catch (e) {}
    }).observe(doc.body, { childList: true, subtree: true });
  }

  function menuEntry(doc, why) {
    var node = make(doc, 'div', 'menu-btn show ' + MENU_CLASS + (why ? ' locked' : ''));
    var svg = boxIcon(doc, 'menu-btn-icon');

    node.setAttribute('data-interactive', '');
    node.setAttribute('data-why', why);
    svg.setAttribute('fill', 'currentColor');
    node.appendChild(svg);
    node.appendChild(make(doc, 'span', 'menu-btn-text', say('bank')));

    if (why) node.appendChild(make(doc, 'div', 'menu-btn-tooltip', say(why)));

    node.addEventListener('click', function (e) {
      e.stopPropagation();

      if (!why) sendAction('menubank');
    });

    return node;
  }

  function boxIcon(doc, className) {
    return svgIcon(doc, className, BOX_PATH);
  }

  function svgIcon(doc, className, shape) {
    var svg = doc.createElementNS(SVG_NS, 'svg');
    var path = doc.createElementNS(SVG_NS, 'path');

    if (className) svg.setAttribute('class', className);

    svg.setAttribute('viewBox', '0 0 24 24');
    path.setAttribute('d', shape);
    svg.appendChild(path);

    return svg;
  }

  function entryTip(doc) {
    var tip = doc.createElement('div');
    var title = doc.createElement('div');
    var body = doc.createElement('div');

    tip.className = 'entry-tip';
    title.className = 'entry-tip-title';
    title.textContent = say('title');
    body.className = 'entry-tip-body';
    body.textContent = say('tip');
    tip.appendChild(title);
    tip.appendChild(body);

    return tip;
  }

  // Buttons in the shop's own look (its Refund button), plus the bank rows, which copy its package rows.
  var SHOP_CSS = [
    '.pb-bank-btn{margin-left:6px}',
    '.pb-bank-btn:disabled{opacity:.35;cursor:default}',
    '.pb-bank-btn:disabled:hover{color:inherit;border-color:inherit}',
    '#pb-shop-bank{margin-top:10px;padding-top:8px;border-top:1px solid rgba(251,176,52,.35)}',
    '.pb-shop-title{font-size:13px;color:#fbb034;margin:0 0 6px 2px}',
    '.pb-shop-row{display:flex;align-items:center;background-color:rgba(48,48,51,.35);padding:8px 15px;margin-bottom:4px;border-radius:4px;border:1px dashed rgba(251,176,52,.3)}',
    '.pb-shop-icon{width:36px;height:36px;margin-right:15px;display:flex;align-items:center;justify-content:center;color:#aaa}',
    '.pb-shop-icon img{width:100%;height:100%;object-fit:contain}',
    '.pb-shop-name{flex:1;font-size:14px;color:#c8c8c8}',
    '.pb-shop-why{font-size:11px;color:#999;margin-right:8px}'
  ].join('\n');

  // While the shop's view is here, every game row the bank can take gets a Bank button beside its Refund button, and
  // the banked packages are listed under the rows. Rows are matched by their place in the list, which is the index the
  // shop itself refunds by. Without the view everything the bank added comes back out.
  //
  // What was done is kept on the page's document (doc.pbShop): the view's version, the rows as they were, the button put
  // in each, and the bank section. A check that finds all of it unchanged returns without searching the page.
  function patchShop(doc) {
    var data = state.shop;

    if (!data || !doc.body) {
      unpatchShop(doc);

      return;
    }

    if (isShopPatched(doc)) return;

    var rows = doc.querySelectorAll('.package-item');
    var section = doc.getElementById(SHOP_BANK_ID);
    var buttons = [];

    addShopStyle(doc);
    rows.forEach(function (row, index) { buttons.push(patchShopRow(doc, row, rowFor(data, index))); });
    doc.pbShopRows = doc.pbShopRows || doc.getElementsByClassName('package-item');
    doc.pbShop = { version: shopVersion, rows: Array.prototype.slice.call(rows), buttons: buttons, section: placeShopBank(doc, section, rows) };

    if (!hasReportedShopPatched) send('diag|bank added to the build shop');
    hasReportedShopPatched = true;
  }

  // Nothing to take out of a shop page the bank never added to.
  function unpatchShop(doc) {
    if (!doc.pbShop) return;

    doc.pbShop = null;
    removeAll(doc.querySelectorAll('.' + SHOP_BUTTON_CLASS));

    var section = doc.getElementById(SHOP_BANK_ID);

    if (section) section.remove();
  }

  // The same view, the same rows in the same order, each button still in its row, and the bank section still last
  // under the rows.
  function isShopPatched(doc) {
    var done = doc.pbShop;
    var rows = doc.pbShopRows;

    if (!done || done.version !== shopVersion || !done.section || !done.section.isConnected) return false;
    if (rows.length !== done.rows.length) return false;

    for (var i = 0; i < rows.length; i++) {
      if (rows.item(i) !== done.rows[i]) return false;
      if (done.buttons[i] && done.buttons[i].parentNode !== done.rows[i]) return false;
    }

    var list = done.section.parentNode;

    return (rows.length === 0 || list === done.rows[0].parentNode) && list.children[list.children.length - 1] === done.section;
  }

  function rowFor(data, index) {
    return data.shop.filter(function (row) { return row.index === index; })[0] || null;
  }

  function patchShopRow(doc, row, entry) {
    var existing = row.querySelector('.' + SHOP_BUTTON_CLASS);

    if (!entry) {
      if (existing) existing.remove();

      return null;
    }

    var node = existing || shopBankButton(doc, row);
    node.textContent = say('bank');
    node.setAttribute('data-index', String(entry.index));
    node.disabled = !!entry.why;

    return node;
  }

  // Placed straight after the Refund button, the row's last child.
  function shopBankButton(doc, row) {
    var node = make(doc, 'button', 'return-btn ' + SHOP_BUTTON_CLASS);

    row.appendChild(node);
    node.addEventListener('mousedown', function (e) { e.stopPropagation(); });
    node.addEventListener('click', function (e) {
      e.stopPropagation();

      if (!node.disabled) sendAction('bank|' + node.getAttribute('data-index'));
    });

    return node;
  }

  // Redrawn only when the view changed, so the game's hover state on it is not reset every poll; kept after the rows,
  // which the shop adds to and takes from at the end of its list.
  function placeShopBank(doc, section, rows) {
    var list = rows.length > 0 ? rows[0].parentNode : doc.querySelector('.left-content-wrapper');

    if (!list) return null;

    if (section && section.getAttribute('data-version') === String(shopVersion)) {
      if (section.parentNode !== list || list.children[list.children.length - 1] !== section) list.appendChild(section);

      return section;
    }

    if (section) section.remove();

    return list.appendChild(shopBank(doc));
  }

  function shopBank(doc) {
    var data = state.shop;
    var section = make(doc, 'div');

    section.id = SHOP_BANK_ID;
    section.setAttribute('data-version', String(shopVersion));
    section.appendChild(make(doc, 'div', 'pb-shop-title', say('inBank', data.bank.length)));
    data.bank.forEach(function (item) { section.appendChild(shopBankRow(doc, item, data.takeWhy)); });
    section.addEventListener('mousedown', function (e) { e.stopPropagation(); });
    section.addEventListener('click', onShopClick);
    section.addEventListener('error', swapBrokenIcon, true);

    return section;
  }

  function shopBankRow(doc, item, why) {
    var row = make(doc, 'div', 'pb-shop-row');

    row.appendChild(icon(doc, item, 'pb-shop-icon'));
    row.appendChild(make(doc, 'div', 'pb-shop-name', item.name));

    if (why) row.appendChild(make(doc, 'span', 'pb-shop-why', say(why)));

    row.appendChild(button(doc, say('takeOut'), 'takeout', item.slot, !why, 'return-btn'));

    return row;
  }

  function onShopClick(e) {
    var target = e.target.closest('[data-act]');

    if (!target) return;

    e.stopPropagation();

    if (!target.disabled) sendAction(target.getAttribute('data-act') + '|' + target.getAttribute('data-args'));
  }

  function addShopStyle(doc) {
    if (doc.getElementById(SHOP_STYLE_ID)) return;

    var style = doc.createElement('style');
    style.id = SHOP_STYLE_ID;
    style.textContent = SHOP_CSS;
    (doc.head || doc.body).appendChild(style);
  }

  function removeAll(nodes) {
    nodes.forEach(function (node) { node.remove(); });
  }

  // A game page in a new iframe is patched, and the click menu watched, as soon as the iframe appears and again when
  // it loads, not at the next poll; the menu's Bank entry depends on that watcher being in place before the game draws
  // the menu's buttons.
  function watchForFrames() {
    if (typeof MutationObserver === 'undefined') return;

    // Any change in the root (the bank's own panel included) lands here, so the check walks the kept list of iframes
    // and searches nothing.
    new MutationObserver(function () {
      var isNew = false;

      for (var i = 0; i < frames.length; i++) {
        var frame = frames.item(i);

        if (frame.pbLoadWatched) continue;

        frame.pbLoadWatched = true;
        frame.addEventListener('load', patchPagesSafely);
        isNew = true;
      }

      if (isNew) patchPagesSafely();
    }).observe(document.body, { childList: true, subtree: true });
  }

  watchForFrames();
  setInterval(patchPagesSafely, 400);

  window.__slpkgbank = { open: open, setData: setData, close: close, status: status, live: live, shop: shop };
  send('ready');
})();
