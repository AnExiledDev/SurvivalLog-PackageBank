// Package Bank's web half. Runs in Root.html, the document every game page iframe sits in. Draws the bank panel over
// all of them, puts a button in the HUD's tool column (bottom left, above the phone and bag buttons) built like the
// game's own buttons there, and while the build shop is open adds a Bank button beside each Refund button of its
// package list and the banked packages under that list. Talks to the plugin through
// vuplex.postMessage("slmod|packagebank|<verb>") and is driven by window.__slpkgbank.
(function () {
  if (window.__slpkgbank) return;

  var PREFIX = 'slmod|packagebank|';
  var HUD_CLASS = 'pb-hud-btn';
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
    title: 'Package Bank', tip: 'Store packages for later and take them out again', atHome: 'At home ({0})',
    inBank: 'In the bank ({0})', bank: 'Bank', takeOut: 'Take out', large: 'Large', medium: 'Medium', small: 'Small',
    story: 'Story Items', used: '{0}: {1}/{2}', usedFree: '{0}: {1}', spots: 'Free package spots: {0}',
    noRefund: 'Not refundable', busy: 'Being moved or installed', notBankable: "Can't be banked",
    full: 'Bank full for this size', noSpot: 'No free package spot', emptyHome: 'No packages in the yard.',
    emptyBank: 'The bank is empty.', escClose: 'Esc to close'
  };

  var state = { data: emptyData(), isOpen: false, isLive: false, shop: null };
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

  var CSS = [
    '#pb-root{position:fixed;inset:0;z-index:2147483000;display:none;align-items:center;justify-content:center;background:rgba(0,0,0,.4);font-family:"Microsoft YaHei","PingFang SC",sans-serif;color:#eee;user-select:none}',
    '#pb-root *{box-sizing:border-box}',
    '.pb-plate{width:min(1040px,92vw);height:min(800px,86vh);display:flex;flex-direction:column;gap:12px;padding:18px 20px;border-radius:10px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(30,30,33,.92),rgba(18,18,20,.94));box-shadow:0 10px 40px rgba(0,0,0,.6)}',
    '.pb-head{display:flex;align-items:center;gap:14px}',
    '.pb-title{font-size:18px;font-weight:600;letter-spacing:1px;text-shadow:0 2px 6px rgba(0,0,0,.6)}',
    '.pb-sizes{flex:1;display:flex;align-items:center;gap:8px;flex-wrap:wrap;font-size:13px}',
    '.pb-chip{padding:3px 9px;border-radius:7px;background:rgba(10,10,12,.72);border:1px solid rgba(255,255,255,.14);white-space:nowrap}',
    '.pb-chip.pb-at-cap{border-color:rgba(229,57,53,.7);color:#ff8a80}',
    '.pb-spots{font-size:13px;color:rgba(238,238,238,.7);white-space:nowrap}',
    '.pb-btn.pb-x{width:30px;height:30px;padding:0;font-size:18px;line-height:28px}',
    '.pb-cols{flex:1;min-height:0;display:flex;gap:12px}',
    '.pb-col{flex:1;min-width:0;display:flex;flex-direction:column;gap:6px}',
    '.pb-col-title{font-size:14px;font-weight:600;color:#EBA308}',
    '.pb-list{flex:1;min-height:0;overflow-y:auto;display:flex;flex-direction:column;gap:6px;padding:6px;background:rgba(10,10,12,.72);border:1px solid rgba(255,255,255,.14);border-radius:8px}',
    '.pb-row{display:flex;align-items:center;gap:10px;padding:7px 9px;border-radius:7px;background:rgba(255,255,255,.04)}',
    '.pb-row:hover{background:rgba(255,255,255,.06)}',
    '.pb-main{flex:1;min-width:0;display:flex;flex-direction:column;gap:4px}',
    '.pb-name{font-size:14px;font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}',
    '.pb-tags{display:flex;gap:6px;flex-wrap:wrap}',
    '.pb-badge{font-size:11px;padding:2px 7px;border-radius:5px;background:rgba(255,255,255,.1);color:rgba(238,238,238,.8)}',
    '.pb-badge-story{background:rgba(124,179,66,.24);color:#aed581}',
    '.pb-badge-looted{background:rgba(255,193,7,.18);color:#EBA308}',
    '.pb-acts{flex:none;display:flex;flex-direction:column;align-items:flex-end;gap:4px;max-width:180px}',
    '.pb-why{font-size:12px;color:rgba(238,238,238,.55);text-align:right}',
    '.pb-icon{flex:none;width:40px;height:40px;display:flex;align-items:center;justify-content:center;border-radius:6px;background:rgba(255,255,255,.05);overflow:hidden;color:rgba(238,238,238,.7);font-weight:600;font-size:17px}',
    '.pb-icon img{width:100%;height:100%;object-fit:contain}',
    '.pb-empty{padding:20px;color:rgba(238,238,238,.55);font-size:14px;text-align:center}',
    '.pb-btn{font:inherit;font-size:13px;color:#dde2f0;padding:6px 11px;border-radius:7px;border:1px solid rgba(255,255,255,.14);background:linear-gradient(180deg,rgba(58,58,60,.9),rgba(38,38,40,.92));cursor:pointer;white-space:nowrap}',
    '.pb-btn:hover:not(:disabled){background:linear-gradient(180deg,rgba(72,66,48,.92),rgba(50,46,32,.95));border-color:rgba(255,193,7,.75)}',
    '.pb-btn:disabled{opacity:.4;cursor:default}',
    '.pb-foot{display:flex;font-size:13px;color:rgba(238,238,238,.7)}',
    '.pb-status{flex:1}',
    '#pb-toast{position:fixed;left:50%;bottom:120px;transform:translateX(-50%);z-index:2147483001;pointer-events:none;display:none;max-width:640px;text-align:center;padding:10px 18px;border-radius:8px;border:1px solid rgba(255,193,7,.75);background:linear-gradient(180deg,rgba(30,30,33,.94),rgba(18,18,20,.96));color:#eee;font-family:"Microsoft YaHei","PingFang SC",sans-serif;font-size:14px;box-shadow:0 6px 20px rgba(0,0,0,.6)}'
  ].join('\n');

  function build() {
    if (ui && document.body.contains(ui.root)) return;

    addStyle();

    var root = el('div');
    root.id = 'pb-root';
    var plate = el('div', 'pb-plate');
    var head = el('div', 'pb-head');
    var sizes = el('div', 'pb-sizes');
    var spots = el('div', 'pb-spots');
    head.appendChild(el('div', 'pb-title', say('title')));
    head.appendChild(sizes);
    head.appendChild(spots);
    head.appendChild(button(document, '\u00d7', 'close', null, true, 'pb-btn pb-x'));

    var cols = el('div', 'pb-cols');
    var homeCol = el('div', 'pb-col');
    var bankCol = el('div', 'pb-col');
    var homeTitle = el('div', 'pb-col-title');
    var bankTitle = el('div', 'pb-col-title');
    var homeList = el('div', 'pb-list');
    var bankList = el('div', 'pb-list');
    homeCol.appendChild(homeTitle);
    homeCol.appendChild(homeList);
    bankCol.appendChild(bankTitle);
    bankCol.appendChild(bankList);
    cols.appendChild(homeCol);
    cols.appendChild(bankCol);

    var foot = el('div', 'pb-foot');
    var status = el('div', 'pb-status');
    var keys = el('div');
    foot.appendChild(status);
    foot.appendChild(keys);

    plate.appendChild(head);
    plate.appendChild(cols);
    plate.appendChild(foot);
    root.appendChild(plate);

    var toast = el('div');
    toast.id = 'pb-toast';

    document.body.appendChild(root);
    document.body.appendChild(toast);

    root.addEventListener('click', onClick);
    // Clicks inside the plate must not reach the game pages underneath; the mask itself closes nothing on purpose.
    root.addEventListener('mousedown', function (e) { e.stopPropagation(); });
    root.addEventListener('error', swapBrokenIcon, true);

    ui = {
      root: root, title: plate.querySelector('.pb-title'), sizes: sizes, spots: spots, homeTitle: homeTitle,
      bankTitle: bankTitle, homeList: homeList, bankList: bankList, status: status, keys: keys, toast: toast
    };
  }

  // A broken icon path swaps to the first-letter placeholder; error events do not bubble, so this listens in capture.
  function swapBrokenIcon(e) {
    var img = e.target;

    if (img && img.tagName === 'IMG' && img.parentNode) img.parentNode.textContent = img.getAttribute('data-letter') || '?';
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
      img.src = item.icon;
      box.appendChild(img);
    } else {
      box.textContent = letter;
    }

    return box;
  }

  function render() {
    var data = state.data;

    ui.title.textContent = say('title');
    renderSizes(data);
    ui.spots.textContent = say('spots', data.spots);
    ui.homeTitle.textContent = say('atHome', data.home.length);
    ui.bankTitle.textContent = say('inBank', data.bank.length);
    renderList(ui.homeList, data.home, homeRow, 'emptyHome');
    renderList(ui.bankList, data.bank, bankRow, 'emptyBank');
    ui.keys.textContent = say('escClose');
  }

  // Story items and every size with limits off show a plain count; a limited size shows used against its limit.
  function renderSizes(data) {
    ui.sizes.textContent = '';

    data.sizes.forEach(function (size) {
      var isCapped = size.cap != null;
      var text = isCapped ? say('used', say(size.key), size.used, size.cap) : say('usedFree', say(size.key), size.used);

      ui.sizes.appendChild(el('div', 'pb-chip' + (isCapped && size.used >= size.cap ? ' pb-at-cap' : ''), text));
    });
  }

  function renderList(list, items, row, emptyKey) {
    var scroll = list.scrollTop;
    list.textContent = '';

    if (items.length === 0) {
      list.appendChild(el('div', 'pb-empty', say(emptyKey)));

      return;
    }

    var fragment = document.createDocumentFragment();

    items.forEach(function (item) { fragment.appendChild(row(item)); });
    list.appendChild(fragment);
    list.scrollTop = scroll;
  }

  // The web view draws no hover tips, so a button that would be refused says why underneath it.
  function homeRow(item) {
    return packageRow(item, button(document, say('bank'), 'store', item.id, !item.why, 'pb-btn'), item.why);
  }

  function bankRow(item) {
    var why = state.data.takeWhy;

    return packageRow(item, button(document, say('takeOut'), 'takeout', item.slot, !why, 'pb-btn'), why);
  }

  function packageRow(item, action, why) {
    var node = el('div', 'pb-row');
    var main = el('div', 'pb-main');
    var tags = el('div', 'pb-tags');
    var acts = el('div', 'pb-acts');
    main.appendChild(el('div', 'pb-name', item.name));

    if (item.size) tags.appendChild(el('span', 'pb-badge' + (item.size === 'story' ? ' pb-badge-story' : ''), say(item.size)));
    if (item.looted) tags.appendChild(el('span', 'pb-badge pb-badge-looted', say('noRefund')));

    main.appendChild(tags);
    acts.appendChild(action);

    if (why) acts.appendChild(el('div', 'pb-why', say(why)));

    node.appendChild(icon(document, item, 'pb-icon'));
    node.appendChild(main);
    node.appendChild(acts);

    return node;
  }

  function open(data) {
    build();
    state.data = data || state.data;
    state.isOpen = true;
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

  // The panel's view while the build shop is open, null otherwise; the shop's buttons and bank rows are drawn from it.
  function shop(data) {
    state.shop = data && data.shop ? data : null;
    shopVersion++;
    patchPagesSafely();
  }

  // The HUD and the build shop are same-origin iframes, so their documents can be reached from here; polled because
  // the game creates and drops those iframes whenever it likes, and the shop redraws its list on every Buy and Refund.
  function patchPages() {
    var frames = document.querySelectorAll('iframe');

    for (var i = 0; i < frames.length; i++) {
      var src = frames[i].src || '';
      var isHud = /CoreUI1\.html/.test(src);

      if (!isHud && !/BuildShop\.html/.test(src)) continue;

      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (!doc) {
        if (!hasReportedPageAccess) send('diag|cannot reach a game page document (root ' + location.protocol + ', iframe ' + src + ')');
        hasReportedPageAccess = true;
        continue;
      }

      if (isHud) placeHudButton(doc);
      else patchShop(doc);
    }
  }

  function patchPagesSafely() {
    try {
      patchPages();
    } catch (e) {}
  }

  // First in the tool column: the column is anchored at the bottom, so the first child sits on top and the game's own
  // buttons do not move. The column hides itself in build mode, and the button with it.
  function placeHudButton(doc) {
    var column = doc.querySelector('.vertical-tools');
    var existing = doc.querySelector('.' + HUD_CLASS);

    if (!column || !state.isLive) {
      if (existing) existing.remove();

      return;
    }

    if (existing && existing.parentNode === column) return;
    if (existing) existing.remove();

    column.insertBefore(hudButton(doc), column.firstChild);

    if (!hasReportedHudPlaced) send('diag|HUD button placed in the tool column');
    hasReportedHudPlaced = true;
  }

  // The same markup as the game's bag button: its classes, an svg icon, and the hover tip its stylesheet shows. The
  // HUD page watches its body for new buttons and tells the game where they are, so a click on this one reaches it.
  function hudButton(doc) {
    var node = doc.createElement('button');
    node.className = 'ui-btn icon-sq interactive-btn has-entry-tip ' + HUD_CLASS;
    node.setAttribute('data-interactive', '');
    node.appendChild(boxIcon(doc));
    node.appendChild(entryTip(doc));
    node.addEventListener('click', function (e) {
      e.stopPropagation();
      send('toggle');
    });

    return node;
  }

  function boxIcon(doc) {
    var svg = doc.createElementNS(SVG_NS, 'svg');
    var path = doc.createElementNS(SVG_NS, 'path');

    svg.setAttribute('class', 'icon-svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    path.setAttribute('d', BOX_PATH);
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
  function patchShop(doc) {
    var data = state.shop;
    var rows = doc.querySelectorAll('.package-item');
    var section = doc.getElementById(SHOP_BANK_ID);

    if (!data || !doc.body) {
      rows.forEach(function (row) { removeAll(row.querySelectorAll('.' + SHOP_BUTTON_CLASS)); });

      if (section) section.remove();

      return;
    }

    addShopStyle(doc);
    rows.forEach(function (row, index) { patchShopRow(doc, row, rowFor(data, index)); });
    placeShopBank(doc, section, rows);

    if (!hasReportedShopPatched) send('diag|bank added to the build shop');
    hasReportedShopPatched = true;
  }

  function rowFor(data, index) {
    return data.shop.filter(function (row) { return row.index === index; })[0] || null;
  }

  function patchShopRow(doc, row, entry) {
    var existing = row.querySelector('.' + SHOP_BUTTON_CLASS);

    if (!entry) {
      if (existing) existing.remove();

      return;
    }

    var node = existing || shopBankButton(doc, row);
    node.textContent = say('bank');
    node.setAttribute('data-index', String(entry.index));
    node.disabled = !!entry.why;
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

    if (!list) return;

    if (section && section.getAttribute('data-version') === String(shopVersion)) {
      if (section.parentNode !== list || list.children[list.children.length - 1] !== section) list.appendChild(section);

      return;
    }

    if (section) section.remove();

    list.appendChild(shopBank(doc));
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

  setInterval(patchPagesSafely, 400);

  window.__slpkgbank = { open: open, setData: setData, close: close, status: status, live: live, shop: shop };
  send('ready');
})();
