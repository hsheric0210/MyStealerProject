# MyStealer.AntiDebug - 샌드박스, 디버거, 가상환경 감지 및 방지

안티바이러스, 방화벽 등과 같은 보안 프로그램들이나 리버스 엔지니어의 디버거, 덤퍼 등이 프로그램을 열거나 분석하는 것을 감지하고 이를 막습니다.

인라인 어셈블리(__asm), SEH (__try, __except)를 이용해서 구현되는 검사들과 같이 C# 단에서 구현이 불가능한 검사들은 `MyStealer.AntiDebug.Native` 모듈에서 구현됩니다.

Anti-Debug 관련 모듈들은 안티바이러스가 의심하기 딱 좋은 대상들이므로 반드시 난독화하여야 합니다.

특히, C# 구현 부분은 메인 실행 파일에 다같이 통합되에 한 번에 난독화가 된다고 하더라도, Native DLL 부분(MyStealer.AntiDebug.Native)은 또 따로 난독화와 보호를 걸어 줘야 합니다.

추천하는 보호 프로그램:

* VMProtect
* Themida
* WinLicense

UPX는 이미 안티바이러스가 쉽게 해제할 수 있기에 그리 추천되지 않습니다. (https://upx.github.io/ Overview란 참고)

## TODO

* 최대한 많은 부분은 C# 단에서 구현하기.
* 진짜로 C/C++의 기능을 사용하지 않으면 구현할 수 없는 부분만 `.Native` 단으로 빼내기.
* `.Native` 모듈의 DLLExport를 최대한 줄이기. (디텍을 피하기 위해 함수 이름을 사용자가 자유롭게 바꿀 수 있도록 돕기 위함)
  * 하나의 함수 당 64개의 검사를 수행하고, 성공한 검사와 실패한 검사를 uint64 형에 비트플래그 형태로 반환하면 될 것 같음.
  * .NET 쪽에서 불러올 때도 DLL 다이나믹 로딩 형태로, GetProcAddress로 불러와 쓰도록 하기.

## 참고 레포지토리

* https://github.com/LordNoteworthy/al-khaser
* https://github.com/HackOvert/AntiDBG
* https://github.com/ThomasThelen/Anti-Debugging
* https://github.com/revsic/AntiDebugging
* https://github.com/CheckPointSW/showstopper
* https://github.com/AdvDebug/AntiCrack-DotNet
