namespace Mori
{
    /// <summary>
    /// 云函数与经分配置，对齐 rosa-env 共用环境规范。
    /// GameKey、云函数名、CLOUD_SYNC_ALLOWLIST 三处统一为 mori。
    /// </summary>
    public static class CloudConfig
    {
        public const string GameKey = "mori";
        public const string BaseUrl = "https://rosa-env-d7grf78r5dbd37323.service.tcloudbase.com";
        const string Prefix = "/" + GameKey + "-api";

        public const string LoginPath = Prefix + "/login";
        public const string PullPath = Prefix + "/save/pull";
        public const string PushPath = Prefix + "/save/push";
        public const string RankSubmitPath = Prefix + "/rank/submit";
        public const string RankListPath = Prefix + "/rank/list";
        public const string AnalyticsPath = BaseUrl + "/analytics-ingest/track";

        public const int SchemaVersion = 1;
        public const float StartupTimeout = 6f;
        public const float DebounceSec = 1.5f;

        public const string SaveKey = GameKey + "_save_v1";
        public const string TokenKey = GameKey + "_token";
        public const string SyncMetaKey = GameKey + "_cloud_meta";
    }
}
