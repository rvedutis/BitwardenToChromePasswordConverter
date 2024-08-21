using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

namespace BitwardenToChromePasswordConverter
{
    abstract class Program
    {
        static void Main(string[] args)
        {
            const string basePath = "c:\\users\\bob\\Desktop\\";
            const string inputFileNameWithExtension = "from-bitwarden.csv";

            var inputFilePath = $"{basePath}{inputFileNameWithExtension}";
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"{inputFilePath} not found.");
            }

            var outputFileName = $"done-{inputFileNameWithExtension}";
            var outputFilePath = $"{basePath}\\{outputFileName}";

            Console.WriteLine("Processing passwords...");

            var records = new List<BitwardenEntry>();

            using (var reader = new StreamReader(inputFilePath))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    records = csv.GetRecords<BitwardenEntry>().Where(b => !string.IsNullOrWhiteSpace(b.login_uri)).ToList();
                }
            }

            using (var writer = new StreamWriter(outputFilePath))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<BitwardenEntryMap>();
                    csv.WriteRecords(records);
                }
            }

            Console.WriteLine("Done.");
        }

        private static string SanitizeUri(string uri)
        {

            var parts = uri.Split(',');
            const string androidAppPattern = "androidapp://";

            return parts.FirstOrDefault(p => !p.StartsWith(androidAppPattern)) ?? "";
        }

        private class BitwardenEntry
        {
            [TypeConverter(typeof(BitwardenUriConverter))]
            public string login_uri { get; set; }
            public string login_username { get; set; }
            public string login_password { get; set; }
        }

        private class BitwardenEntryMap : ClassMap<BitwardenEntry>
        {
            public BitwardenEntryMap()
            {
                
                Map(m => m.login_uri).Name("url").TypeConverter<BitwardenUriConverter>();
                Map(m => m.login_username).Name("username");
                Map(m => m.login_password).Name("password");
            }
        }

        private class BitwardenUriConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                return SanitizeUri(text);
            }
        }
    }
}