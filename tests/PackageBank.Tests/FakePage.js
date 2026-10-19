// Just enough of Root.html, the HUD page (UI/CoreUI1.html) and the build shop (UI/BuildShop/BuildShop.html) for
// page.js to run in Jint: the root document with its page iframes, the vuplex bridge, and clicks that bubble. Test code
// only; nothing here ships.
var window = this;

var location = { protocol: 'https:' };

var intervals = [];

function setInterval(callback) {
  intervals.push(callback);

  return intervals.length;
}

// One run of every interval the script set, which is how its 400 ms page check runs.
function poll() {
  intervals.slice().forEach(function (callback) { callback(); });
}

// Held and never fired: the tests answer every press themselves, as the plugin does.
var timers = {};
var nextTimer = 1;

function setTimeout(callback) {
  timers[nextTimer] = callback;

  return nextTimer++;
}

function clearTimeout(id) {
  delete timers[id];
}

var FakeElement = class {
  constructor(tagName) {
    var element = this;

    this.tagName = tagName;
    this.id = '';
    this.parentElement = null;
    this.children = [];
    this.text = '';
    this.attrs = {};
    this.classes = [];
    this.listeners = {};
    this.style = {};
    this.classList = {
      contains: function (name) { return element.classes.indexOf(name) >= 0; }
    };
  }

  get parentNode() { return this.parentElement; }

  get firstChild() { return this.children.length ? this.children[0] : null; }

  get className() { return this.classes.join(' '); }

  set className(value) { this.classes = String(value).split(' ').filter(function (name) { return name !== ''; }); }

  // As in a browser: an element's text is all of its descendants' text, and setting it replaces the children.
  get textContent() {
    return this.text + this.children.map(function (child) { return child.textContent; }).join('');
  }

  set textContent(value) {
    this.children.slice().forEach(function (child) { this.removeChild(child); }, this);
    this.text = value == null ? '' : String(value);
  }

  hasAttribute(name) { return Object.prototype.hasOwnProperty.call(this.attrs, name); }

  getAttribute(name) { return this.hasAttribute(name) ? this.attrs[name] : null; }

  setAttribute(name, value) { this.attrs[name] = String(value); }

  addEventListener(type, listener) {
    (this.listeners[type] = this.listeners[type] || []).push(listener);
  }

  // A fragment's children move into the parent, as in a browser.
  appendChild(child) {
    if (child.tagName === '#fragment') {
      child.children.slice().forEach(function (moved) { this.appendChild(moved); }, this);

      return child;
    }

    if (child.parentElement) child.parentElement.removeChild(child);

    child.parentElement = this;
    this.children.push(child);

    return child;
  }

  insertBefore(child, before) {
    if (!before) return this.appendChild(child);

    if (child.parentElement) child.parentElement.removeChild(child);

    child.parentElement = this;
    this.children.splice(this.children.indexOf(before), 0, child);

    return child;
  }

  replaceChild(child, old) {
    if (child.parentElement) child.parentElement.removeChild(child);

    this.children[this.children.indexOf(old)] = child;
    child.parentElement = this;
    old.parentElement = null;

    return old;
  }

  removeChild(child) {
    this.children = this.children.filter(function (c) { return c !== child; });
    child.parentElement = null;

    return child;
  }

  remove() {
    if (this.parentElement) this.parentElement.removeChild(this);
  }

  contains(other) {
    for (var element = other; element; element = element.parentElement) {
      if (element === this) return true;
    }

    return false;
  }

  matches(selector) {
    var element = this;

    return selectorSteps(selector).every(function (step) { return matchesStep(element, step); });
  }

  closest(selector) {
    for (var element = this; element; element = element.parentElement) {
      if (element.matches(selector)) return element;
    }

    return null;
  }

  querySelectorAll(selector) {
    return descendantsOf([this]).filter(function (element) { return element.matches(selector); });
  }

  querySelector(selector) {
    return this.querySelectorAll(selector)[0] || null;
  }
};

// Compound selectors of a tag, classes, ids and attributes ("iframe", ".dm-hud-btn", "[data-act]"); the script uses
// nothing else.
function selectorSteps(selector) {
  var steps = selector.match(/^[a-z]+|\.[\w-]+|#[\w-]+|\[[\w-]+\]/g) || [];

  if (steps.join('') !== selector) throw new Error('the fake page does not know the selector "' + selector + '"');

  return steps;
}

function matchesStep(element, step) {
  switch (step.charAt(0)) {
    case '.':
      return element.classList.contains(step.slice(1));
    case '#':
      return element.id === step.slice(1);
    case '[':
      return element.hasAttribute(step.slice(1, -1));
    default:
      return element.tagName === step;
  }
}

function descendantsOf(roots) {
  var found = [];

  roots.forEach(function (root) {
    root.children.forEach(function walk(child) {
      found.push(child);
      child.children.forEach(walk);
    });
  });

  return found;
}

var FakeDocument = class {
  constructor() {
    this.head = new FakeElement('head');
    this.body = new FakeElement('body');
  }

  createElement(tagName) { return new FakeElement(tagName); }

  createElementNS(namespace, tagName) { return new FakeElement(tagName); }

  createTextNode(text) {
    var node = new FakeElement('#text');

    node.textContent = text;

    return node;
  }

  createDocumentFragment() { return new FakeElement('#fragment'); }

  getElementById(id) {
    return descendantsOf([this.head, this.body]).filter(function (element) { return element.id === id; })[0] || null;
  }

  querySelectorAll(selector) {
    return descendantsOf([this.head, this.body]).filter(function (element) { return element.matches(selector); });
  }

  querySelector(selector) {
    return this.querySelectorAll(selector)[0] || null;
  }
};

// Root.html's own document, which holds the page iframes.
var document = new FakeDocument();

// What the script posts to the plugin.
var posted = [];

window.vuplex = {
  postMessage: function (message) { posted.push(message); }
};


function frame(src) {
  var node = document.body.appendChild(new FakeElement('iframe'));

  node.src = src;
  node.contentDocument = new FakeDocument();

  return node.contentDocument;
}

// The HUD with its tool column and the game's bag button in it.
function openHud() {
  var doc = frame('https://game.local/UI/CoreUI1/CoreUI1.html');
  var column = doc.body.appendChild(new FakeElement('div'));

  column.className = 'vertical-tools';
  column.appendChild(new FakeElement('button')).className = 'ui-btn icon-sq';

  return doc;
}

function hudButton() {
  return pageOf(/CoreUI1/).querySelector('.pb-hud-btn');
}

// The build shop with its package list, one row per name, each ending in the shop's Refund button.
function openShop(names) {
  var doc = frame('https://game.local/UI/BuildShop/BuildShop.html');
  var wrapper = doc.body.appendChild(new FakeElement('div'));

  wrapper.className = 'left-content-wrapper custom-scroll';
  names.forEach(function (name) { addShopRow(name); });

  return doc;
}

// The shop adding a row after a Buy, as Vue does: at the end of its own rows, before anything a mod put after them.
function addShopRow(name) {
  var wrapper = shopPage().querySelector('.left-content-wrapper');
  var rows = wrapper.querySelectorAll('.package-item');
  var row = new FakeElement('div');
  var label = row.appendChild(new FakeElement('div'));
  var refund = row.appendChild(new FakeElement('button'));

  row.className = 'package-item';
  label.className = 'package-name';
  label.textContent = name;
  refund.className = 'return-btn';
  refund.textContent = 'Refund';

  var after = rows.length ? rows[rows.length - 1] : null;
  var next = after ? wrapper.children[wrapper.children.indexOf(after) + 1] : wrapper.firstChild;

  wrapper.insertBefore(row, next || null);

  return row;
}

function shopPage() {
  return pageOf(/BuildShop/);
}

function pageOf(pattern) {
  return document.querySelectorAll('iframe').filter(function (node) { return pattern.test(node.src); })[0].contentDocument;
}

function shopRows() {
  return shopPage().querySelectorAll('.package-item');
}

function shopBankButton(index) {
  return shopRows()[index].querySelector('.pb-bank-btn');
}

function shopBank() {
  return shopPage().getElementById('pb-shop-bank');
}

function shopTakeOut(slot) {
  return shopBank().querySelectorAll('[data-act]').filter(function (node) { return node.getAttribute('data-args') === String(slot); })[0];
}

function panelButton(action, args) {
  return document.querySelectorAll('[data-act]').filter(function (node) {
    return node.getAttribute('data-act') === action && (args == null || node.getAttribute('data-args') === String(args));
  })[0];
}

// A click as the game's click-through delivers one: dispatched on whatever is under the pointer, greyed buttons
// included, bubbling up until a listener stops it.
function click(target) {
  var isStopped = false;
  var event = { target: target, stopPropagation: function () { isStopped = true; } };

  for (var node = target; node && !isStopped; node = node.parentElement) {
    (node.listeners.click || []).slice().forEach(function (listener) { listener(event); });
  }
}
