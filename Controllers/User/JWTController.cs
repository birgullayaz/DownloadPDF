using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ISLEMLER.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class JwtController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtController> _logger;

        public JwtController(IConfiguration configuration, ILogger<JwtController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

     }
    }