using AutoMapper;
using DatabaseManager.Services.RulesSqlite.Models;

namespace DatabaseManager.Services.RulesSqlite
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
