using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main()
    {
        string directory = "TextFiles";
        GenerateFiles(directory);
        string combinedFilePath = Path.Combine(directory, "CombinedFile.txt");
        CombineFiles(directory, combinedFilePath, "abc");
        ImportToDatabase(combinedFilePath);
        ExecuteStoredProcedure();
    }

    static void GenerateFiles(string directory)
    {
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        Random random = new Random();
        for (int fileIndex = 0; fileIndex < 100; fileIndex++)
        {
            string filePath = Path.Combine(directory, $"file_{fileIndex + 1}.txt");
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                for (int i = 0; i < 100000; i++)
                {
                    string date = GenerateRandomDate(random).ToString("dd.MM.yyyy");
                    string randomLatin = GenerateRandomString(random, 10, false);
                    string randomCyrillic = GenerateRandomString(random, 10, true);
                    int randomEvenNumber = GenerateRandomEvenNumber(random);
                    double randomDecimal = Math.Round(random.NextDouble() * 19 + 1, 8);

                    writer.WriteLine($"{date}||{randomLatin}||{randomCyrillic}||{randomEvenNumber}||{randomDecimal.ToString("G8").Replace('.', ',')}||");
                }
            }
        }
    }

    static void CombineFiles(string sourceDirectory, string destinationPath, string symbolsToRemove)
    {
        var deletedLinesCount = 0;
        var allLines = Directory.GetFiles(sourceDirectory, "*.txt")
                                .SelectMany(File.ReadAllLines)
                                .ToList();

        var filteredLines = allLines.Where(line =>
        {
            if (line.Contains(symbolsToRemove))
            {
                deletedLinesCount++;
                return false;
            }
            return true;
        }).ToList();

        File.WriteAllLines(destinationPath, filteredLines);
        Console.WriteLine($"Удалено строк: {deletedLinesCount}");
    }

    static void ImportToDatabase(string filePath)
    {
        using var connection = new SQLiteConnection("Data Source=C:\\Users\\37544\\Desktop\\B1_firstTask\\B1_firstTask\\B1first_database.db");
        connection.Open();
         
        int importedRows = 0;
        int totalLines = File.ReadLines(filePath).Count();
        foreach (var line in File.ReadLines(filePath))
        {
            var parts = line.Split("||", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 5)
            {
                try
                {
                    var date = parts[0];
                    var latin = parts[1];
                    var cyrillic = parts[2];
                    var integerNumber = int.Parse(parts[3]);
                    var decimalNumber = double.Parse(parts[4].Replace(',', '.'));

                    var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = "INSERT INTO Data (Date, Latin, Cyrillic, IntegerNumber, DecimalNumber) VALUES ($date, $latin, $cyrillic, $integerNumber, $decimalNumber)";
                    insertCmd.Parameters.AddWithValue("$date", date);
                    insertCmd.Parameters.AddWithValue("$latin", latin);
                    insertCmd.Parameters.AddWithValue("$cyrillic", cyrillic);
                    insertCmd.Parameters.AddWithValue("$integerNumber", integerNumber);
                    insertCmd.Parameters.AddWithValue("$decimalNumber", decimalNumber);
                    insertCmd.ExecuteNonQuery();
                    importedRows++;
                    Console.WriteLine($"Импортировано: {importedRows}, осталось: {totalLines - importedRows}");
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Ошибка формата в строке: {line}. Подробности: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при импорте строки: {line}. Подробности: {ex.Message}");
                }
            }
        }
    }
    static void ExecuteStoredProcedure()
    {
        using var connection = new SQLiteConnection("Data Source=C:\\Users\\37544\\Desktop\\B1_firstTask\\B1_firstTask\\B1first_database.db");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"  
            SELECT SUM(IntegerNumber) AS TotalSum,  
                   (SELECT AVG(DecimalNumber) FROM Data) AS MedianDecimalNumber   
            FROM Data";

        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                var sum = reader["TotalSum"];
                var median = reader["MedianDecimalNumber"];
                Console.WriteLine($"Сумма всех целых чисел: {sum}, медиана всех дробных чисел: {median}");
            }
        }
    }

    static DateTime GenerateRandomDate(Random random)
    {
        int year = random.Next(DateTime.Now.Year - 5, DateTime.Now.Year + 1);
        int month = random.Next(1, 13);
        int day = random.Next(1, DateTime.DaysInMonth(year, month) + 1);
        return new DateTime(year, month, day);
    }

    static string GenerateRandomString(Random random, int length, bool isCyrillic)
    {
        const string latinChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string cyrillicChars = "АБВГДЕЁЖЗИЙКЛМНОПРСтУФХЦЧШЩЪЫЬЭЮЯ";
        var chars = isCyrillic ? cyrillicChars : latinChars;
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    static int GenerateRandomEvenNumber(Random random)
    {
        return random.Next(1, 50000001) * 2;
    }
}