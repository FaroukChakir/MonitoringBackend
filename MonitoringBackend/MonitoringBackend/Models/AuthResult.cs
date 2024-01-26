namespace MonitoringBackend.Models
{

    public class AuthResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public bool Result { get; set; }
        public List<string> Errors { get; set; }

    }
}
