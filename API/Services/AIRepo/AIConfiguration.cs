namespace API.Services.AIRepo
{
    public class AIConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 300;
        public float Temperature { get; set; } = 0.7f;

        public bool IsValid => !string.IsNullOrEmpty(ApiKey) && (ApiKey.StartsWith("sk-") || ApiKey.StartsWith("sk-proj-"));
    }
}
