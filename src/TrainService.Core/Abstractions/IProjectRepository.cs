using System.Threading.Tasks;

namespace TrainService.Core.Abstractions;

public interface IProjectRepository
{
    Task SaveDocumentAsync(string jsonContent);
    Task<string?> LoadDocumentAsync();
}
