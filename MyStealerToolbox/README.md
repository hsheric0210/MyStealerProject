# MyStealerToolbox - 툴박스 유틸리티

완성된 프로그램을 난독화하기 전에 내부 설정 파일을 수정하거나, 프로그램이 내보낸 암호화된 로그 파일이나 출력 파일들을 복호화하는 것을 지원하는 유틸리티입니다.

## 커맨드 라인 옵션

### config - 내부 설정 파일 수정

`MyStealerToolbox.exe config <옵션>`

몹션

|짧은 옵션 이름|긴 옵션 이름|설명|필수 여부|
|:---|:---|:---|:---:|
|`-x`|`--executable`|프로그램 실행 파일 경로|O|
|`-w`|`--webhook-url`|데이터를 보낼 웹훅 주소|O|
|`-e`|`--encryption-key`|프로그램이 로그 및 출력 파일을 암호화할 때 사용할 키|X|

예시: `MyStealerToolbox.exe config -x "D:\mystealer.exe" -w "https://***" -e "MyEncryptionKey"`

### decrypt - 프로그램 출력 파일 복호화

`MyStealerToolbox.exe decrypt <옵션>`

|짧은 옵션 이름|긴 옵션 이름|설명|필수 여부|
|:---|:---|:---|:---:|
|`-i`|`--input`|복호화할 파일 경로|O|
|`-e`|`--encryption-key`|프로그램이 로그 및 출력 파일을 암호화할 때 사용할 키|O|
|`-o`|`--output`|복호화가 완료된 파일이 저장될 경로|O|

예시: `MyStealerToolbox.exe decrypt -i "D:\mystealer.log" -e "MyStealerKey" -o "D:\mystealer.log.dec"`
