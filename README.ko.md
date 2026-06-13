# SWeetJelly Serialization

SWeetJelly Serialization은 Unity 프로젝트에서 사용할 수 있는 작은 직렬화 서비스 패키지다.

텍스트 직렬화, persistent data path 기반 파일 저장, 선택적 문자열 암호화, Initialiser 기반 서비스 등록을 제공한다.

## 구조

```text
Runtime
├── Contract
│   ├── ISerializationService
│   ├── ITextSerializer
│   ├── IBinarySerializer
│   ├── IStringEncryptor
│   ├── IFileStorage
│   ├── IPathProvider
│   └── SerializationFormat
├── App
│   ├── SerializationService
│   ├── JsonDataSerializer
│   ├── XmlDataSerializer
│   ├── BinaryDataSerializer
│   └── AesStringEncryptor
├── Framework
│   ├── UnityFileStorage
│   ├── UnityPersistentDataPathProvider
│   └── UnityResourcesReader
└── InitialiserIntegration
    └── SerializationInitModule
```

## 책임

### Contract

외부에서 사용할 공개 직렬화 API를 정의한다.

- `ISerializationService`
- `ITextSerializer`
- `IBinarySerializer`
- `IStringEncryptor`
- `IFileStorage`
- `IPathProvider`
- `SerializationFormat`

### App

Unity에 의존하지 않는 직렬화 흐름과 기본 serializer 구현을 포함한다.

`SerializationService`의 책임은 다음과 같다.

- 값을 텍스트로 직렬화한다.
- 텍스트를 값으로 역직렬화한다.
- 직렬화된 byte 데이터를 특정 경로에 저장한다.
- 특정 경로에서 byte 데이터를 읽어 역직렬화한다.
- persistent data path 기준 파일 경로를 구성한다.
- secure key가 제공되면 선택적으로 암호화를 적용한다.

기본 구현은 다음과 같다.

- Newtonsoft Json 기반 `JsonDataSerializer`
- `System.Xml.Serialization.XmlSerializer` 기반 `XmlDataSerializer`
- secure text 직렬화를 위한 `AesStringEncryptor`
- placeholder 상태의 `BinaryDataSerializer`

### Framework

Unity에 직접 의존하는 adapter를 포함한다.

- `UnityFileStorage`는 파일 읽기, 쓰기, 존재 확인, 삭제를 처리한다.
- `UnityPersistentDataPathProvider`는 `Application.persistentDataPath`를 제공한다.
- `UnityResourcesReader`는 Unity `Resources`의 `TextAsset` 데이터를 읽는다.

### Initialiser Integration

`SerializationInitModule`은 Serialization 패키지를 Initialiser 시스템에 설치한다.

모듈은 다음 객체들을 생성한다.

- `UnityPersistentDataPathProvider`
- `UnityFileStorage`
- `JsonDataSerializer`
- `XmlDataSerializer`
- `BinaryDataSerializer`
- `AesStringEncryptor`
- `SerializationService`

이후 서비스를 `ISerializationService`로 등록한다.

## Serialization Formats

`SerializationFormat`은 현재 다음 값을 정의한다.

```csharp
SerializationFormat.Binary
SerializationFormat.Xml
SerializationFormat.Json
```

JSON과 XML은 텍스트 직렬화를 지원한다.

Binary는 contract에는 포함되어 있지만, 현재 `BinaryDataSerializer`는 `NotSupportedException`을 던진다. 실제 binary serializer가 제공되기 전까지는 `SerializationFormat.Binary`를 사용하지 않는다.

## 기본 사용법

애플리케이션 context에서 `ISerializationService`를 resolve한 뒤 데이터를 직렬화한다.

```csharp
using SWeetJelly.Initialiser.Framework;
using SWeetJelly.Serialization.Contract;

var service = InitialiserBehaviour.Services.Resolve<ISerializationService>();

var data = new PlayerData
{
    Level = 3,
    Name = "Sweet"
};

var json = service.SerializeToText(data, SerializationFormat.Json);

if (json.IsFailure)
    return;

var loaded = service.DeserializeFromText<PlayerData>(json.Value, SerializationFormat.Json);

if (loaded.IsSuccess)
{
    var player = loaded.Value;
}
```

## Persistent Data Path

Unity `Application.persistentDataPath` 아래에 파일을 저장해야 한다면 persistent data path helper를 사용한다.

```csharp
var saveResult = await service.SaveToPersistentDataPathAsync(
    data,
    "player-data.json",
    SerializationFormat.Json,
    cancellationToken);

if (saveResult.IsFailure)
    return;

var loaded = await service.LoadFromPersistentDataPathAsync<PlayerData>(
    "player-data.json",
    SerializationFormat.Json,
    cancellationToken);

if (loaded.IsSuccess)
{
    var player = loaded.Value;
}
```

같은 파일에 대해 존재 확인이나 삭제도 처리할 수 있다.

```csharp
if (service.ExistsAtPersistentDataPath("player-data.json"))
{
    service.DeleteAtPersistentDataPath("player-data.json");
}
```

## Custom Paths

다른 시스템이 저장 위치를 관리한다면 명시적인 경로 API를 사용한다.

```csharp
var saveResult = await service.SaveToPathAsync(
    data,
    fullPath,
    SerializationFormat.Json,
    secureKey: "",
    ct: cancellationToken);

if (saveResult.IsFailure)
    return;

var loaded = await service.LoadFromPathAsync<PlayerData>(
    fullPath,
    SerializationFormat.Json,
    secureKey: "",
    ct: cancellationToken);
```

## Secure Text

비어 있지 않은 secure key를 전달하면 직렬화된 텍스트를 암호화한다.

```csharp
var secureText = service.SerializeToText(
    data,
    SerializationFormat.Json,
    secureKey: "local-save-key");

if (secureText.IsFailure)
    return;

var loaded = service.DeserializeFromText<PlayerData>(
    secureText.Value,
    SerializationFormat.Json,
    secureKey: "local-save-key");
```

직렬화와 역직렬화에는 같은 secure key를 사용해야 한다.

secure key가 제공되었지만 `IStringEncryptor`가 할당되어 있지 않으면 `SerializationService`는 예외를 던진다.

## Initialiser Setup

1. 다음 메뉴에서 `SerializationInitModule`을 만든다.

```text
Create > SWeetJelly > Initialiser > Modules > Serialization
```

2. 생성한 module asset을 `ProjectInitSettingSo`에 추가한다.
3. 해당 setting을 `InitialiserBehaviour`에 할당한다.
4. Initialiser 시작 이후 `ISerializationService`를 resolve한다.

```csharp
using SWeetJelly.Initialiser.Framework;
using SWeetJelly.Serialization.Contract;

var serialization = InitialiserBehaviour.Services.Resolve<ISerializationService>();
```

## Custom Implementations

`SerializationService`는 contract에 의존하므로, 테스트나 프로젝트에서 infrastructure 구현을 교체할 수 있다.

교체 가능한 지점은 다음과 같다.

- `IFileStorage`
- `IPathProvider`
- `ITextSerializer`
- `IBinarySerializer`
- `IStringEncryptor`

예시:

```csharp
var service = new SerializationService(
    fileStorage,
    pathProvider,
    jsonSerializer,
    xmlSerializer,
    binarySerializer,
    encryptor);
```

## 설계 메모

- 공개 API는 `SWeetJelly.Serialization.Contract`를 통해 노출한다.
- Unity 파일 시스템 세부 구현은 `SWeetJelly.Serialization.Framework`에 둔다.
- `SerializationService`는 Unity API에 의존하지 않는다.
- 소비자는 concrete serializer 구현이 아니라 `ISerializationService`에 의존하는 것이 좋다.
- Save, Loading, gameplay 시스템은 Serialization을 infrastructure service로 다룬다.

## 현재 상태

현재 이 패키지는 다음을 제공한다.

- JSON 텍스트 직렬화
- XML 텍스트 직렬화
- 선택적 AES 문자열 암호화
- persistent data path 저장/로드 helper
- 명시적 경로 저장/로드 helper
- Unity 파일 저장 adapter
- Unity persistent data path adapter
- Resources 텍스트/byte reader
- Initialiser 연동

Binary 직렬화는 아직 구현되어 있지 않다.
