using System;

namespace TaekwondoScoreboard
{
    public enum ActionType
    {
        Punch1,
        Body2,
        Head3,
        SpinHead4,
        KyongGo,
        GamJeom
    }

    public sealed class JudgeAction
    {
        public int Judge { get; set; }

        public string Side { get; set; }

        public ActionType Action { get; set; }

        public DateTime TimeUtc { get; set; }
    }

    public sealed class ScoreState
    {
        public int Round { get; set; }

        public double BlueScore { get; set; }

        public double RedScore { get; set; }

        public int BlueRounds { get; set; }

        public int RedRounds { get; set; }

        public int SecondsRemaining { get; set; }

        public string Status { get; set; }

        public bool MatchActive { get; set; }

        public bool Resting { get; set; }

        public bool GoldenPoint { get; set; }
    }
}