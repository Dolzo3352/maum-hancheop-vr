# 마음 한첩 웹사이트

`index.html`, `styles.css`, `app.js`와 `images/`로 구성된 정적 사이트입니다. Vercel의 기존 배포 디렉터리 `output`을 그대로 사용합니다.

- 기본 문구는 HTML의 한국어 본문입니다. 각 요소의 `data-en`에 영문을, `data-en-alt`와 `data-en-aria-label`에 영문 접근성 설명을 저장합니다.
- 언어는 `?lang=ko`, `?lang=en`으로 지정할 수 있습니다. 명시한 언어, 저장한 선택, 브라우저 언어 순으로 적용합니다.
- YouTube 영상은 사용자가 재생 버튼을 누른 뒤에만 로드합니다.
- 갤러리는 키보드로 열고, 방향키로 이동하고, Esc로 닫을 수 있습니다.
- 체험 시간과 장소·일정은 아직 확인되지 않아 문의로 안내합니다.
- 메인 페이지는 영상 → 줄거리와 작품 장면 → 체험 순서 → 문의로 이어집니다. 체험 순서는 `index.html#experience`에 있으며, `experience.js`의 단계 문구와 SVG 자세가 스크롤 진행률에 따라 바뀝니다. 단계 버튼, 단계별 모드, 동작 줄이기 설정을 지원합니다. 기존 `experience.html` 주소는 메인의 체험 영역으로 이동합니다.
- `playtest.html`은 사용자 테스트 사진 전용 페이지입니다. 두 사진 모두 확대해서 볼 수 있습니다.
- 실제 플레이 테스트 사진은 `images/playtest/`에 있습니다. 첫 사진은 웹용으로 축소했고, 두 번째 사진은 원본 해상도를 유지했습니다.
- 체험 페이지의 동작 검증은 프로젝트 루트에서 `node tests/experience.test.cjs`로 실행합니다.
- 페이지 순서, 사진 분리, 내부 링크와 중복 ID는 `python tests/site-structure.py`로 확인합니다.
- 체험 일러스트는 `python scripts/build-experience-scene.py`로 다시 생성합니다. 인물, 한옥, 감나무, 구름과 원형 게이지를 각각 SVG 요소로 구성했습니다. 게이지 충전 → 구름 소멸 → 강우 순서로 진행됩니다.

프로젝트 루트에서 `python scripts/preview.py`를 실행하면 `http://127.0.0.1:4173`에서 미리 볼 수 있습니다. 이 서버는 `output`만 제공합니다.

원본 이미지는 `images/world/`에 보존됩니다. Pillow가 설치된 Python에서 `python scripts/prepare-images.py`를 실행하면 WebP 이미지와 공유용 JPEG를 다시 생성합니다.

