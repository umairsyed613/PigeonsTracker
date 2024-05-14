using PigeonsTracker.Shared.Models;
using PigeonsTrackerApi.Models;

namespace PigeonsTrackerApi.Mapper;

public static class PigeonDiseaseMapper
{
    public static FsPigeonDiseaseAndCure Map(this PigeonDiseaseAndCure pigeonDiseaseAndCure)
    {
        return new FsPigeonDiseaseAndCure()
        {
            Id = pigeonDiseaseAndCure.Id,
            Title = pigeonDiseaseAndCure.Title,
            Content = pigeonDiseaseAndCure.Content,
            CreatedAt = pigeonDiseaseAndCure.CreatedAt,
            ModifiedAt = pigeonDiseaseAndCure.ModifiedAt
        };
    }

    public static PigeonDiseaseAndCure Map(this FsPigeonDiseaseAndCure pigeonDiseaseAndCure)
    {
        return new PigeonDiseaseAndCure()
        {
            Id = pigeonDiseaseAndCure.Id,
            Title = pigeonDiseaseAndCure.Title,
            Content = pigeonDiseaseAndCure.Content,
            CreatedAt = pigeonDiseaseAndCure.CreatedAt,
            ModifiedAt = pigeonDiseaseAndCure.ModifiedAt
        };
    }
}