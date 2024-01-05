# MyStealer - 개인 정보 크롤러(그래버)

사용자의 컴퓨터에 저장되어 있는 계정 정보, 세션 토큰 등을 싸그리 털어갑니다.

흔히 '토큰 털렸다'라는 말이 나오거나, '세션 하이재킹' 공격을 위해 정보 수집하는 툴이 바로 이러한 종류의 것입니다.

많은 부분을 GitHub에 널린 Grabber, Stealer류 프로그램들과 [Quasar RAT](https://github.com/quasar/Quasar), [Metasploit Framework Payloads](https://github.com/rapid7/metasploit-payloads)의 `post/windows/gather/credentials/*` 페이로드들을 참고하고 있습니다.

https://aluigi.altervista.org/pwdrec.htm

## 지원 브라우저

브라우저에 저장된 계정 정보, 쿠키 및 로컬 저장소의 데이터를 남김없이 싸그리 털어옵니다.

* [x] Chromium 기반<sub>Quasar</sub>
  * [x] Brave
  * [x] Google Chrome
  * [x] Discord<sub>브라우저아님</sub>
  * [x] Iridium Browser
  * [x] Microsoft Edge
  * [x] 네이버 웨일
  * [x] Opera (stable)
  * [x] Yandex Browser
* [x] FireFox 기반<sub>Quasar</sub>
  * [x] LibreWolf
  * [x] Pale Moon
  * [x] ThunderBird<sup>브라우저아님</sup>
  * [x] Waterfox

\* 브라우저아님 : 비록 브라우저는 아니지만 CEF로 구현되어 있고, 브라우저가 계정 정보를 저장하는 것과 동일한 방식으로 세션 토큰을 저장하기에 목록에 포함되었습니다.

## 지원 SSH/FTP 클라이언트

* [x] FileZilla FTP Client<sub>Quasar</sub>
* [ ] SmartFTP<sub>Metasploit</sub>
* [x] WinSCP<sub>Quasar</sub> - 만약 인증서 로그인을 사용하고 해당 인증서 파일을 로컬에서 찾을 수 있다면 인증서 데이터 전체를 비밀번호에 저장합니다
* [ ] Bulletproof FTP<sub>Metasploit</sub>
* [ ] CoreFTP<sub>Metasploit</sub>

## 메신저 프로그램

* [ ] 카카오톡

## 지원 기타 프로그램

* [ ] KeePass<sub>KeeFarce</sub>
* [ ] DynDNS<sub>Metasploit</sub>
* [ ] Steam - 비밀번호 대신 ConnectCache가 추출됩니다
