using System.Reflection;
using FluentAssertions;
using Xunit;

namespace TrainService.Simulation.Tests;

public class SmokeTests
{
    [Fact]
    public void Simulation_Iskelet_BosKaliyor()
    {
        // Simulation projesi yüklendiğini doğrulama ve
        // Faz H öncesi public tip sayısının sıfır/iskelet olduğunu assert etme.
        // TrainService.Simulation assembly'sini yükle
        var assembly = Assembly.Load("TrainService.Simulation");
        assembly.Should().NotBeNull("Simulation derlemesi belleğe yüklenebilmeli");
        
        var publicTypes = assembly.GetExportedTypes();
        // Faz H henüz yapılmadı, o yüzden sadece 0 ya da iskelet sınıfı varsa çok az public tip bekliyoruz.
        // Hatta class Library olarak boş geliyorsa 0 da olabilir.
        publicTypes.Length.Should().BeLessThan(10, "Faz H öncesi simulation projesi sadece iskelet halinde olmalı");
    }
}
