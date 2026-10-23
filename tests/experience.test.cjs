const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

function setup({reduce = false, height = 900} = {}) {
  const events = {}, elements = new Map();
  const make = id => {
    if (elements.has(id)) return elements.get(id);
    const attrs = {}, handlers = {}, classes = new Set();
    const element = {dataset: {}, style: {setProperty() {}}, hidden: false, textContent: '',
      classList: {toggle(name, on) {on ? classes.add(name) : classes.delete(name);}, contains: name => classes.has(name)},
      setAttribute(k, v) {attrs[k] = String(v);}, getAttribute: k => attrs[k],
      addEventListener(k, fn) {handlers[k] = fn;}, click() {handlers.click({preventDefault() {}});}, focus() {}};
    elements.set(id, element); return element;
  };
  const section = make('journey'), mode = make('journeyMode');
  const buttons = Array.from({length: 4}, (_, i) => {const button = make(`button${i}`); button.dataset.step = i; return button;});
  const links = Array.from({length: 4}, (_, i) => {const link = make(`link${i}`); link.dataset.stageLink = i; return link;});
  section.querySelectorAll = () => buttons;
  section.querySelector = () => make('svg');
  const query = {matches: reduce, addEventListener(type, fn) {this.change = fn;}};
  const document = {documentElement: {lang: 'ko'}, getElementById: make,
    querySelector: () => ({getBoundingClientRect: () => ({height: 88})}),
    querySelectorAll: () => links,
    addEventListener: (type, fn) => {events[type] = fn;}};
  const window = {scrollY: 0, innerHeight: height,
    scrollTo({top}) {this.scrollY = top;}, addEventListener(type, fn) {events[type] = fn;}};
  section.getBoundingClientRect = () => {
    const h = section.classList.contains('journey-static') ? height - 88 : height * 4;
    return {top: 500 - window.scrollY, bottom: 500 - window.scrollY + h, height: h};
  };
  let pending;
  vm.runInNewContext(fs.readFileSync('output/experience.js', 'utf8'), {document, window, innerWidth: 1280, innerHeight: height,
    matchMedia: () => query, requestAnimationFrame: fn => {pending = fn; return 1;}});
  const flush = () => {if (pending) {const fn = pending; pending = null; fn();}};
  const scroll = value => {window.scrollY = 412 + value * (height * 4 - (height - 88)); events.scroll(); flush();};
  return {make, buttons, links, mode, section, query, document, events, window, scroll, flush};
}
const page = setup();
for (let i = 0; i < 4; i++) {
  page.buttons[i].click();
  assert.equal(page.section.dataset.stage, String(i));
  assert.equal(page.buttons[i].getAttribute('aria-pressed'), 'true');
}
assert.equal(page.make('rain').getAttribute('opacity'), '1');
page.scroll(.6);
assert.equal(page.section.dataset.stage, '2');
assert.equal(page.make('rain').getAttribute('opacity'), '0');
page.scroll(.3);
assert.equal(page.section.dataset.stage, '1');
page.scroll(.96);
const cloudPose = page.make('cloud-left').getAttribute('transform');
page.mode.click();
assert(page.section.classList.contains('journey-static'));
assert.equal(page.make('cloud-left').getAttribute('transform'), cloudPose);
page.mode.click();
assert(!page.section.classList.contains('journey-static'));
assert.equal(page.make('cloud-left').getAttribute('transform'), cloudPose);
page.document.documentElement.lang = 'en'; page.events.languagechange();
assert.equal(page.make('stepTitle').textContent, 'Make it rain.');
page.links[0].click();
assert.equal(page.section.dataset.stage, '0');
const reduced = setup({reduce: true});
assert(reduced.section.classList.contains('journey-static'));
assert(reduced.mode.hidden);
reduced.buttons[3].click();
assert.equal(reduced.section.dataset.stage, '3');
reduced.scroll(.1);
assert.equal(reduced.section.dataset.stage, '3');
const short = setup({height: 480});
assert(short.section.classList.contains('journey-static'));
assert(short.mode.hidden);
console.log('Passed: stage buttons, reverse scroll, mode continuity, language, reduced motion, short viewport.');
