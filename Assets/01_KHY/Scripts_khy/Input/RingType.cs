using UnityEngine;

/// <summary>
/// 링 충전 시스템의 색상 타입.
/// 대상 오브젝트별로 다른 색상의 링이 표시됩니다.
/// </summary>
public enum RingType
{
    None,
    Orange,      // 바위 / 지하 → 바위 파괴 / 두더지 소환
    Blue,        // 냇물 / 물  → 소용돌이 생성
    Grey,        // 구름        → 비 내리기 / 구름 소멸
    Persimmon,   // 감나무      → 나무 급성장 / 열매 맺기
    LightGreen,  // 억새풀      → 풀 급성장
    Gold,        // 매 (Hawk)   → 매 호출 / 아이템 전달
    DarkEarth,   // 두더지      → 두더지 상호작용
    PinkWarm     // 아이의 보따리 → 반전(진심) 에너지 추출
}

/// <summary>
/// RingType에 대응하는 색상을 제공하는 유틸리티.
/// </summary>
public static class RingTypeColors
{
    public static Color GetColor(RingType type)
    {
        return type switch
        {
            RingType.Orange     => new Color(1.0f, 0.55f, 0.15f),   // 오렌지
            RingType.Blue       => new Color(0.2f, 0.6f, 1.0f),     // 파란
            RingType.Grey       => new Color(0.7f, 0.7f, 0.75f),    // 회색
            RingType.Persimmon  => new Color(1.0f, 0.45f, 0.1f),    // 주황 (감)
            RingType.LightGreen => new Color(0.5f, 0.9f, 0.3f),     // 연두
            RingType.Gold       => new Color(1.0f, 0.85f, 0.2f),    // 금빛
            RingType.DarkEarth  => new Color(0.45f, 0.3f, 0.15f),   // 짙은 흙색
            RingType.PinkWarm   => new Color(1.0f, 0.6f, 0.7f),     // 따뜻한 분홍
            _                   => Color.white
        };
    }
}
