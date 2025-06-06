# **🛠️ W3ObjectEditor**

**Warcraft III 오브젝트 데이터 편집기**

유닛(.w3u), 스킬(.w3a), 마법/버프(.w3h), 아이템(.w3t) 파일을 직관적인 UI로 편집할 수 있는 Windows Forms 기반 도구입니다.


<img width="450" alt="Program" src="https://raw.githubusercontent.com/feralshining/W3ObjectEditor/main/assets/program.PNG">
<br>


## **📦 주요 기능**

- ✅ **`.w3u`**, **`.w3a`**, **`.w3h`**, **`.w3t`** 파일 읽기 및 수정 지원
- ✅ DataGridView 기반 데이터 시각화 및 편집
- ✅ 편집 모드 ON/OFF 토글 지원
- ✅ CSV 내보내기 및 불러오기 (UTF-8 지원, 줄바꿈 포함)
- ✅ 툴팁(Ubertip) 등 여러 줄 텍스트 필드 완전 지원
- ✅ 검색 기능 및 일치 항목 탐색
- ✅ 총 데이터 수 / 선택된 수 / 선택된 데이터 인덱스 값 등 상태 표시줄 표시
- ✅ .NET Framework 4.7.2 이상에서 실행 가능


## **🧩 지원 형식**

| **확장자** | **설명** |
| --- | --- |
| **`.w3u`** | 유닛(Unit) 데이터 |
| **`.w3a`** | 능력(Ability) 데이터 |
| **`.w3h`** | 효과(Effect/Buff) 데이터 |
| **`.w3t`** | 아이템(Item) 데이터 |


## **🔧 사용법**

### **1. 실행**

- **`W3ObjectEditor.exe`** 실행
- **`.w3u`**, **`.w3a`**, **`.w3h`**, **`.w3t`** 파일 중 하나를 열기

### **2. 편집**

- 편집 모드 토글 버튼을 눌러 활성화
- 셀 클릭 후 수정 가능 (더블 클릭 또는 F2)

### **3. 저장**

- 상단 메뉴 → Save As
- 기존 파일에 덮어쓰기

### **4. CSV 연동**

- 파일 → CSV 내보내기: 현재 데이터 내보내기
- 파일 → CSV 불러오기: 편집한 CSV 데이터 불러오기
    
    ⚠️ **`줄바꿈`**, **`쉼표`**, **`큰따옴표`** 등은 자동 이스케이프 처리됨
    


## **📝 개발 환경**

- 언어: C#
- 프레임워크: .NET Framework 4.7.2
- UI: Windows Forms
- 추가 라이브러리: 없음


## **🚧 향후 계획**

- 사용자 지정 필터 기능 추가
- 백업/변경 이력 관리 기능 추가


## **📬 문의 및 기여**

- 이슈 등록: [Issue 탭](https://github.com/feralshining/W3ObjectEditor/issues)
- 기여 가이드: PR 대환영!
