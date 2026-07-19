namespace TrainService.App.Services;

/// <summary>
/// Aktif CAD aracına göre Prompt Area metnini üreten statik servis.
/// v3.0.29.19 — Command Line + Prompt Area için.
/// </summary>
public static class ToolPromptService
{
    /// <summary>
    /// Verilen araç adına göre bağlamsal yönlendirme mesajı döndürür.
    /// </summary>
    public static string GetPrompt(string toolName)
    {
        return toolName switch
        {
            "Select" => "Nesne seçin veya pencere çizin",
            "Track"  => "İlk noktayı tıklayın",
            "Route"  => "Segment üzerine tıklayın",
            "Hybrid" => "Ray başlangıç noktasını tıklayın",
            "Switch" => "Makas yerleştirme noktasını tıklayın",
            "Ramp"   => "Rampa başlangıç noktasını tıklayın",
            ""       => "Komut girin veya araç seçin",
            _        => "Komut girin veya araç seçin"
        };
    }
}