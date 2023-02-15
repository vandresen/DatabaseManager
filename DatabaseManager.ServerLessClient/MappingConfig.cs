using AutoMapper;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.Shared;

namespace DatabaseManager.ServerLessClient
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<ConnectParametersDto, ConnectParameters>().ReverseMap();
            });

            return mappingConfig;
        }
    }
}
