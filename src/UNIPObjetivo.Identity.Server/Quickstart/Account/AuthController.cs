using Jwks.Manager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace UNIPObjetivo.Identity.Server.Quickstart.Account
{
    [ApiController]
    [Authorize(AuthenticationSchemes = "BearerIdentity")]
    [Route("[controller]")]
    public class AuthController : Controller
    {
        public AuthController(IJsonWebKeySetService jwksService)
        {
            _jwksService = jwksService;
        }

        private readonly IJsonWebKeySetService _jwksService;

        [HttpPost("autenticar")]
        public async Task<ActionResult> Login(string login, string password)
        {
            var identity = ObterClaimsUsuario();
            var encodedToken = await CodificarToken(identity);
            return Ok(new
            {
                AccessToken = encodedToken,
                ExpiresIn = TimeSpan.FromHours(1).TotalSeconds
            });
        }

        private async Task<string> CodificarToken(ClaimsIdentity identityClaims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwks = _jwksService.GetCurrent();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "http://localhost:4000",
                Subject = identityClaims,
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = jwks
            });

            return tokenHandler.WriteToken(token);
        }

        private ClaimsIdentity ObterClaimsUsuario()
        {
            var identityClaims = new ClaimsIdentity();
            IList<Claim> claims = new List<Claim>();
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, "admin@admin.com"));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, "admin@admin.com"));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, ToUnixEpochDate(DateTime.UtcNow).ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(DateTime.UtcNow).ToString(), ClaimValueTypes.Integer64));

            identityClaims.AddClaims(claims);

            return identityClaims;
        }

        private static long ToUnixEpochDate(DateTime date) => (long)Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
    }
}
