# 마음 한첩 VR 웹사이트 - 배포 가이드

## 🚀 자동 배포 (Vercel CLI 사용)

### 준비사항
```bash
# Node.js 설치 확인
node --version
npm --version

# Vercel CLI 설치
npm install -g vercel

# Vercel 로그인
vercel login
```

### 배포 실행
```bash
# 프로젝트 폴더로 이동
cd "웹 사이트 만들기/landing page"

# 배포 시작
vercel --prod

# 설정 선택:
# - Link to existing project? → No
# - Project name → maum-hancheop-vr
# - Directory → output
```

---

## 🌐 수동 배포 (웹 인터페이스 - 추천)

### 1. GitHub 리포지토리 생성
```
1. https://github.com 접속
2. "New repository" 클릭
3. Repository name: maum-hancheop-vr
4. Description: VR 인터랙티브 시네마틱 작품 '마음 한첩' 웹사이트
5. Public 선택
6. "Create repository" 클릭
```

### 2. 파일 업로드 (수동)
```
1. GitHub 리포지토리 페이지에서 "uploading an existing file" 클릭
2. output/index.html 파일 업로드
3. 필요한 이미지 파일들도 업로드
```

### 3. Vercel 연결
```
1. https://vercel.com 접속 (GitHub 계정으로 로그인)
2. "New Project" 클릭
3. "Import Git Repository" → GitHub 선택
4. maum-hancheop-vr 리포지토리 선택
5. Build Settings:
   - Framework Preset: Other
   - Root Directory: output (또는 ./)
   - Build Command: (비워두기)
   - Output Directory: ./
6. "Deploy" 클릭
```

### 4. 도메인 확인
배포 완료 후 URL이 생성됩니다:
```
https://maum-hancheop-vr.vercel.app
```

---

## 📁 업로드할 파일들

### 필수 파일
- `output/index.html` (메인 웹페이지)

### 선택 파일 (추후 추가)
- `products/마음한첩/images/` 폴더의 스크린샷들
- `brand/logo/logo.png` (로고 파일)

---

## 🔧 로컬 테스트

배포 전 로컬에서 테스트:
```
1. output/index.html 파일을 브라우저에서 열기
2. 모든 링크와 스타일이 제대로 작동하는지 확인
```

---

## 📊 Google Analytics 확인

Vercel 배포 후:
```
1. Google Analytics 실시간 대시보드 확인
2. 방문자 통계가 쌓이는지 테스트
```

---

## 💡 팁

- **도메인 커스텀**: vercel.com에서 유료 플랜으로 커스텀 도메인 연결 가능
- **HTTPS 자동**: Vercel에서 무료 HTTPS 제공
- **빠른 로딩**: 글로벌 CDN으로 빠른 속도 보장

배포 완료되면 URL을 공유해주세요! 🌐