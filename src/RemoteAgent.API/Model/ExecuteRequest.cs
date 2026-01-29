namespace RemoteAgent.WebAPI.Model
{
    public record ExecuteRequest
    {
        public required string TargetOS { get; set; } = string.Empty;
        public required string Version { get; set; } = string.Empty;
        public required string Command { get; set; } = string.Empty;
    }

}
