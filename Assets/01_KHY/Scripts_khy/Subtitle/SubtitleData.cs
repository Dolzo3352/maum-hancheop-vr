using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지별 자막 데이터.
///
/// 각 스테이지의 타임라인 오디오에 맞춰 자막 텍스트와 시간을 저장합니다.
/// entries는 startTime 기준 오름차순으로 정렬되어야 합니다.
///
/// 사용법:
///   Project 창 우클릭 → Create → DearBrave → SubtitleData
/// </summary>
[CreateAssetMenu(fileName = "SubtitleData", menuName = "DearBrave/SubtitleData")]
public class SubtitleData : ScriptableObject
{
    [Tooltip("자막 목록 (startTime 기준 오름차순)")]
    public List<SubtitleEntry> entries = new List<SubtitleEntry>();
}

/// <summary>
/// 개별 자막 항목. 시작/끝 시간과 한국어/영어 텍스트를 가집니다.
/// </summary>
[System.Serializable]
public class SubtitleEntry
{
    [Tooltip("자막 시작 시간 (초) — 타임라인 기준")]
    public float startTime;

    [Tooltip("자막 끝 시간 (초) — 타임라인 기준")]
    public float endTime;

    [TextArea(2, 4)]
    [Tooltip("한국어 자막")]
    public string koText;

    [TextArea(2, 4)]
    [Tooltip("영어 자막")]
    public string enText;
}
