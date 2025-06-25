using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FC_Server.Models;
using FC_Server.DAL;


namespace FC_Server.Services
{
    public interface IGoogleMapsService
    {
        // Add methods that your tracking service needs
        Task<double> CalculateDistance(double lat1, double lon1, double lat2, double lon2);
        Task<string> GetDirections(double fromLat, double fromLon, double toLat, double toLon);
    }
}

