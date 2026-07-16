using System.ComponentModel.DataAnnotations;

namespace HR_System.Core.DTO.LazyLoading;

public class LazyDTO
{
    [Range(0, int.MaxValue)]
    public required int Taken { get; set; }
    
    [Range(0, int.MaxValue)]
    public required int SectionSize { get; set; }
    
    
    
    // override
    override public string ToString()
    {
        return $"Taken: {Taken}\nSectionSize: {SectionSize}\n";
    }
}