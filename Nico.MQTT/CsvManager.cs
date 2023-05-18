using CsvHelper;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nico.MQTT
{
    public static class CsvManager
    {

        public static void ReadTopic(string topic)
        {
            topic = topic.Replace("/", ".");
            using (var reader = new StreamReader($"{topic}.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                while (csv.Read())
                {
                    for (int i = 0; csv.TryGetField<int>(i, out var value); i++)
                    {
                        Console.Write($"{value},");
                    }
                }
            }
        }
        public static void AppendTopic(string topic, int value)
        {
            topic = topic.Replace("/", ".");
            using (var writer = new StreamWriter($"{topic}.csv", true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteField<int>(value);
                csv.NextRecord();
            }
        }


    }
}
