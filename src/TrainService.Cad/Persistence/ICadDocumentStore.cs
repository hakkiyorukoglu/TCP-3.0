using System.Threading.Tasks;

namespace TrainService.Cad.Persistence;

public interface ICadDocumentStore
{
    Task SaveDocumentAsync(System.Guid projectId, CadDocument document);
    Task LoadDocumentAsync(System.Guid projectId, CadDocument document);
    Task CreateSnapshotAsync(CadDocument document, string name);
}
