using AutoMapper;
using DatabaseManager.Services.Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<RuleFunctionsDto, RuleFunctions>().ReverseMap();
                config.CreateMap<RuleModelDto, RuleModel>().ReverseMap();
            });

            return mappingConfig;
        }
    }
}
