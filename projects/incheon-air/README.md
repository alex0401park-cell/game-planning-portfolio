# Incheon-Air — 기업 연계 대기 정보 서비스

도로 CCTV를 활용한 초미세먼지 예측 결과를 지도에 표현하는 3인 캡스톤 프로젝트입니다. 협약 산업체는 (주)이안에스아이티이며, 저는 기획과 프론트엔드 전반을 담당했습니다.

## 소스와 역할

- [팀 저장소](https://github.com/lc-moon/26_2_AICapstone_service)
- [확인한 프론트엔드 소스 버전](https://github.com/lc-moon/26_2_AICapstone_service/tree/273861f41a542df9b98d667706fd72ec2d4b7c1f/web)
- [해당 버전 실행·구현 문서](https://github.com/lc-moon/26_2_AICapstone_service/blob/273861f41a542df9b98d667706fd72ec2d4b7c1f/web/README.md)

저는 `web/`의 화면 구현과 API 연동을 맡았습니다. 모델과 백엔드는 팀원들이 담당했습니다. 위 링크는 로컬 Git 원격 주소와 커밋을 바탕으로 연결했으며 외부 계정의 열람 권한은 아직 확인하지 않았습니다.

## 구현 사례

| 작업 | 사용자에게 제공하는 동작 | 실제 변경 기록 |
| --- | --- | --- |
| 지도 상태 복원 | 원리 페이지를 보고 돌아오면 지도 시점과 선택 위치 복원 | [3791ab4](https://github.com/lc-moon/26_2_AICapstone_service/commit/3791ab496a356470e990f701f64cab55dc8abebc) |
| 모바일 검색 | 검색창을 접고 지도 영역 복구 | [736dc12](https://github.com/lc-moon/26_2_AICapstone_service/commit/736dc12471ceb8e57b4e79dd175d4fc2cb353752) |
| 상세 패널 배치 | 실제 패널 높이를 반영해 선택 지점 정렬 | [28dde19](https://github.com/lc-moon/26_2_AICapstone_service/commit/28dde193a715c3a593c1903ff225f46fa3bcd5e8) |
| 근거 위치 | API 좌표에 따른 CCTV·측정소 위치와 방향 표시 | [c0bc50d](https://github.com/lc-moon/26_2_AICapstone_service/commit/c0bc50d7d03d9aa27e172d150100ca1a262f3ac2) |

빠른 연속 선택 뒤 늦게 도착한 응답을 무시하고, 사진이 없는 경우 패널을 줄이며, 밤·비·자료 부족 등 상태를 구분해 표시하는 내용이 해당 소스 문서에 기록되어 있습니다.

## CCTV 화면의 버전 구분

제가 담당한 기능으로는 요청에 따라 지도 위 CCTV 지점을 선택해 현장 영상을 볼 수 있도록 한 작업이 있습니다. 다만 이번에 연결한 소스 버전은 모자이크된 CCTV 사진과 촬영 시각, 버튼을 통한 즉석 촬영·추정 결과를 설명합니다. 연속 실시간 영상 기능의 최종 소스 링크는 아직 확인 중입니다.

## 원본 저장소 실행

원본 저장소를 내려받은 후 루트에서 실행합니다.

```sh
python3 -m http.server 8766 --bind 127.0.0.1
```

브라우저에서 `http://localhost:8766/web/?mode=mock`을 엽니다. 실제 API는 별도 서버와 CORS 설정이 필요합니다. 지도와 라이브러리 CDN에 인터넷 연결이 필요합니다. 이 포트폴리오 폴더 자체는 서비스 소스를 포함하지 않습니다.

## 검증

원본에서 `node --test web/*.test.mjs`로 단위 검사를 실행할 수 있습니다. 기존 개발 문서에는 30개 테스트 통과 기록이 있습니다. 이번 문서 정리에서는 테스트를 다시 실행하지 않았습니다.

[전체 포트폴리오](../../README.md)
