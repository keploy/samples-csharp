using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace DiseaseDiagnosisAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DiseaseDiagnosisController : ControllerBase
    {
        [HttpPost]
        public IActionResult Diagnose([FromBody] SymptomInput input)
        {
            string result = GetDiagnosis(input.Symptoms);
            return Ok(new { diagnosis = result });
        }

       private string GetDiagnosis(string? symptoms)
{
    if (string.IsNullOrWhiteSpace(symptoms))
        return "No symptoms provided.";

    symptoms = symptoms.ToLower();

    if (symptoms.Contains("fever") && symptoms.Contains("cough"))
        return "Possibly Flu or COVID-19. Consult a physician.";
    else if (symptoms.Contains("headache") && symptoms.Contains("nausea"))
        return "Possibly Migraine.";
    else if (symptoms.Contains("abdominal pain"))
        return "Possibly Gastroenteritis.";
    else
        return "Symptoms not conclusive. Further testing needed.";
}

    }

    public class SymptomInput
    {
        public string Symptoms { get; set; }
    }
}
