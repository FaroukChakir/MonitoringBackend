using System.ComponentModel.DataAnnotations;

namespace MonitoringBackend.Models.DTOs
{
    public class ConnectServerDto
    {
        public string Ip_Host { get; set; } = string.Empty;
        public string Ssh_Password { get; set; } = string.Empty;
    }
}





