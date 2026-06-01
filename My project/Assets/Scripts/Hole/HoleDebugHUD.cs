using UnityEngine;
using VoidEater.Core;
using VoidEater.Objects;
using VoidEater.Player;

namespace VoidEater.Hole
{
    public sealed class HoleDebugHUD : MonoBehaviour
    {
        [SerializeField] private HoleBase target;
        [SerializeField] private HoleGameController gameController;
        [SerializeField, Min(12)] private int fontSize = 82;
        [SerializeField, Range(0f, 1f)] private float topPositionRatio = 0.99f;
        [SerializeField, Range(0.1f, 1f)] private float widthRatio = 0.9f;
        [SerializeField] private bool showSwallowDebug;
        [SerializeField, Min(10)] private int debugFontSize = 22;

        private GUIStyle scoreStyle;
        private GUIStyle debugStyle;
        private GUIStyle endStyle;
        private GUIStyle titleStyle;
        private GUIStyle panelStyle;
        private GUIStyle buttonStyle;

        private void Awake()
        {
            ResolveReferences();
        }

        public void SetTarget(HoleBase nextTarget)
        {
            target = nextTarget;
        }

        public void SetGameController(HoleGameController controller)
        {
            gameController = controller;
        }

        private void OnGUI()
        {
            ResolveReferences();
            if (target == null)
            {
                return;
            }

            EnsureStyle();
            if (gameController != null && gameController.IsWaitingToStart)
            {
                DrawStartScreen();
                return;
            }

            DrawScoreAndTimer();
            DrawDebug();
            DrawEndScreen();
        }

        private void DrawStartScreen()
        {
            Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.Box(overlay, GUIContent.none, panelStyle);

            float titleHeight = Mathf.Max(92f, Screen.height * 0.16f);
            Rect titleRect = new Rect(0f, Screen.height * 0.12f, Screen.width, titleHeight);
            GUI.Label(titleRect, "VOID EATER", titleStyle);

            float boardWidth = Mathf.Min(620f, Screen.width * 0.72f);
            float boardHeight = Mathf.Min(220f, Screen.height * 0.28f);
            Rect boardRect = new Rect((Screen.width - boardWidth) * 0.5f, Screen.height * 0.34f, boardWidth, boardHeight);
            string leaderboard =
                "LEADERBOARD\n" +
                "1. BEST SCORE  " + gameController.BestScore + "\n" +
                "CURRENT SCORE  " + target.Score;
            GUI.Label(boardRect, leaderboard, endStyle);

            float buttonWidth = Mathf.Min(360f, Screen.width * 0.52f);
            float buttonHeight = Mathf.Min(96f, Screen.height * 0.13f);
            Rect buttonRect = new Rect((Screen.width - buttonWidth) * 0.5f, Screen.height * 0.68f, buttonWidth, buttonHeight);
            if (GUI.Button(buttonRect, "PLAY", buttonStyle))
            {
                gameController.StartGame();
            }
        }

        private void DrawScoreAndTimer()
        {
            float width = Screen.width * widthRatio;
            float y = Screen.height * (1f - topPositionRatio);
            float height = fontSize * 1.6f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, y, width, height);
            string timer = gameController != null ? "  " + Mathf.CeilToInt(gameController.RemainingTime) + "s" : string.Empty;
            GUI.Label(rect, target.Score.ToString() + "pts" + timer, scoreStyle);
        }

        private void DrawDebug()
        {
            if (!showSwallowDebug)
            {
                return;
            }

            Swallowable candidate = target.DebugCandidate;
            string candidateName = candidate != null ? candidate.name : "None";
            float requiredRadius = candidate != null ? candidate.RequiredPassThroughRadius : 0f;
            bool canSwallow = candidate != null && target.CanSwallow(candidate);
            string text =
                "Hole R: " + target.Radius.ToString("0.00") +
                "\nTarget: " + candidateName +
                "\nTarget Required R: " + requiredRadius.ToString("0.00") +
                "\nCan Swallow: " + canSwallow;

            Rect rect = new Rect(24f, 24f, 480f, 140f);
            GUI.Label(rect, text, debugStyle);
        }

        private void DrawEndScreen()
        {
            if (gameController == null || !gameController.IsFinished)
            {
                return;
            }

            Rect rect = new Rect(0f, Screen.height * 0.36f, Screen.width, Screen.height * 0.28f);
            GUI.Label(rect, "TIME UP\nFINAL SCORE " + gameController.FinalScore, endStyle);
        }

        private void EnsureStyle()
        {
            if (scoreStyle != null && scoreStyle.fontSize == fontSize && debugStyle != null && debugStyle.fontSize == debugFontSize)
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

            debugStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = debugFontSize,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };

            endStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(fontSize, 92),
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(fontSize + 24, 108),
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal =
                {
                    background = Texture2D.blackTexture
                }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(44, fontSize - 12),
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                },
                hover =
                {
                    textColor = Color.cyan
                },
                active =
                {
                    textColor = Color.yellow
                }
            };
        }

        private void ResolveReferences()
        {
            if (target == null)
            {
                target = FindFirstObjectByType<HoleBase>();
            }

            if (gameController == null)
            {
                gameController = FindFirstObjectByType<HoleGameController>();
            }

            if (gameController == null && target != null)
            {
                GameObject controllerObject = new GameObject("Game Controller");
                gameController = controllerObject.AddComponent<HoleGameController>();
                gameController.SetPlayer(target);
            }

            if (gameController != null && gameController.Player == null && target != null)
            {
                gameController.SetPlayer(target);
            }

            if (target is PlayerHole playerHole)
            {
                playerHole.SetGameController(gameController);
            }
        }
    }
}
