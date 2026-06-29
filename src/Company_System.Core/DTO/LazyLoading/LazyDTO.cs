namespace HR_System.Core.DTO.LazyLoading;

public class LazyDTO
{
    public required int Taken { get; set; }
    public required int SectionSize { get; set; }
    
    
    
    // override
    override public string ToString()
    {
        return $"Taken: {Taken}\nSectionSize: {SectionSize}\n";
    }
}