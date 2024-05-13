using Microsoft.EntityFrameworkCore;
using System.Text.Json;

int apartmentId = 0;

await using (var db = new AppDbContext())
{
    var apartment = new Apartment();
    db.Add(apartment);
    await db.SaveChangesAsync();

    var person = new Person
    {
        ApartmentId = apartment.Id,
        PhoneNumber = new("old")
    };

    db.Add(person);
    await db.SaveChangesAsync();

    apartmentId = apartment.Id;
}

await using (var db = new AppDbContext())
{
    var query = (from apartment in db.Apartments

                 let persons = (from otherApartment in db.Apartments

                                from person in (from p in db.Persons
                                                where p.ApartmentId == otherApartment.Id
                                                // Removing the Wrapper type and using an anonymous type correctly returns the PhoneNumber.
                                                select new Wrapper
                                                {
                                                    Person = p
                                                })

                                where otherApartment.Id == apartment.Id
                                select person).ToList()
                 where apartment.Id == apartmentId
                 select new
                 {
                     apartment.Id,
                     persons
                 });

    // PhoneNumber is not queried in the query string.
    Console.WriteLine(query.ToQueryString());

    var result = await query.ToListAsync();

    // PhoneNumber is null even though it shouldn't be.
    Console.WriteLine(JsonSerializer.Serialize(result));
}

public class AppDbContext : DbContext
{
    public DbSet<Apartment> Apartments { get; set; }
    public DbSet<Person> Persons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5433;Database=efcore-owned-entity;Username=postgres;Password=developer");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Apartment>(e =>
        {
            e.HasMany(a => a.Persons)
             .WithOne()
             .HasForeignKey(p => p.ApartmentId);
        });

        modelBuilder.Entity<Person>(e =>
        {
            e.OwnsOne(p => p.PhoneNumber);
        });
    }
}

public class Apartment
{
    public int Id { get; set; }
    public List<Person> Persons { get; set; }
}

public class Person
{
    public int Id { get; private set; }

    public PhoneNumber PhoneNumber { get; set; }

    public int ApartmentId { get; set; }
}

public record PhoneNumber(string? Number);

public class Wrapper
{
    public Person Person { get; set; }
}
