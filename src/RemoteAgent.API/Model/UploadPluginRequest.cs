namespace RemoteAgent.WebAPI.Model
{
    public record UploadPluginRequest
    {
        public required string DllFile { get; set; }

        public required string Name { get; set; }
    }
}


