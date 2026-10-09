using Core.Modules.SatelliteData.Application.DTOs;
using Core.Modules.SatelliteData.Application.Interfaces;
using Core.Modules.SatelliteData.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Modules.SatelliteData.Application.Services
{
    public partial class SatellitesGetDataService : ISatellitesGetService
    {
        private readonly ISatellitesGetDataRepository _satellitesGetDataRepository;
        private readonly ILogger<SatellitesGetDataService> _logger;

        public SatellitesGetDataService(ISatellitesGetDataRepository satellitesGetDataRepository, ILogger<SatellitesGetDataService> logger)
        {
            _satellitesGetDataRepository = satellitesGetDataRepository;
            _logger = logger;
        }

        public async Task<List<SatellitesFilterDTO>> GetSatellitesFiltersById(string category, int page, int pageSize = 25)
        {
            if (string.IsNullOrEmpty(category)) return new List<SatellitesFilterDTO>();

            var satellitesList = await _satellitesGetDataRepository.GetSatellitesFiltersById(category, page, pageSize);
            if (satellitesList == null || satellitesList.Count <= 0) return new List<SatellitesFilterDTO>();

            List<SatellitesFilterDTO> satellitesListDTO = new List<SatellitesFilterDTO>();

            foreach (var satellite in satellitesList)
            {
                SatellitesFilterDTO dto = new SatellitesFilterDTO
                {
                    NoradId = satellite.NORAD_CAT_ID,
                    Name = satellite.OBJECT_NAME
                };

                satellitesListDTO.Add(dto);
            }

            return satellitesListDTO;
        }

        public async Task<List<SatellitesFilterDTO>> GetSatellitesFiltersByName(string category, int page, int pageSize = 25)
        {
            if (string.IsNullOrEmpty(category)) return new List<SatellitesFilterDTO>();

            var satellitesList = await _satellitesGetDataRepository.GetSatellitesFiltersByName(category, page, pageSize);
            if (satellitesList == null || satellitesList.Count <= 0) return new List<SatellitesFilterDTO>();

            List<SatellitesFilterDTO> satellitesListDTO = new List<SatellitesFilterDTO>();

            foreach (var satellite in satellitesList)
            {
                SatellitesFilterDTO dto = new SatellitesFilterDTO
                {
                    NoradId = satellite.NORAD_CAT_ID,
                    Name = satellite.OBJECT_NAME
                };

                satellitesListDTO.Add(dto);
            }

            return satellitesListDTO;
        }

        public async Task<List<SatellitesFilterDTO>> GetDataById(int noradId)
        {
            LogLaunchById();

            if (noradId < 0)
            {
                LogCancelNegativeNumber();
                return new List<SatellitesFilterDTO>();
            }

            var satellitesList = await _satellitesGetDataRepository.GetDataById(noradId);
            if (satellitesList == null || satellitesList.Count <= 0)
            {
                LogCancelNull(noradId);
                return new List<SatellitesFilterDTO>();
            }

            List<SatellitesFilterDTO> satellitesListDTO = new List<SatellitesFilterDTO>();

            foreach (var satellite in satellitesList)
            {
                SatellitesFilterDTO dto = new SatellitesFilterDTO
                {
                    NoradId = satellite.NORAD_CAT_ID,
                    Name = satellite.OBJECT_NAME
                };

                satellitesListDTO.Add(dto);
            }

            LogEndById();
            return satellitesListDTO;
        }

        public async Task<List<SatellitesFilterDTO>> GetDataByName(string satelliteName)
        {
            LogLaunchByName();

            if (string.IsNullOrEmpty(satelliteName))
            {
                LogCancelNameNull();
                return new List<SatellitesFilterDTO>();
            }

            var satellitesList = await _satellitesGetDataRepository.GetDataByName(satelliteName);
            if (satellitesList == null || satellitesList.Count <= 0)
            {
                LogCancelNull(satelliteName);
                return new List<SatellitesFilterDTO>();
            }

            List<SatellitesFilterDTO> satellitesListDTO = new List<SatellitesFilterDTO>();

            foreach (var satellite in satellitesList)
            {
                SatellitesFilterDTO dto = new SatellitesFilterDTO
                {
                    NoradId = satellite.NORAD_CAT_ID,
                    Name = satellite.OBJECT_NAME
                };

                satellitesListDTO.Add(dto);
            }

            LogEndByName();
            return satellitesListDTO;
        }
    }
}