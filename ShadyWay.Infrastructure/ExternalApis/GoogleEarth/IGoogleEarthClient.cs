namespace ShadyWay.Infrastructure.ExternalApis.GoogleEarth
{
    public interface IGoogleEarthClient
    {
        // עצים — גובה הצמרת דרך ETH Canopy Height על GEE
        Task<IEnumerable<TreeInfo>> GetTreesInAreaAsync(
            double minLat, double minLon,
            double maxLat, double maxLon);
    }
}
