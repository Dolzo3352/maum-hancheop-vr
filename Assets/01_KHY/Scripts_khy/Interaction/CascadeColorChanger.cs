using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 여러 오브젝트에 붙은 MaterialColorChanger를 캐스케이드(물결)로 순차 호출.
///
/// 각 오브젝트에 이미 MaterialColorChanger가 붙어있어야 합니다.
/// (targetColor, duration, propertyName 등은 각자의 설정을 따름)
///
/// Timeline Signal에서 StartCascade()를 호출하면
/// 리스트 순서대로 간격을 두고 StartTransition()이 호출됩니다.
///
/// 사용 예:
///   - 계단식 지형 레이어들이 위→아래로 색상 전환
///   - 방 안 오브젝트들이 안→밖으로 황금빛 전환
/// </summary>
public class CascadeColorChanger : MonoBehaviour
{
    [Header("대상")]
    [Tooltip("순서대로 캐스케이드할 MaterialColorChanger 목록")]
    [SerializeField] private MaterialColorChanger[] targets;

    [Header("타이밍")]
    [Tooltip("다음 오브젝트 시작까지의 간격 (초). 작을수록 물결이 빠름")]
    [SerializeField] private float delayBetween = 0.15f;

    [Header("이벤트")]
    [Tooltip("마지막 오브젝트의 전환 시작 후 호출")]
    [SerializeField] private UnityEvent onAllStarted;

    /// <summary>
    /// 캐스케이드 색상 전환 시작. Timeline Signal에서 호출.
    /// </summary>
    public void StartCascade()
    {
        if (targets == null || targets.Length == 0) return;
        StartCoroutine(CascadeCoroutine());
    }

    private IEnumerator CascadeCoroutine()
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
                targets[i].StartTransition();

            if (i < targets.Length - 1)
                yield return new WaitForSeconds(delayBetween);
        }

        onAllStarted?.Invoke();
    }
}
