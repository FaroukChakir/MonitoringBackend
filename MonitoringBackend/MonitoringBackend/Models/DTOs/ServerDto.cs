using System.ComponentModel.DataAnnotations;

namespace MonitoringBackend.Models.DTOs
{
    public class ServerDto
    {
        public string Host_IP { get; set; } = string.Empty;
        public string Ssh_User_Login { get; set; } = string.Empty;
    }
}





