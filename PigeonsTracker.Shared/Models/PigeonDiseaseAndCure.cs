using System;
using System.Collections.Generic;

namespace PigeonsTracker.Shared.Models;

public class PigeonDiseaseAndCure
{
    public string Id { get; set; }
    public Dictionary<string, string> Title { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> Content { get; set; } = new Dictionary<string, string>();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}