namespace Amplify.Application.Common.DTOs.Trading;

public class ClosePositionDto
{
    public Guid PositionId { get; set; }
    public decimal ExitPrice { get; set; }
    public string? Notes { get; set; }
}