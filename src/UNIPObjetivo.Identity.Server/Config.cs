using IdentityModel;
using IdentityServer4;
using IdentityServer4.Test;
using IdentityServer4.Models;
using System.Security.Claims;
using System.Collections.Generic;

namespace UNIPObjetivo.Identity.Server
{
    public class Config
    {
        // Aqui vamos definir os resources que serão utilizados no nosso servidor
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            var customProfile = new IdentityResource(
                name: "custom.profile",
                displayName: "Custom Profile",
                userClaims: new[] { "customclaim" }
            );

            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                customProfile,
                new IdentityResource
                {
                    Name = "role",
                    UserClaims = new List<string> {"role"}
                }
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource> 
            {
                new ApiResource 
                {
                    Name = "api1",
                    DisplayName = "API #1",
                    Description = "Allow the application to access API #1 on your behalf",
                    Scopes = new List<string> {"api1.read", "api1.write"},
                    ApiSecrets = new List<Secret> {new Secret("ScopeSecret".Sha256())},
                    UserClaims = new List<string> {"role"}
                }
            };
        }

        public static IEnumerable<ApiScope> GetApiScopes()
        {
            return new List<ApiScope>
            {
                new ApiScope("api1.read", "Read Access to API #1"),
                new ApiScope("api1.write", "Write Access to API #1")
            };
        }

        // Aqui definimos qual Client (aplicação) que poderá acessar nosso servidor de identidade.
        public static IEnumerable<Client> GetClients()
        {
            // Credenciais da Aplicação
            return new List<Client>
            {
                // OpenID Connect
                new Client
                {
                    // O Nome ÚNICO da nossa aplicação autorizada no nosso servidor de autoridade
                    ClientId = "ObjetivoApp",
                    
                    // Nome de exibição da nossa aplicação
                    ClientName = "Objetivo Application",
                    
                    //Tipo de autenticação permitida
                    AllowedGrantTypes = GrantTypes.Implicit,

                    //Url de redicionamento para quando o login for efetuado com sucesso.
                    RedirectUris = { "https://localhost:5001/signin-oidc" },

                    //Url de redirecionamento para quando o logout for efetuado com sucesso.
                    PostLogoutRedirectUris = { "https://localhost:5001/signout-callback-oidc" },

                    //Escopos permitidos dentro da aplicação
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "custom.profile"
                    }
                },
                new Client
                {
                    ClientId = "oauthClient",
                    ClientName = "Example client application using client credentials",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = new List<Secret> {new Secret("SuperSecretPassword".Sha256())}, // change me!
                    AllowedScopes = new List<string> {"api1.read"}
                },
                new Client
                {
                    ClientId = "oidcClient",
                    ClientName = "Example Client Application",
                    ClientSecrets = new List<Secret> {new Secret("SuperSecretPassword".Sha256())}, // change me!
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = new List<string> {"https://localhost:5002/signin-oidc"},
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "role",
                        "api1.read"
                    },
                    RequirePkce = true,
                    AllowPlainTextPkce = false
                }
            };
        }

        // TestUser é uma classe de exemplo do proprio IdentityServer, aonde configuramos basicamente login/senha e as claims de exibição.
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "admin@admin.br",
                    Password = "admin",

                    Claims = new List<Claim>
                    {
                        new Claim("name", "Portal Objetivo"),
                        new Claim("website", "https://objetivo.br"),
                        new Claim("email", "admin@admin.br"),
                        new Claim("customclaim", "Minha claim customizada")
                    }
                },
                new TestUser 
                {
                    SubjectId = "5BE86359-073C-434B-AD2D-A3932222DABE",
                    Username = "scott",
                    Password = "password",
                    Claims = new List<Claim> 
                    {
                        new Claim(JwtClaimTypes.Email, "scott@scottbrady91.com"),
                        new Claim(JwtClaimTypes.Role, "admin")
                    }
                }
            };
        }
    }
}