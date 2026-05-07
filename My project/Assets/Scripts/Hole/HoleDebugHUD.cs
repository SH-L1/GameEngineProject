using UnityEngine;

namespace VoidEater.Hole
{
    public sealed class HoleDebugHUD : MonoBehaviour
    {
        [SerializeField] private HoleBase target;
        [SerializeField, Min(12)] private int fontSize = 82;
        [SerializeField, Range(0f, 1f)] private float topPositionRatio = 0.99f;
        [SerializeField, Range(0.1f, 1f)] private float widthRatio = 0.9f;

        private GUIStyle scoreStyle;

        private void OnGUI()
        {
            if (target == null)
            {
                return;
            }

            EnsureStyle();

            float width = Screen.width * widthRatio;
            float y = Screen.height * (1f - topPositionRatio);
            float height = fontSize * 1.6f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, y, width, height);
            GUI.Label(rect, target.Score.ToString() + "pts", scoreStyle);
        }

        private void EnsureStyle()
        {
            if (scoreStyle != null && scoreStyle.fontSize == fontSize)
            {
                return;
            }

            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
        }
    }
}
