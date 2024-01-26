using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringBackend.Data;
using MonitoringBackend.Models;
using MonitoringBackend.Models.DTOs;
using Renci.SshNet;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MonitoringBackend.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class SshManagementController : ControllerBase
    {
        private readonly SshManagementController sshManagementController;
        private readonly ILogger<SshManagementController> _logger;
        private readonly ApiDbContext _dbContext;
        public SshManagementController(ILogger<SshManagementController> logger,
            ApiDbContext apiDbContext)
        {
            _logger = logger;
            _dbContext = apiDbContext;
        }

        /************Add Server********/


        [HttpPost]
        [Route("AddServer")]
        public async Task<IActionResult> AddServer([FromBody] ServerDto server)
        {
            try
            {
                if (ModelState.IsValid)
                    {
                    string ipAddressPattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"; // im using this to define IP address input
                    if (!Regex.IsMatch(server.Host_IP, ipAddressPattern))
                    {
                        return BadRequest("Invalid IP address format");
                    }

                    var monitoredServer = new ServerDto()
                    {
                        Host_IP = server.Host_IP,
                        Ssh_User_Login = server.Ssh_User_Login,
                    };


                    var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    if (userId == null)
                    {
                        return BadRequest("Timeout userId");
                    }
                    


                    var addServer = new ServerMonitored()
                    {
                        Host_IP = monitoredServer.Host_IP,
                        Ssh_User_Login = monitoredServer.Ssh_User_Login,
                        User_Id = userId
                    };
                    var existingServer = GetServerByHostIp(addServer.Host_IP);
                    if (existingServer != null)
                    {
                        return BadRequest("Server exists already");
                    }

                    _dbContext.servers.Add(addServer);
                    var isSaved = _dbContext.SaveChanges();

                    if (isSaved == 0)
                    {
                        return BadRequest("Server exists already");
                    }

                    var ReturnedServer = new ServerDto()
                    {
                        Host_IP = addServer.Host_IP,
                        Ssh_User_Login = addServer.Ssh_User_Login
                    };
                    return Ok(ReturnedServer);
                }
                else
                {
                    return BadRequest("Bad request");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }


        /************ Get Servers ********/


        [HttpGet]
        [Route("GetServers")]
        public async Task<IActionResult> GetServers()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized("Timeout");
                }

                var userServers = await _dbContext.servers
                    .Where(x => x.User_Id == userId)
                    .Select(serverMonitored => new ServerDto
                    {
                        Host_IP = serverMonitored.Host_IP,
                        Ssh_User_Login = serverMonitored.Ssh_User_Login,
                    })
                    .ToListAsync();

                return Ok(userServers);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        /************Connect to Server********/

        [HttpPost]
        [Route("TestConnexion")]
        public async Task<IActionResult> TestConnexion([FromBody] ConnectServerDto server)
        {
            var serverCredentials = GetServerByHostIp(server.Ip_Host);
            if(serverCredentials==null)
            {
                return BadRequest("Server does not exist");

            }
            try
            {

            using (var client = new SshClient(serverCredentials.Host_IP, serverCredentials.Ssh_User_Login, server.Ssh_Password))
            {
                try
                {

                    client.Connect();

                    if (client.IsConnected)
                    {
                        client.Disconnect();
                        return StatusCode(200, "connextion established");
                    }
                    else
                    {
                        return BadRequest("Connextion not established");

                    }

                }
                catch (Exception ex)
                {
                    return StatusCode(500, "internal server error");
                }
            }
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Verify your IP and Password");
            }
        }

        /************ Find Server Function ********/

        private ServerDto GetServerByHostIp(string Ip)
        {
            var serverFound = _dbContext.servers.FirstOrDefault(x => x.Host_IP == Ip);

            if (serverFound == null)
            {
                return null;
            }

            var server = new ServerDto()
            {
                Host_IP = Ip,
                Ssh_User_Login = serverFound.Ssh_User_Login
            };

            return server;
        }


        /************Delete Server********/

        [HttpPost]
        [Route("RemoveServer")]
        public async Task<IActionResult> RemoveServer([FromBody] DeleteServer deleteServer)
        {
            try
            {
                var ServerToDelete = _dbContext.servers.FirstOrDefault(y => y.Host_IP == deleteServer.Ip_Host);
                if (ServerToDelete == null)
                { 
                        return BadRequest("Server not found");
                }
                _dbContext.servers.Remove(ServerToDelete);
                _dbContext.SaveChanges();
                return Ok("Server : "+ ServerToDelete.Host_IP + "was deleted successfuly");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        /************ Server Monitoring ********/
        [HttpPost]
        [Route("Monitoring")]
        public async Task<IActionResult> ServerMonitoring([FromBody] ConnectServerDto connectServerDto)
        {
            var serverCredentials = GetServerByHostIp(connectServerDto.Ip_Host);
            if (serverCredentials == null)
            {
                return BadRequest("Server does not exist");
            }
            Dictionary<string, string> commands = new Dictionary<string, string>
    {
        { "CPU", "top -bn1 | grep 'Cpu(s)' | awk '{print $2 + $4}'" },
        { "Ping Latency", "ping -c 4 google.com | tail -1| awk '{print $4}' | cut -d '/' -f 2" },
        { "Services", "systemctl list-units --type=service --all --no-pager" },
        { "RAM Used", "free -m | awk 'NR==2{printf \"%.2f%%\", $3*100/$2 }'" },
        { "Swap Used", "free -m | awk 'NR==3{printf \"%.2f%%\", $3*100/$2 }'" },
        { "Used Space", "df -h --output=used /home/"+ serverCredentials.Ssh_User_Login+"| sed -n 2p | tr -d '\n'" },
        { "Available Space", "df -h --output=avail /home/" + serverCredentials.Ssh_User_Login + " | sed -n 2p | tr -d '\n'" },
        { "Network Traffic In", "cat /proc/net/dev | awk '/:/ {print $2}' | paste -sd+ - | bc" },
        { "Network Traffic Out", "cat /proc/net/dev | awk '/:/ {print $10}' | paste -sd+ - | bc" }
    };



            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var kvp in commands)
            {
                string result = ExecuteSshCommand(serverCredentials.Host_IP, serverCredentials.Ssh_User_Login, connectServerDto.Ssh_Password, kvp.Value);
                results.Add(kvp.Key, result);
            }

            // to separate the "Services" from the general results
            List<Dictionary<string, string>> serviceInfoList = new List<Dictionary<string, string>>();
            if (results.ContainsKey("Services"))
            {
                // to process the services output to extract name and active status
                string servicesOutput = results["Services"];
                var serviceLines = servicesOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in serviceLines.Skip(1)) // Skip the header line
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        string serviceName = parts[0];
                        string isActive = parts[2].ToLower() == "active" ? "Active" : "Inactive";
                        serviceInfoList.Add(new Dictionary<string, string> { { "ServiceName", serviceName }, { "Activity", isActive } });
                    }
                }

                // to remove "Services" from the general results
                results.Remove("Services");
            }

            // to combine the general results and the separate services
            var combinedResults = new
            {
                GeneralResults = results,
                Services = serviceInfoList,
                IsServicesActive = serviceInfoList.Any() 
            };

            return Ok(combinedResults);
        }

        private string ExecuteSshCommand(string host, string username, string password, string command)
        {
            try
            {
                using (var client = new SshClient(host, username, password))
                {
                    client.Connect();

                    if (client.IsConnected)
                    {
                        using (var commandExec = client.CreateCommand(command))
                        {
                            var output = commandExec.Execute();
                            client.Disconnect();
                            return output;
                        }
                    }
                    else
                    {
                        return "SSH connection failed.";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }


        /************ Server Monitoring ********/


        //[HttpPost]
        //[Route("ConnectServer")]
        //public async Task<IActionResult> ConneServer([FromBody] ServerDto serverDto)
        //{
        //    string serv = "";
        //    string Login = "";
        //    string Password = "";
        //    var filenames = new List<string>();

        //    using (var client = new SshClient(host, "", ""))
        //    {
        //        client.Connect();

        //        using (var command = client.CreateCommand("ls /"))
        //        {
        //            var output = command.Execute();

        //            // Split the output into lines
        //            string[] lines = output.Split('\n');

        //            // Loop through each line and store the filename in the list
        //            foreach (var line in lines)
        //            {
        //                // Ignore empty lines
        //                if (!string.IsNullOrWhiteSpace(line))
        //                {
        //                    // Remove leading and trailing whitespaces
        //                    filenames.Add(line.Trim());
        //                }
        //            }
        //        }

        //        client.Disconnect();
        //    }

        //    // Return the list of filenames as JSON
        //    return Ok(new { Filenames = filenames });
        //}
    }
}
