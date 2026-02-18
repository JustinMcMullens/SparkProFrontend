namespace SparkBackend.Endpoints;

public record CommissionParticipantDto(
    int UserId,
    string Name,
    string Role,
    string PayoutType,
    decimal Amount,
    decimal CalculatedPayout
);

public record ManagerOverrideDto(
    int UserId,
    string Name,
    string Role,
    decimal Amount
);

public record CommissionCalcResponse(
    int SaleId,
    string ProjectType,
    string Phase,
    string Mode,
    object SaleDetails,
    decimal StackBase,
    decimal StackPercent,
    decimal StackAmount,
    decimal Adders,
    decimal Clawbacks,
    decimal ManagerRate,
    List<CommissionParticipantDto> Participants,
    List<ManagerOverrideDto> ManagerOverrides,
    decimal RegionalRate,
    List<ManagerOverrideDto> RegionalOverrides,
    decimal CompanyRemaining
);

public record CommissionCalcSave(
    string Phase,
    string Mode,
    decimal StackPercent,
    decimal Adders,
    decimal Clawbacks,
    List<CommissionParticipantDto> Participants
);

public record ParticipantDto(int UserId, string FirstName, string LastName, decimal SplitPercent);

public record PayrollDealWithParticipantsDto(
    int SaleId,
    string CustomerFirstName,
    string CustomerLastName,
    string ProjectType,
    string Stage,
    string Status,
    DateTime? SignedDate,
    DateTime UpdatedAt,
    DateTime CreatedAt,
    IReadOnlyList<ParticipantDto> Setters,
    IReadOnlyList<ParticipantDto> Closers
);

public record LoginRequest(string Username, string Password);
public record UpdateLastCompanyRequest(int CompanyId);
public record UpdateStatusRequest(string? Status);
public record AssignTicketRequest(int? AssignedUserId);
public record UpdateProfileRequest(string? FirstName, string? LastName, string? Email, string? Phone, string? ProfileImageUrl, string? DashboardBannerUrl, string? ProfileBannerUrl);
public record UpdateBannerRequest(string? DashboardBannerUrl, string? ProfileBannerUrl);
public record UpdateUserPermissionsRequest(int? RoleId);
public record AssignCommissionPlanRequest(long PlanId, decimal? PercentRate, decimal? FlatAmount, DateOnly? EffectiveDate);
public record PayoutUploadEntry(int? SaleId, string? Type, string? ActualDate, string? Description);

// === Phase 0: New DTOs for frontend endpoints ===

public class UnifiedAllocationDto
{
    public int AllocationId { get; set; }
    public string Industry { get; set; } = null!;
    public int SaleId { get; set; }
    public int UserId { get; set; }
    public int AllocationTypeId { get; set; }
    public int MilestoneNumber { get; set; }
    public decimal AllocatedAmount { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? PayrollBatchId { get; set; }
    public int? BucketId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public record CancelSaleRequest(string Reason);

public record BatchApproveRequest(List<BatchApproveItem> Allocations);
public record BatchApproveItem(string Industry, int AllocationId);

public record CreateBatchRequest(string? Description, DateOnly? PeriodStart, DateOnly? PeriodEnd);
public record AddAllocationsToBatchRequest(List<BatchApproveItem> Allocations);

public record CreateGoalRequest(
    string GoalName,
    int GoalTypeId,
    string GoalLevel,
    decimal TargetValue,
    DateOnly StartDate,
    DateOnly EndDate,
    int? TeamId,
    int? OfficeId,
    int? RegionId
);

public record AssignGoalRequest(List<int> UserIds);

public record CreateAnnouncementRequest(
    string Title,
    string Body,
    string? Priority,
    DateTime? ExpiresAt,
    List<AnnouncementTargetDto>? Targets
);

public record AnnouncementTargetDto(string TargetType, int? TargetId);

public record CreateTicketRequest(
    string Subject,
    string? Description,
    string? Priority,
    int? CategoryId
);

public record AddCommentRequest(string Body);
public record ChangeTicketStatusRequest(string Status);
