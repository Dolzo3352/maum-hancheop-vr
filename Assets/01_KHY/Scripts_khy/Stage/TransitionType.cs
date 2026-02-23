/// <summary>
/// Stage 진입 시 전환 연출 방식.
/// </summary>
public enum TransitionType
{
    Fade,       // 화면 페이드 아웃 → 인
    Dissolve,   // 두 Stage가 겹치며 교차
    Physical    // 물리적 이동 (걸어서 다음 Stage로)
}
