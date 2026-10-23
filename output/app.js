'use strict';

// The Korean document stays readable when JavaScript or storage is unavailable.
const translations = [...document.querySelectorAll('[data-en]')].map(element => ({element, ko: element.innerHTML, en: element.dataset.en}));
const translatedAttributes = ['aria-label', 'alt', 'title'].flatMap(attribute =>
  [...document.querySelectorAll(`[data-en-${attribute}]`)].map(element => ({element, attribute, ko: element.getAttribute(attribute), en: element.getAttribute(`data-en-${attribute}`)}))
);
const metaCopy = {
  ko: {title: '마음 한첩 — A Dose of Heart', description: '마음 한첩은 신령이 되어 아이의 아버지를 위한 약을 만드는 Meta Quest 3 VR 작품입니다.', social: '마음 한첩은 신령이 되어 아이의 아버지를 위한 약을 만드는 Meta Quest 3 VR 작품입니다.', image: '가을빛 감나무골을 담은 마음 한첩의 디오라마'},
  en: {title: 'A Dose of Heart — 마음 한첩', description: 'A Dose of Heart is an interactive VR story for Meta Quest 3. Play a spirit preparing medicine for a child to take home.', social: 'An interactive VR story for Meta Quest 3. Play a spirit preparing medicine for a child to take home.', image: 'The autumn diorama of Persimmon Village in A Dose of Heart'}
};
let language = 'ko';
const galleryButtons = [...document.querySelectorAll('[data-gallery]')];
const dialog = document.getElementById('galleryDialog');
let imageIndex = 0;

function applyLanguage(next, save = false) {
  language = next === 'en' ? 'en' : 'ko';
  document.documentElement.lang = language;
  translations.forEach(item => { item.element.innerHTML = item[language]; });
  translatedAttributes.forEach(item => { item.element.setAttribute(item.attribute, item[language]); });
  document.querySelectorAll('[data-lang]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.lang === language)));
  const copy = metaCopy[language];
  const isExperience = document.body.dataset.page === 'experience';
  document.title = isExperience ? (language === 'en' ? 'Experience — A Dose of Heart' : '체험 방식 — 마음 한첩') : copy.title;
  const experienceDescription = language === 'en' ? 'Sit down, gather clouds with two controllers, and make it rain. Explore the steps and playtest photos of A Dose of Heart.' : '의자에 앉아 컨트롤러로 구름을 뭉쳐 비를 내립니다. 마음 한첩의 체험 순서와 플레이 테스트 사진을 확인하세요.';
  document.querySelector('meta[name="description"]').content = isExperience ? experienceDescription : copy.description;
  document.querySelector('meta[property="og:title"]').content = document.title;
  document.querySelector('meta[property="og:description"]').content = isExperience ? experienceDescription : copy.social;
  document.querySelector('meta[property="og:image:alt"]').content = copy.image;
  document.querySelector('meta[property="og:locale"]').content = language === 'en' ? 'en_US' : 'ko_KR';
  const subject = language === 'en' ? 'A Dose of Heart — demonstration / exhibition enquiry' : '마음 한첩 체험·전시 문의';
  const emailLink = document.getElementById('emailLink');
  if (emailLink) emailLink.href = `mailto:dearbrave1111@gmail.com?subject=${encodeURIComponent(subject)}`;
  document.querySelectorAll('[data-page-link]').forEach(link => {
    const destination = new URL(link.href);
    destination.searchParams.set('lang', language);
    link.href = destination.href;
  });
  const video = document.querySelector('#videoFrame iframe');
  if (video) video.title = language === 'en' ? 'A Dose of Heart — playthrough' : '마음 한첩 플레이 영상';
  if (dialog?.open) showImage(imageIndex);
  if (save) {
    try { localStorage.setItem('mh_lang', language); } catch (_) { /* Storage can be disabled. */ }
    const url = new URL(location.href);
    url.searchParams.set('lang', language);
    history.replaceState(null, '', url);
  }
  document.dispatchEvent(new CustomEvent('languagechange', {detail: language}));
}

let initialLanguage = new URLSearchParams(location.search).get('lang');
if (!['ko', 'en'].includes(initialLanguage)) {
  try { initialLanguage = localStorage.getItem('mh_lang'); } catch (_) { /* Use the browser language. */ }
}
if (!['ko', 'en'].includes(initialLanguage)) initialLanguage = navigator.language.toLowerCase().startsWith('ko') ? 'ko' : 'en';
applyLanguage(initialLanguage);
document.querySelectorAll('[data-lang]').forEach(button => button.addEventListener('click', () => applyLanguage(button.dataset.lang, true)));

document.getElementById('playVideo')?.addEventListener('click', () => {
  const iframe = document.createElement('iframe');
  iframe.src = 'https://www.youtube.com/embed/7jBBZXJNfPc?autoplay=1&playsinline=1&rel=0';
  iframe.title = language === 'en' ? 'A Dose of Heart — playthrough' : '마음 한첩 플레이 영상';
  iframe.allow = 'autoplay; encrypted-media; picture-in-picture; fullscreen';
  iframe.allowFullscreen = true;
  iframe.referrerPolicy = 'strict-origin-when-cross-origin';
  document.getElementById('videoPoster').hidden = true;
  document.getElementById('videoFrame').append(iframe);
  iframe.focus();
}, {once: true});

function showImage(index) {
  imageIndex = (index + galleryButtons.length) % galleryButtons.length;
  const button = galleryButtons[imageIndex];
  const image = document.getElementById('galleryImage');
  image.src = button.dataset.full;
  image.alt = button.querySelector('img').alt;
  document.getElementById('galleryCaption').textContent = button.closest('figure').querySelector('figcaption span').textContent;
  document.getElementById('galleryCount').textContent = `${String(imageIndex + 1).padStart(2, '0')} / ${String(galleryButtons.length).padStart(2, '0')}`;
}
galleryButtons.forEach((button, index) => button.addEventListener('click', () => {
  showImage(index);
  dialog.showModal();
  document.body.classList.add('modal-open');
  document.getElementById('closeGallery').focus();
}));
document.getElementById('closeGallery')?.addEventListener('click', () => dialog.close());
document.getElementById('previousImage')?.addEventListener('click', () => showImage(imageIndex - 1));
document.getElementById('nextImage')?.addEventListener('click', () => showImage(imageIndex + 1));
dialog?.addEventListener('click', event => { if (event.target === dialog) dialog.close(); });
dialog?.addEventListener('keydown', event => {
  if (event.key === 'Tab') {
    const first = document.getElementById('closeGallery');
    const last = document.getElementById('nextImage');
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }
  if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
    event.preventDefault();
    showImage(imageIndex + (event.key === 'ArrowLeft' ? -1 : 1));
  }
});
dialog?.addEventListener('close', () => document.body.classList.remove('modal-open'));

if ('IntersectionObserver' in window) {
  const navigationLinks = [...document.querySelectorAll('.site-header nav a[href^="#"]')];
  const observer = new IntersectionObserver(entries => {
    entries.forEach(entry => {
      if (!entry.isIntersecting) return;
      navigationLinks.forEach(link => {
        if (link.hash === `#${entry.target.id}`) link.setAttribute('aria-current', 'location');
        else link.removeAttribute('aria-current');
      });
    });
  }, {rootMargin: '-20% 0px -60% 0px', threshold: 0});
  document.querySelectorAll('main > section').forEach(section => observer.observe(section));
}
