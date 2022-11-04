using AutoMapper;
using DatabaseManager.Services.Datasources.Models;
using DatabaseManager.Services.Datasources.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Datasources
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<ConnectParametersDto, ConnectParameters>();
                config.CreateMap<DataSourceEntity, ConnectParametersDto>()
                .ForMember(dest => dest.SourceName, act => act.MapFrom(src => src.RowKey));
                config.CreateMap<ConnectParameters, DataSourceEntity>()
                .ForMember(dest => dest.RowKey, act => act.MapFrom(src => src.SourceName));
            });

            return mappingConfig;
        }
    }
}
