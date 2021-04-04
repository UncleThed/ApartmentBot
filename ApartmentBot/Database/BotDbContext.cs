using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using ApartmentBot.Models;
using ApartmentBot.Parsers;
using ApartmentBot.Parsers.SutkiTomsk;

namespace ApartmentBot.Database
{
    public class BotDbContext : DbContext
    {
        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<Client> Clients { get; set; }

        public BotDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var parseController = new ParseController<List<Apartment>>(new SutkiTomskParser());
            var apartments = parseController.GetDataFromSite().Result;

            modelBuilder.Entity<Apartment>()
                .ToTable("Apartment")
                .HasData(apartments);

            modelBuilder.Entity<Client>()
                .ToTable("Client");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=tcp:gostevoitomskserver.database.windows.net,1433;Initial Catalog=GostevoiTomskDataBase;Persist Security Info=False;User ID=SanyaGenze;Password=Genze.1998;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        }
    }
}
