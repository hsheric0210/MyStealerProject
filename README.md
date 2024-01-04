# MyStealer Project - C# 기반 경량 다용도 맬웨어

초창기에는 단순히 브라우저의 쿠키 등을 훔치는 가벼운 그래버(Grabber)로 계획되었지만, 점점 더 범위가 넓어져 Anti-VM, 원격 셸 등의 다양한 기능들이 추가 예정에 있습니다.

## 모듈 목록

* MyStealer - 개인 정보 크롤러 모듈; 추후 MyStealer.Collector로 분리할 예정
* MyStealer.AntiDebug - 샌드박스, 디버거 및 가상 환경 감지 및 방지 모듈
* MyStealer.AntiDebug.Native - MyStealer.AntiDebug에서 실행할 수 없는 네이티브 코드들을 담고 있는 C DLL입니다. 런타임에 랜덤한 이름으로 생성된 후 다이나믹 로드됩니다. AntiDebug 개발 중 가장 큰 목표는 바로 이 네이티브 코드들(어셈블리어 등)을 진짜 최소한으로 사용하는 것입니다. (C# 상에서 VirtualAlloc으로 메모리 할당 후 직접 셸코드 쓰고 실행시키는 방법까지도 고민중)
* MyStealer.MyShell
* MyStealer.Shared
* MyStealerToolbox - 이미 컴파일된 프로그램의 설정 파일 수정 유틸리티

## 사용 전 팁

안티바이러스 등의 감지를 피하기 위해 프로그램을 난독화하는 것을 강력하게 추천합니다!

한 가지의 난독화 프로그램만을 사용하기보다는, **서로 다른 여러 난독화 프로그램들을 함께 사용하는 것이** 큰 도움이 됩니다.
(물론, 난독화 프로그램이 사용하는 dnlib, Mono.Cecil 라이버르리 등에 오류를 발생시키는 일부 난독화 기능들은 처음, 중간 단계에서는 비활성화하고 맨 마지막 난독화 단계에서만 적용해야 합니다)

[유명한 .NET 난독화 프로그램 목록 (영문)](https://github.com/NotPrab/.NET-Obfuscator)

[유명한 난독화 및 패커 목록 (영문)](https://unprotect.it/category/packers/)

개인적으로 추천하는 난독화 프로그램 목록들:

* [BitMono](https://github.com/sunnamed434/BitMono)
* [ConfuserEx](https://github.com/mkaring/ConfuserEx)
* [LoGiC.NET](https://github.com/AnErrupTion/LoGiC.NET)
* [Obfuscar](https://github.com/obfuscar/obfuscar)
* [ASAF](https://github.com/Charterino/AsStrongAsFuck)
* [MindLated](https://github.com/Sato-Isolated/MindLated)

주의할 점은, 한 번 난독화한 후에는 더 이상 MyStealerToolbox를 통해 설정 파일을 변경할 수 없게 됩니다! (파일을 읽어들이는 과정에서 강력한 난독화에 의해 오류 발생)
이 점에 주의해 주세요.

또한 단순히 메인 프로그램을 덜렁 놔두기만 하는 대신 [donut](https://github.com/TheWover/donut)과 같은 셸코드 인젝터를 활용하거나, 페이로드를 암호화해 놓고 실행할 때는 간단한 전용 복호화 및 로더 프로그램을 사용하는 등의 다양한 수단을 활용하면 안티 바이러스를 더욱 쉽게 우회할 수 있습니다.
