using MonitoringBackend.Configuration;
using MonitoringBackend.Models.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using MonitoringBackend.Data;
using MonitoringBackend.Models;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;

namespace learnWebApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]

    public class AuthManagementController : ControllerBase
    {


            private readonly ILogger<AuthManagementController> _logger;
            private readonly UserManager<IdentityUser> _userManager;
            private readonly JwtConfig _jwtConfig;
            private readonly ApiDbContext _dbContext;
            private readonly TokenValidationParameters _tokenvalidationparameters;
            public AuthManagementController(ILogger<AuthManagementController> logger,
                UserManager<IdentityUser> userManager,
                ApiDbContext apiDbContext,
                TokenValidationParameters validationParameters,
                IOptionsMonitor<JwtConfig> _optionsMonitor)
            {
                _logger = logger;
                _tokenvalidationparameters = validationParameters;
                _dbContext = apiDbContext; 
                _userManager = userManager;
                _jwtConfig = _optionsMonitor.CurrentValue;
            }



        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto requestDto)
        {
            if (ModelState.IsValid)
            {
                var emailExists = await _userManager.FindByNameAsync(requestDto.User_Login);
                if (emailExists != null)
                {
                    return BadRequest("Login Already Exists");
                }
                var newUser = new IdentityUser()
                {
                    UserName = requestDto.User_Login
                };
                var isCreated = await _userManager.CreateAsync(newUser, requestDto.User_Password);
                if (isCreated.Succeeded)
                {

                    var jwttoken = await GenerateJwtAccessToken(newUser);
                    return Ok(jwttoken);
                }
                return BadRequest(isCreated.Errors.Select(x => x.Description).ToList());
            }
            else
            {
                return BadRequest("bad request payload");
            }
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto requestDto)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByNameAsync(requestDto.User_Login);
                if (existingUser == null)
                {
                    return BadRequest("Invalid User");
                }

                var isPasswordValid = await _userManager.CheckPasswordAsync(existingUser, requestDto.User_Password);
                if (isPasswordValid)
                {
                    var jwttoken = await GenerateJwtAccessToken(existingUser);



                    return Ok(jwttoken);
                    
                }
            }

            return BadRequest(new AuthResult()
            {
                Errors = new List<string>()
                {
                    "Invalid Payload"
                },
                Result = false
            });
        }



        private async Task<AuthResult> GenerateJwtAccessToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.UTF8.GetBytes(_jwtConfig.Secret);


            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString()),
                }),
                Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
                
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshtoken = new RefreshToken()
            {
                JwId = token.Id,
                Token = TokenString(25),
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                IsRevoked = false,
                IsUsed = false,
                User_Id = user.Id,
            };
            await _dbContext.refreshTokens.AddAsync(refreshtoken);
            await _dbContext.SaveChangesAsync();

            return new AuthResult()
            {
                AccessToken = jwtToken,
                RefreshToken = refreshtoken.Token,
                Result = true
            };
        }

        private string TokenString(int length) => new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~&é&)éçààç_", length).Select(s => s[new Random().Next(s.Length)]).ToArray());


        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest _tokenRequest)
        {
            

            if(ModelState.IsValid)
            {

                var result = VerifyAndGenerateToken(_tokenRequest);

                // Convert AuthResult to AuthResultDto
                var resultDto = new AuthResult
                {
                    AccessToken = result.Result.AccessToken,
                    RefreshToken = result.Result.RefreshToken,
                    Result = result.Result.Result
                };
                return Ok(resultDto);
            }
            return BadRequest(new AuthResult(){
                    Errors = new List<string>()
                    {
                     "invalid"   
                    },
                    Result = false
            });
        }

        private async Task<AuthResult> VerifyAndGenerateToken(TokenRequest _tokenRequest)
        {
            var jwttokenhandler = new JwtSecurityTokenHandler();
            try
            {
                _tokenvalidationparameters.ValidateLifetime = false;

                var tokenInverification = jwttokenhandler.ValidateToken(_tokenRequest.Token,_tokenvalidationparameters,out var validatetoken);

                if(validatetoken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,StringComparison.InvariantCultureIgnoreCase);
                    if(result==false)
                    {
                        return null;
                    }
                    var utcExpiryDate = long.Parse(tokenInverification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

                    var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

                    if(expiryDate > DateTime.Now)
                    {
                        return new AuthResult()
                        {
                            Errors = new List<string>()
                            {
                                "Expired Token"
                            },
                            Result = false
                        };
                    }
                }


                //var storedToken = await _dbContext.refreshTokens.FirstOrDefaultAsync(x => x.Token == _tokenRequest.RefreshToken);
                var storedToken = _dbContext.refreshTokens.FirstOrDefault(x => x.Token == _tokenRequest.RefreshToken);

                if (storedToken == null)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>()
                            {
                                "Invalid Token is null"
                            },
                        Result = false
                    };
                }

                if (storedToken.IsUsed)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>()
                            {
                                "Invalid Token is used"
                            },
                        Result = false
                    };
                }
                if (storedToken.IsRevoked)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>()
                            {
                                "Invalid Token is revoked"
                            },
                        Result = false
                    };
                }
                var jti = tokenInverification.Claims.FirstOrDefault(x=>x.Type == JwtRegisteredClaimNames.Jti).Value;
                if (storedToken.JwId != jti)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>()
                            {
                                "Invalid Token jwId "
                            },
                        Result = false
                    };
                }

                if (storedToken.ExpiryDate < DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>()
                            {
                                "Expired Token"
                            },
                        Result = false
                    };
                }
                storedToken.IsUsed = true;
                _dbContext.refreshTokens.Update(storedToken);
                _dbContext.SaveChanges();

                var dbUser = await _userManager.FindByIdAsync(storedToken.User_Id);
                var returnVal = await GenerateJwtAccessToken(dbUser);
                return returnVal;


            }
            catch (Exception ex)
            {
                return new AuthResult()
                {
                    Errors = new List<string>()
                            {
                                "Server Error"
                            },
                    Result = false
                };
            }

        }

        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var datetimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            datetimeVal = datetimeVal.AddSeconds(unixTimeStamp).ToUniversalTime();
            return datetimeVal;
        }


        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout([FromBody] TokenRequest _tokenRequest)
        {
            try
            {
                var storedToken = _dbContext.refreshTokens.FirstOrDefault(x => x.Token == _tokenRequest.RefreshToken);

                if (storedToken != null)
                {
                    storedToken.IsRevoked = true;
                    _dbContext.refreshTokens.Update(storedToken);
                    await _dbContext.SaveChangesAsync();
                    return Ok(new AuthResult
                    {
                        Result = true,
                        
                        
                    });
                }

                return BadRequest(new AuthResult
                {
                    Errors = new List<string>
            {
                "Invalid refresh token"
            },
                    Result = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResult
                {
                    Errors = new List<string>
            {
                "Server error during logout"
            },
                    Result = false
                });
            }
        }

    }
}
