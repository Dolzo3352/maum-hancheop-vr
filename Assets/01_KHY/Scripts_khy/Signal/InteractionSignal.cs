using UnityEngine;
using UnityEngine.Timeline;

/// <summary>
/// 인터랙션 대기 Signal 에셋.
///
/// 타임라인에 이 Signal을 배치하면, 해당 시점에서 타임라인이 일시정지되고
/// 플레이어의 인터랙션을 기다립니다.
///
/// 사용법:
///   1. Project 창에서 우클릭 → Create → DearBrave → InteractionSignal
///   2. 생성된 Signal을 타임라인의 Signal Track에 드래그
///   3. 해당 스테이지의 SignalReceiver에서 InteractionSignalHandler의 메서드를 연결
/// </summary>
[CreateAssetMenu(menuName = "DearBrave/Interaction Signal", fileName = "InteractionSignal")]
public class InteractionSignal : SignalAsset
{
    // SignalAsset을 상속하여 타임라인에서 사용 가능한 Signal 에셋을 만듭니다.
    // 데이터는 SignalReceiver 쪽(InteractionSignalHandler)에서 관리합니다.
    //
    // 참고: 하나의 InteractionSignal 에셋을 여러 타임라인에서 재사용합니다.
    // "바위 파괴 시그널", "감나무 시그널" 같이 여러 개 만들 필요 없이
    // 하나만 만들고, 받는 쪽에서 어떤 오브젝트를 대상으로 할지 지정합니다.
}
