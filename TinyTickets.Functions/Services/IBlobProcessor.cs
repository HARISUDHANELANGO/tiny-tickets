using TinyTickets.Functions.Models;
using System.Threading.Tasks;

namespace TinyTickets.Functions.Services
{
    public interface IBlobProcessor
    {
        Task ProcessAsync(BlobInfo blobInfo);
    }
}
