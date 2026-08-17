namespace KeepCoreSafe.Analytics
{
    public static class AnalyticsEventIds
    {
        public const string TutorialStarted = "funnel:tutorial:started";
        public const string TutorialCompleted = "funnel:tutorial:completed";
        public const string TutorialException = "tutorial:exception";
        public const string PrologueStarted = "funnel:prologue:started";
        public const string PrologueCompleted = "funnel:prologue:completed";
        public const string GameStarted = "funnel:game:started";
        public const string GameOver = "funnel:game:over";
        public const string GracefulExit = "session:game:graceful_exit";
        public const string GameAbandoned = "session:game:abandoned";
        public const string RerollUsed = "gameplay:reroll:used";
        public const string OfferSelected = "gameplay:offer:selected";
        public const string MergePerformed = "gameplay:merge:performed";
    }

    public static class AnalyticsProgressions
    {
        public const string Tutorial = "tutorial";
        public const string Wave = "wave";
    }

    public static class AnalyticsTutorialSteps
    {
        public const string Introduction = "introduction";
        public const string AttackMerge = "attack_merge";
        public const string HealerLesson = "healer_lesson";
        public const string DefenseWave = "defense_wave";
    }

    public static class AnalyticsExceptionTypes
    {
        public const string WrongFirstPlacement = "wrong_first_placement";
        public const string WrongColorSelected = "wrong_color_selected";
        public const string LilyCellPlacement = "lily_cell_placement";
        public const string InvalidDismantle = "invalid_dismantle";
    }

    public static class AnalyticsFields
    {
        public const string StepId = "step_id";
        public const string ExceptionType = "exception_type";
        public const string WaveNumber = "wave_number";
        public const string WaveId = "wave_id";
        public const string WaveType = "wave_type";
        public const string ClearType = "clear_type";
        public const string GameOverType = "game_over_type";
        public const string EnemyCount = "enemy_count";
        public const string PlannedEnemyCount = "planned_enemy_count";
        public const string RequiredEnergy = "required_energy";
        public const string CurrentEnergy = "current_energy";
        public const string RerollCount = "reroll_count";
        public const string RerollCost = "reroll_cost";
        public const string OfferId = "offer_id";
        public const string BlockId = "block_id";
        public const string SourceBlockCount = "source_block_count";
        public const string BoardBlockCount = "board_block_count";
        public const string BoardBasicCount = "board_basic_count";
        public const string BoardSkillCount = "board_skill_count";
        public const string CoreHealthRatio = "core_health_ratio";
        public const string Phase = "phase";
    }
}
