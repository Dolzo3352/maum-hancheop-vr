# 마음 한첩 웹사이트

`index.html`, `styles.css`, `app.js`와 `images/`로 구성된 정적 사이트입니다. Vercel의 기존 배포 디렉터리 `output`을 그대로 사용합니다.

- 기본 문구는 HTML의 한국어 본문입니다. 각 요소의 `data-en`에 영문을, `data-en-alt`와 `data-en-aria-label`에 영문 접근성 설명을 저장합니다.
- 언어는 `?lang=ko`, `?lang=en`으로 지정할 수 있습니다. 명시한 언어, 저장한 선택, 브라우저 언어 순으로 적용합니다.
- YouTube 영상은 사용자가 재생 버튼을 누른 뒤에만 로드합니다.
- 갤러리는 키보드로 열고, 방향키로 이동하고, Esc로 닫을 수 있습니다.
- 체험 시간과 장소·일정은 아직 확인되지 않아 문의로 안내합니다.

프로젝트 루트에서 `python scripts/preview.py`를 실행하면 `http://127.0.0.1:4173`에서 미리 볼 수 있습니다. 이 서버는 `output`만 제공합니다.

원본 이미지는 `images/world/`에 보존됩니다. Pillow가 설치된 Python에서 `python scripts/prepare-images.py`를 실행하면 WebP 이미지와 공유용 JPEG를 다시 생성합니다.

