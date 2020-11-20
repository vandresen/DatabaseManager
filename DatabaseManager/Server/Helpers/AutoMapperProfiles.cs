using AutoMapper;
using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<SourceEntity, ConnectParameters>()
                .ForMember(dest => dest.SourceName, opt => opt.MapFrom(src => src.RowKey));
            CreateMap<ConnectParameters, SourceEntity>()
                .ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.SourceName));
        }
    }
}
