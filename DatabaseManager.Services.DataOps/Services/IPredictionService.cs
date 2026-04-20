using DatabaseManager.Services.DataOps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Services
{
    public interface IPredictionService
    {
        Task<T> ProcessPrediction<T>(PredictionParameters predictionParameter);
    }
}
