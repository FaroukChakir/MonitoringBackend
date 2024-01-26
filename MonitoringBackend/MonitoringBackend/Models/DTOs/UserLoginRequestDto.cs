using System.ComponentModel.DataAnnotations;

namespace MonitoringBackend.Models.DTOs
{
    public class UserLoginRequestDto
    {
        [Required]
        public string User_Login { get; set; } = string.Empty;

        [Required]
        public string User_Password { get; set; } = string.Empty;



    }
}
