using System;
using System.IO;
using System.Text.Json;

class Program {
    static void Main() {
        var path = @"C:\Users\yoruk\.gemini\antigravity\brain\ca31125c-e38f-45c4-a69a-24dbb2c0d0bc\.system_generated\logs\transcript_full.jsonl";
        foreach(var line in File.ReadLines(path)) {
            if (line.Contains("TEST BORCU KAPATMA")) {
                var doc = JsonDocument.Parse(line);
                if (doc.RootElement.GetProperty("type").GetString() == "USER_INPUT") {
                    File.WriteAllText("muhur_talimati.txt", doc.RootElement.GetProperty("content").GetString());
                    Console.WriteLine("Yazıldı.");
                    break;
                }
            }
        }
    }
}
