'use strict';
(() => {
  const section = document.getElementById('journey');
  if (!section) return;
  const chapters = [
    {at: 0, seek: .12, ko: ['의자에 앉습니다.', '앉은 자세로 체험을 시작합니다.'], en: ['Take a seat.', 'The experience is played seated.']},
    {at: .23, seek: .36, ko: ['기기를 착용합니다.', '헤드셋을 쓰고 양손에 컨트롤러를 듭니다.'], en: ['Put on the headset.', 'Hold a controller in each hand.']},
    {at: .48, seek: .66, ko: ['구름을 뭉칩니다.', '구름을 뭉치면 원형 게이지가 찹니다.'], en: ['Gather the clouds.', 'The circular gauge fills as you gather the clouds.']},
    {at: .76, seek: .96, ko: ['비를 내립니다.', '게이지가 차면 구름이 사라지고 비가 내립니다.'], en: ['Make it rain.', 'When the gauge is full, the clouds disappear and rain begins.']}
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
    const gather = ease(progress, .48, .76);
    const vanished = ease(progress, .77, .84);
    const rainfall = ease(progress, .84, .93);
    translate('person', -8 * (1 - seat), -16 * (1 - seat));
    translate('headset', 0, -60 * (1 - gear));
    opacity('headset', gear);
    opacity('cloud-left', gear * (1 - vanished));
    opacity('cloud-right', gear * (1 - vanished));
    const cloudScale = .72 - .16 * gather - .12 * vanished;
    translate('cloud-left', 590 + 110 * gather, 164 - 8 * gather, `scale(${cloudScale})`);
    translate('cloud-right', 850 - 110 * gather, 164 + 8 * gather, `scale(${cloudScale})`);
    opacity('cloud-gauge', ease(progress, .45, .5) * (1 - ease(progress, .81, .88)));
    $('gauge-fill').setAttribute('stroke-dashoffset', 100 * (1 - gather));
    $('gauge-value').textContent = `${Math.round(gather * 100)}%`;
    const backX = 345 + 56 * gear + 9 * gather, backY = 354 - 83 * gear - 25 * gather;
    const frontX = 359 + 84 * gear - 5 * gather, frontY = 360 - 52 * gear - 35 * gather;
    const backArm = `M286 269Q${299 + 12 * gear} ${303 - 10 * gear} ${320 + 20 * gear} ${318 - 20 * gear}L${backX - 8} ${backY}`;
    const frontArm = `M323 273Q${328 + 20 * gear} ${301 - 12 * gear} ${343 + 30 * gear} ${317 - 15 * gear}L${frontX - 9} ${frontY}`;
    $('arm-back').setAttribute('d', backArm);
    $('arm-front').setAttribute('d', frontArm);
    $('arm-back-seam').setAttribute('d', backArm);
    $('arm-front-seam').setAttribute('d', frontArm);
    translate('hand-back', backX, backY, `rotate(${-25 * gather})`);
    translate('hand-front', frontX, frontY, `rotate(${-20 * gather})`);
    opacity('controller-back', gear); opacity('controller-front', gear);
    $('ray-left').setAttribute('d', `M${backX + 8} ${backY - 10}L${590 + 110 * gather} 175`);
    $('ray-right').setAttribute('d', `M${frontX + 8} ${frontY - 10}L${850 - 110 * gather} 185`);
    opacity('controller-rays', gear * .55 * (1 - vanished));
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
    section.querySelector('svg').setAttribute('viewBox', innerWidth <= 760 ? '130 25 850 500' : '0 0 1000 540');
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
  section.querySelector('svg').setAttribute('viewBox', innerWidth <= 760 ? '130 25 850 500' : '0 0 1000 540');
  updateMode(false);
  syncScroll();
})();
