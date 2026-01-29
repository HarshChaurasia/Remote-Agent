namespace RemoteAgent.WebAPI.Model
{
    public record ExecuteResponse
    {
        public bool Success { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

}
