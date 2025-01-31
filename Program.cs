using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace BookStoreApp
{
    public class Författare
    {
        [BsonId]
        public ObjectId ID { get; set; } 
        public string Förnamn { get; set; }
        public string Efternamn { get; set; }
        public DateTime Födelsedatum { get; set; }
        public List<Böcker> Böcker { get; set; }
    }

    public class Butiker
    {
        [BsonId]
        public ObjectId ID { get; set; }
        public string Butiksnamn { get; set; }
        public string Adress { get; set; }
        public List<Lagersaldo> Lagersaldo { get; set; }
    }

    public class Customer
    {
        [BsonId]
        public ObjectId ID { get; set; }
        public string Förnamn { get; set; }
        public string Andranamn { get; set; }
        public string Email { get; set; }
        public string Telefonnummer { get; set; }
        public string Favoritgenre { get; set; }
        public ObjectId? FavoritförfattareID { get; set; } 
        public Författare FavoritFörfattare { get; set; }
    }

    public class Böcker
    {
        [BsonId]
        public string ISBN13 { get; set; }
        public string Titel { get; set; }
        public string Språk { get; set; }
        public decimal Pris { get; set; }
        public DateTime Utgivningsdatum { get; set; }
        public ObjectId FörfattareID { get; set; } 
        public string Genre { get; set; }
    }

    public class Ordrar
    {
        [BsonId]
        public ObjectId ID { get; set; }
        public ObjectId CustomerID { get; set; } 
        public Customer Customer { get; set; }
        public DateTime Orderdatum { get; set; }
        public decimal Totalamount { get; set; }
    }

    public class Lagersaldo
    {
        [BsonId]
        public ObjectId ID { get; set; }
        public ObjectId ButikID { get; set; }  
        public ObjectId BokID { get; set; }    
        public int Antal { get; set; }
    }

    public class BookStoreContext
    {
        private readonly IMongoDatabase _database;

        public BookStoreContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<Författare> Författare => _database.GetCollection<Författare>("Författare");
        public IMongoCollection<Butiker> Butiker => _database.GetCollection<Butiker>("Butiker");
        public IMongoCollection<Böcker> Böcker => _database.GetCollection<Böcker>("Böcker");
        public IMongoCollection<Lagersaldo> Lagersaldo => _database.GetCollection<Lagersaldo>("Lagersaldo");
        public IMongoCollection<Customer> Customers => _database.GetCollection<Customer>("Customers");
        public IMongoCollection<Ordrar> Ordrar => _database.GetCollection<Ordrar>("Ordrar");
    }

    class Program
    {
        static void Main(string[] args)
        {
            var context = new BookStoreContext("mongodb://localhost:27017", "BookStoreDB");

            bool fortsätt = true;
            while (fortsätt == true)
            {
                Console.Clear();
                Console.WriteLine("Välkommen till Boklagerhantering");
                Console.WriteLine("1. Lista lagersaldo för butiker");
                Console.WriteLine("2. Lägg till bok i butik");
                Console.WriteLine("3. Ta bort bok från butik");
                Console.WriteLine("4. Avsluta");
                Console.Write("Välj ett alternativ: ");
                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        ListLagersaldo(context);
                        break;
                    case "2":
                        AddBookToStore(context);
                        break;
                    case "3":
                        RemoveBookFromStore(context);
                        break;
                    case "4":
                        fortsätt = false;
                        break;
                    default:
                        Console.WriteLine("Ogiltigt val.");
                        break;
                }
            }
        }

        static void AddBookToStore(BookStoreContext context)
        {
            Console.Clear();
            Console.WriteLine("Välj en butik:");

            var butikern = context.Butiker.Find(_ => true).ToList();

            for (int i = 0; i < butikern.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {butikern[i].Butiksnamn}");
            }

            Console.Write("Ange butikens nummer: ");
            var butikChoice = int.Parse(Console.ReadLine()) - 1;

            var böcker = context.Böcker.Find(_ => true).ToList();

            Console.WriteLine("\nVälj en bok att lägga till:");
            for (int i = 0; i < böcker.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {böcker[i].Titel} ({böcker[i].ISBN13})");
            }

            Console.Write("Ange bokens nummer: ");
            var bokChoice = int.Parse(Console.ReadLine()) - 1;

            Console.Write("Ange antal: ");
            var antal = int.Parse(Console.ReadLine());

            var butik = butikern[butikChoice];
            var bok = böcker[bokChoice];

            var lagersaldo = new Lagersaldo
            {
                ButikID = butik.ID,
                BokID = bok.ISBN13, 
                Antal = antal
            };

            context.Lagersaldo.InsertOne(lagersaldo);

            Console.WriteLine("Bok har lagts till i butiken!");
            Console.WriteLine("\nTryck på en tangent för att återgå.");
            Console.ReadKey();
        }

        static void ListLagersaldo(BookStoreContext context)
        {
            var lagersaldo = context.Lagersaldo
                .Aggregate()
                .Lookup<Butiker, Lagersaldo, Butiker>(ls => ls.ButikID, b => b.ID, (ls, b) => new { ls, b })
                .Lookup<Lagersaldo, Böcker, Böcker>(ls => ls.ls.BokID, b => b.ISBN13, (ls, b) => new { ls.ls, b })
                .ToList();

            Console.Clear();
            Console.WriteLine("Lagersaldo:");
            foreach (var item in lagersaldo)
            {
                Console.WriteLine($"{item.ls.b.Butiksnamn} - {item.b.Titel} ({item.ls.Antal} st)");
            }
            Console.WriteLine("\nTryck på en tangent för att återgå.");
            Console.ReadKey();
        }

        static void RemoveBookFromStore(BookStoreContext context)
        {
            Console.Clear();
            Console.WriteLine("Välj en butik:");
            var butiker = context.Butiker.Find(_ => true).ToList();
            for (int i = 0; i < butiker.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {butiker[i].Butiksnamn}");
            }

            Console.Write("Ange butikens nummer: ");
            var butikChoice = int.Parse(Console.ReadLine()) - 1;

            var lagersaldo = context.Lagersaldo
                .Find(ls => ls.ButikID == butiker[butikChoice].ID)
                .ToList();

            Console.WriteLine("\nVälj en bok att ta bort:");
            for (int i = 0; i < lagersaldo.Count; i++)
            {
                var bok = context.Böcker.Find(b => b.ISBN13 == lagersaldo[i].BokID).FirstOrDefault();
                Console.WriteLine($"{i + 1}. {bok?.Titel} ({lagersaldo[i].Antal} st)");
            }

            Console.Write("Ange bokens nummer: ");
            var bokChoice = int.Parse(Console.ReadLine()) - 1;

            context.Lagersaldo.DeleteOne(ls => ls.ID == lagersaldo[bokChoice].ID);

            Console.WriteLine("Bok har tagits bort!");
            Console.WriteLine("\nTryck på en tangent för att återgå.");
            Console.ReadKey();
        }
    }
}
