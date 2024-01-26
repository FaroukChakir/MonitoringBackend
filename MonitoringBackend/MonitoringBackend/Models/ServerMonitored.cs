using System.ComponentModel.DataAnnotations;

namespace MonitoringBackend.Models
{
    public class ServerMonitored
    {
        public int Id { get; set; }
        public string Host_IP { get; set; } = string.Empty;
        public string Ssh_User_Login { get; set; } = string.Empty;
        public string User_Id { get; set; }
    }
}