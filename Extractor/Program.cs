
using System;
using System.IO;
using System.Text.Json;

var path = @"C:\Users\yoruk\.gemini\antigravity\brain\ca31125c-e38f-45c4-a69a-24dbb2c0d0bc\.system_generated\logs\transcript_full.jsonl";
foreach(var line in File.ReadLines(path)) {
    if (line.Contains("public void T302_ZoomAt_ImlecAltiSabit")) {
        var doc = JsonDocument.Parse(line);
        if (doc.RootElement.GetProperty("type").GetString() == "USER_INPUT") {
            File.WriteAllText(@"..\eski_talimat.txt", doc.RootElement.GetProperty("content").GetString());
            Console.WriteLine("Yazıldı.");
            break;
        }
    }
}

