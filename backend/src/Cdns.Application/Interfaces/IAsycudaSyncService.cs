using Cdns.Application.DTOs.Sync;

namespace Cdns.Application.Interfaces;

public interface IAsycudaSyncService
{
    Task<IReadOnlyList<CusDecSyncRow>> GetChangedCusDecsAsync(DateTime since, CancellationToken cancellationToken);
    Task<IReadOnlyList<CusDecItemSyncRow>> GetChangedCusDecItemsAsync(DateTime since, CancellationToken cancellationToken);
}
