# SWeetJelly Serialization

Unity 6.3 LTS용 직렬화 서비스다. JSON/XML, 선택적 인증 암호화, 파일 IO adapter와 Bootstrapper 등록을 제공한다. 게임 저장 모델·저장 주기·원자적 파일 교체·키 보관 정책은 소비자가 소유한다.

## 설치와 사용

공개 2.0.0 태그 게시 후 소비 프로젝트 manifest에 serialization·bootstrapper·shared Git URL을 각각 명시한다. 정확한 JSON과 직접 생성 예제는 [영문 README][readme]가 소유한다. Newtonsoft.Json 3.2.2와 Bootstrapper가 요구하는 uGUI는 UPM으로 해석한다. 기존 프로젝트의 다른 의존성을 덮어쓰지 않는다. 루트 [LICENSE][license]는 MIT다.

일반 소비자는 Contract의 ISerializationService를 조립 지점에서 받는다. 직접 생성할 때에는 App의 SerializationService와 Framework.Serialization의 JsonDataSerializer/XmlDataSerializer/AesStringEncryptor, 필요한 Framework.Unity adapter를 조합한다. 결과의 IsSuccess를 확인한 후 Value를 사용한다.

## 2.0의 변경

*   구체 codec/encryptor는 App namespace/assembly에서 Framework.Serialization로 이동했다. 서비스와 포트의 기존 공개 메서드는 유지한다.
*   기본 Binary placeholder는 제거했다. Binary=0, Xml=1, Json=2는 유지하며 사용자 IBinarySerializer로 Binary 파일 기능을 제공할 수 있다. Binary text와 Binary+secureKey는 실패한다.
*   새 암호문은 버전·무작위 salt/IV·변조 검증을 포함한다. 기존 암호문은 읽을 수 있으며 명시적 재저장 시 새 형식을 쓴다. 읽기만으로 파일을 덮어쓰지 않는다. 새 형식은 1.x에서 읽을 수 없다.
*   XML 파일도 secureKey를 적용한다. 1.x에서 키가 무시돼 평문으로 저장된 XML은 키 없이 읽은 후 필요한 경우 새로 암호화 저장한다. 복호화 실패 후 평문 fallback은 하지 않는다.
*   사전 취소는 작업 전에 확인하고 비동기 취소는 OperationCanceledException으로 전파한다. 일반 직렬화·경로·IO 실패는 Result, Exists/Delete의 동기 오류는 예외다. 진행 중인 동기 codec/KDF를 토큰이 중단하지는 못한다.
*   Bootstrapper 종료는 자신이 등록한 동일 인스턴스만 해제한다. 외부 adapter dispose나 진행 중 요청 취소로 해석하지 않는다. bootstrap asset instance 하나는 활성 등록 하나만 소유한다.

자세한 이전 절차와 모델 보존은 [migration][migration], 암호화 형식·legacy 한계·동기 KDF 비용은 [encryption][encryption]을 읽는다. 기존 암호문에는 인증이 없으므로 새 형식과 같은 변조 검증을 보장하지 않는다. 게임 모델 자체의 필드 변경을 자동 이전하지 않는다.

## 테스트와 sample

Package Manager에서 Basic sample을 가져오고 [sample 안내][sample]를 따른다. IL2CPP reflection에는 실제 DTO와 converter 보존이 필요하며 sample link.xml은 sample 모델만 보존한다. [테스트 안내][tests]와 [아키텍처][architecture]를 참고한다. 호스트·격리·Player 결과와 미검증 플랫폼을 구분한다.

[readme]: README.md
[license]: LICENSE.md
[migration]: docs/migration-2.0.md
[encryption]: docs/encryption-format.md
[sample]: Samples~/Basic/README.md
[tests]: Tests/README.md
[architecture]: ARCHITECTURE.md
