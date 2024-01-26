namespace MonitoringBackend.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string User_Id { get; set; }
        public string Token { get; set;}
        public string JwId { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }

        public DateTime AddedDate { get; set; }
        public DateTime ExpiryDate { get; set;}
    }
}
