﻿using Jwks.Manager;
using Microsoft.EntityFrameworkCore;
using Jwks.Manager.Store.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace UNIPObjetivo.Identity.Server
{
    public class ApplicationDbContext : IdentityDbContext, ISecurityKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<SecurityKeyWithPrivate> SecurityKeys { get; set; }
    }
}