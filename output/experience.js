'use strict';
(() => {
  const section = document.getElementById('journey');
  if (!section) return;
  const chapters = [
    {at: 0, seek: .12, ko: ['의자에 앉습니다.', '앉은 자세로 체험을 시작합니다.'], en: ['Take a seat.', 'The experience is played seated.']},
    {at: .23, seek: .36, ko: ['기기를 착용합니다.', '헤드셋을 쓰고 양손에 컨트롤러를 듭니다.'], en: ['Put on the headset.', 'Hold a controller in each hand.']},
    {at: .48, seek: .66, ko: ['구름을 뭉칩니다.', '컨트롤러를 움직여 구름을 한데 모읍니다.'], en: ['Gather the clouds.', 'Move the controllers to bring the clouds together.']},
    {at: .76, seek: .96, ko: ['비를 내립니다.', '뭉친 구름에서 비가 내립니다.'], en: ['Make it rain.', 'Rain falls from the gathered clouds.']}
  ];
  const $ = id => document.getElementById(id);
  const buttons = [...section.querySelectorAll('[data-step]')];
  const reduced = matchMedia('(prefers-reduced-motion: reduce)');
  let progress = 0, active = -1, currentLanguage = '', manual = false, staticMode = false, frame = 0;
  let headerHeight = 88;
  const clamp = n => Math.max(0, Math.min(1, n));
  const ease = (p, start, end) => {const t = clamp((p - start) / (end - start)); return t * t * (3 - 2 * t);};
  const translate = (id, x, y, extra = '') => $(id).setAttribute('transform', `translate(${x} ${y}) ${extra}`);
  const opacity = (id, value) => $(id).setAttribute('opacity', value);

  function render(value) {
    progress = clamp(value);
    const index = chapters.reduce((result, chapter, i) => progress >= chapter.at ? i : result, 0);
    const lang = document.documentElement.lang === 'en' ? 'en' : 'ko';
    if (index !== active || currentLanguage !== lang) {
      active = index;
      currentLanguage = lang;
      $('stepNumber').textContent = String(active + 1).padStart(2, '0');
      $('stepTitle').textContent = chapters[active][lang][0];
      $('stepCopy').textContent = chapters[active][lang][1];
      buttons.forEach((button, i) => button.setAttribute('aria-pressed', String(i === active)));
    }
    $('journeyMode').textContent = lang === 'en' ? (staticMode ? 'Scroll through' : 'Step by step') : (staticMode ? '스크롤로 보기' : '단계별로 보기');
    $('journeyMode').setAttribute('aria-pressed', String(staticMode));
    $('journeyHint').textContent = lang === 'en' ? (staticMode ? 'Choose a step.' : 'Scroll or choose a step.') : (staticMode ? '단계를 선택하세요.' : '스크롤하거나 단계를 선택하세요.');
    $('journeyProgress').style.transform = `scaleX(${progress})`;
    const seat = ease(progress, 0, .16);
    const gear = ease(progress, .23, .37);
    const gather = ease(progress, .48, .74);
    const rainfall = ease(progress, .76, .88);
    translate('person', -28 * (1 - seat), -24 * (1 - seat));
    opacity('person', .5 + .5 * seat);
    translate('headset', 0, -60 * (1 - gear));
    opacity('headset', gear);
    opacity('cloud-left', .18 + .82 * gear);
    opacity('cloud-right', .18 + .82 * gear);
    translate('cloud-left', 590 + 95 * gather, 164 - 8 * gather);
    translate('cloud-right', 830 - 95 * gather, 164 + 8 * gather);
    const backX = 369 + 47 * gear + 27 * gather, backY = 326 - 55 * gear - 36 * gather;
    const frontX = 388 + 94 * gear - 15 * gather, frontY = 330 - 22 * gear - 53 * gather;
    $('arm-back').setAttribute('d', `M294 274L${320 + 20 * gear} ${318 - 20 * gear}L${backX} ${backY}`);
    $('arm-front').setAttribute('d', `M324 274L${343 + 30 * gear} ${317 - 15 * gear}L${frontX} ${frontY}`);
    translate('hand-back', backX, backY, `rotate(${-25 * gather})`);
    translate('hand-front', frontX, frontY, `rotate(${-20 * gather})`);
    opacity('hand-back', gear); opacity('hand-front', gear);
    $('ray-left').setAttribute('d', `M${backX + 8} ${backY - 10}L${590 + 95 * gather} 195`);
    $('ray-right').setAttribute('d', `M${frontX + 8} ${frontY - 10}L${830 - 95 * gather} 195`);
    opacity('controller-rays', gear * .7);
    opacity('rain', rainfall); opacity('rain-rings', rainfall);
    translate('rain', 0, 25 * rainfall);
    section.dataset.stage = String(active);
  }
  function metrics() {
    const rect = section.getBoundingClientRect();
    return {top: window.scrollY + rect.top - headerHeight, distance: Math.max(1, rect.height - (window.innerHeight - headerHeight))};
  }
  function syncScroll() {
    frame = 0;
    if (staticMode) return;
    const {top, distance} = metrics();
    render((window.scrollY - top) / distance);
  }
  function schedule() { if (!frame) frame = requestAnimationFrame(syncScroll); }
  function seek(index) {
    const value = chapters[index].seek;
    const {top, distance} = metrics();
    window.scrollTo({top: top + (staticMode ? 0 : value * distance), behavior: 'instant'});
    render(value);
  }
  function updateMode(preserve = true) {
    const rect = section.getBoundingClientRect();
    const visible = rect.top < innerHeight && rect.bottom > headerHeight;
    const saved = progress;
    const next = manual || reduced.matches || innerHeight - headerHeight < 460;
    const changed = next !== staticMode;
    staticMode = next;
    section.classList.toggle('journey-static', staticMode);
    $('journeyMode').hidden = reduced.matches || innerHeight - headerHeight < 460;
    if (changed && preserve && visible) {
      const {top, distance} = metrics();
      window.scrollTo({top: top + (staticMode ? 0 : saved * distance), behavior: 'instant'});
    }
    render(saved);
  }
  function resize() {
    headerHeight = document.querySelector('.site-header').getBoundingClientRect().height;
    section.style.setProperty('--journey-header', `${headerHeight}px`);
    section.querySelector('svg').setAttribute('viewBox', innerWidth <= 760 ? '210 90 750 430' : '0 0 1000 540');
    updateMode();
    schedule();
  }
  buttons.forEach(button => button.addEventListener('click', () => seek(Number(button.dataset.step))));
  document.querySelectorAll('[data-stage-link]').forEach(link => link.addEventListener('click', event => {
    event.preventDefault();
    const index = Number(link.dataset.stageLink);
    seek(index);
    buttons[index].focus({preventScroll: true});
  }));
  $('journeyMode').addEventListener('click', () => {manual = !manual; updateMode();});
  document.addEventListener('languagechange', () => render(progress));
  reduced.addEventListener('change', () => updateMode());
  window.addEventListener('scroll', schedule, {passive: true});
  window.addEventListener('resize', resize);
  headerHeight = document.querySelector('.site-header').getBoundingClientRect().height;
  section.style.setProperty('--journey-header', `${headerHeight}px`);
  section.querySelector('svg').setAttribute('viewBox', innerWidth <= 760 ? '210 90 750 430' : '0 0 1000 540');
  updateMode(false);
  syncScroll();
})();
