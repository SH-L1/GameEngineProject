using System;
using UnityEngine;
using VoidEater.Hole;

namespace VoidEater.Core
{
    public sealed class HoleGameController : MonoBehaviour
    {
        private const string BestScoreKey = "VoidEater.BestScore";

        [SerializeField] private HoleBase player;
        [SerializeField, Min(10f)] private float roundDuration = 90f;
        [SerializeField] private bool startOnAwake;

        private float remainingTime;
        private bool running;
        private bool finished;

        public event Action GameStarted;
        public event Action GameFinished;

        public HoleBase Player => player;
        public float RemainingTime => remainingTime;
        public float RoundDuration => roundDuration;
        public bool IsRunning => running;
        public bool IsFinished => finished;
        public bool IsWaitingToStart => !running && !finished;
        public int FinalScore => player != null ? player.Score : 0;
        public int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);

        private void Awake()
        {
            remainingTime = roundDuration;
            if (startOnAwake)
            {
                StartGame();
            }
        }

        private void Update()
        {
            if (!running)
            {
                return;
            }

            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            if (remainingTime <= 0f)
            {
                FinishGame();
            }
        }

        public void SetPlayer(HoleBase nextPlayer)
        {
            player = nextPlayer;
        }

        public void StartGame()
        {
            remainingTime = roundDuration;
            finished = false;
            running = true;
            Time.timeScale = 1f;
            GameStarted?.Invoke();
        }

        public void FinishGame()
        {
            if (finished)
            {
                return;
            }

            remainingTime = 0f;
            running = false;
            finished = true;
            SaveBestScore();
            GameFinished?.Invoke();
        }

        private void SaveBestScore()
        {
            int finalScore = FinalScore;
            if (finalScore <= BestScore)
            {
                return;
            }

            PlayerPrefs.SetInt(BestScoreKey, finalScore);
            PlayerPrefs.Save();
        }
    }
}
