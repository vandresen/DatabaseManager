using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Common.Helpers
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
