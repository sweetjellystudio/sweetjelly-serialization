# Serialization Tests

이 폴더는 `com.sweetjelly.serialization` 패키지를 현재 Unity 테스트 프로젝트에서 검증하기 위한 테스트를 담는다.

## EditMode

`EditMode` 테스트는 Unity 파일 시스템에 직접 의존하지 않도록 fake path provider와 memory file storage를 사용한다.

- `SerializationServiceTest`
  - JSON 직렬화와 역직렬화 round trip을 검증한다.
  - XML 직렬화와 역직렬화 round trip을 검증한다.
  - persistent data path 저장/불러오기 흐름을 memory storage로 검증한다.
  - secure key를 사용한 JSON 암호화/복호화 흐름을 검증한다.
  - Binary format의 text serialization이 지원되지 않아 `NotSupportedException`을 발생시키는지 검증한다.

- `SerializationPackageIntegrationTest`
  - `SerializationPackageBootstrapperSo`이 `ISerializationService`를 `ServiceRegistry`에 등록하는지 검증한다.

## PlayMode

현재 serialization 패키지에는 PlayMode 테스트를 두지 않았다.

실제 `Application.persistentDataPath`, Unity `Resources`, 플랫폼별 파일 권한까지 검증해야 할 때 `PlayMode` 테스트를 추가한다.
