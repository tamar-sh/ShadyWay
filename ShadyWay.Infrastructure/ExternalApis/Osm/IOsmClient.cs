using ShadyWay.Core.Models;

namespace ShadyWay.Infrastructure.ExternalApis.Osm
{
    public interface IOsmClient
    {
        Task<OsmMapData> GetOsmMapDataAsync(BoundingBox bbox);
    }
}
