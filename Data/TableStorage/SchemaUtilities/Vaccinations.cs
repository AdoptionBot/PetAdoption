namespace Data.TableStorage.SchemaUtilities
{
    [Flags]
    public enum Vaccinations
    {
        none = 0,

        // Cats
        Trivalent = 1 << 0,     // coryza, panleukopenia, rhinotracheitis
        Felv = 1 << 1,          // feline leukemia
        FIP = 1 << 2,           // feline infectious peritonitis
        FIV = 1 << 3,           // feline immunodeficiency virus

        // Dogs
        Parvovirus = 1 << 4,
        Distemper = 1 << 5,
        Hepatitis = 1 << 6,     // Canine Adenovirus
        Leptospirosis = 1 << 7,
        Parainfluenza = 1 << 8,
        Bordetella = 1 << 9,    // Kennel Cough
        LymeDisease = 1 << 10,
        Coronavirus = 1 << 11,  // Canine coronavirus (CCoV)

        // Cats and Dogs
        Rabies = 1 << 12
    }
}
