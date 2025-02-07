using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public ObjectId Id { get; set; }  

        public string Titel { get; set; }
        public string Språk { get; set; }
        public decimal Pris { get; set; }
        public DateTime Utgivningsdatum { get; set; }

        [BsonRepresentation(BsonType.ObjectId)] 
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
            var context = new BookStoreContext("mongodb+srv://glenncouesme:1jgqur1g6sNh79IW@cluster0.pymok.mongodb.net/\r\n", "BookStoreDB"); // Byt ut connection stringen till atlasen 

            bool fortsätt = true;
            while (fortsätt)
            {
                Console.Clear();
                Console.WriteLine("Välkommen till Boklagerhantering");
                Console.WriteLine("1. Lista lagersaldo för butiker");
                Console.WriteLine("2. Lägg till bok i butik");
                Console.WriteLine("3. Ta bort bok från butik");
                Console.WriteLine("5. Avsluta");  
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
                    case "5":
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
            if (butikern.Count == 0)
            {
                Console.WriteLine("Inga butiker tillgängliga.");
                Console.ReadKey();
                return;
            }

            for (int i = 0; i < butikern.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {butikern[i].Butiksnamn}");
            }

            Console.Write("Ange butikens nummer: ");
            var butikChoice = int.Parse(Console.ReadLine()) - 1;

            var böcker = context.Böcker.Find(_ => true).ToList();
            if (böcker.Count == 0)
            {
                Console.WriteLine("Inga böcker tillgängliga.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nVälj en bok att lägga till:");
            for (int i = 0; i < böcker.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {böcker[i].Titel} ({böcker[i].Id})");  
            }

            Console.Write("Ange bokens nummer: ");
            var bokChoice = int.Parse(Console.ReadLine()) - 1;

            Console.Write("Ange antal: ");
            var antal = int.Parse(Console.ReadLine());

            var butik = butikern[butikChoice];
            var bok = böcker[bokChoice];

            try
            {
                var lagersaldo = new Lagersaldo
                {
                    ButikID = butik.ID,
                    BokID = bok.Id,  
                    Antal = antal
                };

                context.Lagersaldo.InsertOne(lagersaldo);
                Console.WriteLine("Bok har lagts till i butiken!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ett fel inträffade: {ex.Message}");
            }

            Console.WriteLine("\nTryck på en tangent för att återgå.");
            Console.ReadKey();
        }

        static void ListLagersaldo(BookStoreContext context)
        {
            var lagersaldoList = context.Lagersaldo.Find(_ => true).ToList();

            var butikerList = context.Butiker.Find(_ => true).ToList();
            var böckerList = context.Böcker.Find(_ => true).ToList();

            Console.Clear();
            Console.WriteLine("Lagersaldo:");

            foreach (var lagersaldo in lagersaldoList)
            {
                var butik = butikerList.FirstOrDefault(b => b.ID == lagersaldo.ButikID);
                var bok = böckerList.FirstOrDefault(b => b.Id == lagersaldo.BokID);  

                if (butik != null && bok != null)
                {
                    Console.WriteLine($"{butik.Butiksnamn} - {bok.Titel} ({lagersaldo.Antal} st)");
                }
            }

            Console.WriteLine("\nTryck på en tangent för att återgå.");
            Console.ReadKey();
        }

        static void RemoveBookFromStore(BookStoreContext context)
        {
            Console.Clear();
            Console.WriteLine("Välj en butik:");
            var butiker = context.Butiker.Find(_ => true).ToList();
            if (butiker.Count == 0)
            {
                Console.WriteLine("Inga butiker tillgängliga.");
                Console.ReadKey();
                return;
            }

            for (int i = 0; i < butiker.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {butiker[i].Butiksnamn}");
            }

            Console.Write("Ange butikens nummer: ");
            var butikChoice = int.Parse(Console.ReadLine()) - 1;

            var lagersaldo = context.Lagersaldo
                .Find(ls => ls.ButikID == butiker[butikChoice].ID)
                .ToList();

            if (lagersaldo.Count == 0)
            {
                Console.WriteLine("Inga böcker i denna butik.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nVälj en bok att ta bort:");
            for (int i = 0; i < lagersaldo.Count; i++)
            {
                var bok = context.Böcker.Find(b => b.Id == lagersaldo[i].BokID).FirstOrDefault(); 
                Console.WriteLine($"{i + 1}. {bok?.Titel} ({lagersaldo[i].Antal} st)");
            }

            Console.Write("Ange bokens nummer: ");
            var bokChoice = int.Parse(Console.ReadLine()) - 1;

            try
            {
                context.Lagersaldo.DeleteOne(ls => ls.ID == lagersaldo[bokChoice].ID);
                Console.WriteLine("Bok har tagits bort!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ett fel inträffade: {ex.Message}");
            }

            Console.WriteLine("\nTryck på en tangent för att återgå.");
            Console.ReadKey();
        }
    }
}
