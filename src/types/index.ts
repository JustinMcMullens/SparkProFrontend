// Commission Platform - TypeScript Types
// Matches the actual Spark backend schema and API response shapes

// ============================================================================
// Common / Envelope Types
// ============================================================================

export interface ApiResponse<T> {
  data: T;
}

export interface PaginatedResponse<T> {
  data: T[];
  meta: {
    page: number;
    pageSize: number;
    totalCount: number;
  };
}

/** RFC 7807 Problem Details */
export interface ApiError {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

// ============================================================================
// Authority / Role
// ============================================================================

/** 1–2 = Rep/Setter/Closer, 3 = Team Lead, 4 = Management, 5 = Admin */
export type AuthorityLevel = 1 | 2 | 3 | 4 | 5;

export type RoleName = 'ReadOnly' | 'SalesRep' | 'TeamLead' | 'Management' | 'Administrator';

// ============================================================================
// Auth Types
// ============================================================================

export interface LoginRequest {
  username: string;
  password: string;
}

export interface Company {
  id: number;
  name: string;
  subdomain: string;
  logoUrl?: string | null;
}

export interface LoginResponse {
  userId: number;
  username: string;
  firstName: string;
  lastName: string;
  email: string;
  profileImageUrl?: string | null;
  dashboardBannerUrl?: string | null;
  profileBannerUrl?: string | null;
  lastAccessedCompanyId: number;
  companies: Company[];
}

export interface SessionResponse extends LoginResponse {
  permissions: Array<{
    companyId: number;
    authorityLevel: AuthorityLevel;
    permissionsJson?: string | null;
  }>;
}

// ============================================================================
// Enums / Union Types
// ============================================================================

export type SaleStatus =
  | 'PENDING'
  | 'APPROVED'
  | 'INSTALLED'
  | 'COMPLETED'
  | 'CANCELLED'
  | 'ON_HOLD';

export type BatchStatus =
  | 'DRAFT'
  | 'SUBMITTED'
  | 'APPROVED'
  | 'EXPORTED'
  | 'PAID'
  | 'CANCELLED';

export type Industry = 'Solar' | 'Pest' | 'Roofing' | 'Fiber';

export type TicketStatus = 'OPEN' | 'IN_PROGRESS' | 'PENDING' | 'RESOLVED' | 'CLOSED';

export type TicketPriority = 'LOW' | 'MEDIUM' | 'HIGH' | 'URGENT';

export type GoalLevel = 'INDIVIDUAL' | 'TEAM' | 'OFFICE' | 'REGION' | 'COMPANY';

// ============================================================================
// User / Employee Types
// ============================================================================

export interface User {
  userId: number;
  username?: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  profileImageUrl?: string | null;
  dashboardBannerUrl?: string | null;
  profileBannerUrl?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface UpdateProfileRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
  phone?: string;
}

export interface Employee {
  userId: number;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  office?: string;
  region?: string;
  team: string;
  managerId?: number | null;
  profilePicture?: string | null;
  role?: string;
  status: string;
  startDate: string;
  recruitedBy?: string | null;
  totalOverride: number;
  paidOverride: number;
}

export interface OrgUser {
  userId: number;
  firstName: string;
  lastName: string;
  position?: string | null;
  phone?: string | null;
  managerUserId?: number | null;
}

// ============================================================================
// Customer Types
// ============================================================================

export interface Customer {
  customerId: number;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  stateCode?: string;
  postalCode?: string;
}

// ============================================================================
// Sale Types
// ============================================================================

export interface SaleParticipant {
  userId: number;
  firstName: string;
  lastName: string;
  role: string;
  participationType?: string;
  splitPercent?: number;
  isPrimary?: boolean;
}

export interface SaleListItem {
  saleId: number;
  saleDate: string;
  saleStatus: SaleStatus;
  contractAmount: number;
  isActive: boolean;
  projectType: string;
  generationType?: string | null;
  customer: {
    customerId: number;
    firstName: string;
    lastName: string;
    city?: string;
    stateCode?: string;
  };
  participants: SaleParticipant[];
  createdAt: string;
}

export interface SolarDetail {
  industry: 'Solar';
  systemSizeKw?: number;
  purchaseOrLease?: string;
  installDate?: string;
  signedDate?: string;
  completedDate?: string;
  systemSoldValue?: number;
  ppwSold?: number;
  ppwGross?: number;
  ppwNet?: number;
  adderBreakdown?: string;
  adderTotal?: number;
  product?: string;
  financeRate?: number;
  commissionPaymentBasis?: string;
}

export interface PestDetail {
  industry: 'Pest';
  serviceType?: string;
  serviceFrequency?: string;
  signedDate?: string;
  completedDate?: string;
  contractStartDate?: string;
  contractEndDate?: string;
  contractLengthMonths?: number;
  initialServicePrice?: number;
  recurringPrice?: number;
  contractTotalValue?: number;
  commissionPaymentBasis?: string;
}

export interface RoofingDetail {
  industry: 'Roofing';
  installDate?: string;
  completionDate?: string;
  signedDate?: string;
  projectValue?: number;
  frontendReceivedAmount?: number;
  backendReceivedAmount?: number;
  financeRate?: number;
  commissionPaymentBasis?: string;
}

export interface FiberDetail {
  industry: 'Fiber';
  installDate?: string;
  signedDate?: string;
  completedDate?: string;
  location?: string;
  isp?: string;
  fiberPlan?: string;
  commissionPaymentBasis?: string;
}

export type IndustryDetail = SolarDetail | PestDetail | RoofingDetail | FiberDetail;

export interface ProjectPayout {
  projectPayoutId: number;
  milestoneNumber: number;
  milestoneName?: string;
  payoutAmount: number;
  payoutDate?: string;
  isPaid: boolean;
  paidAt?: string;
}

export interface SaleTotal {
  received?: number;
  payroll?: number;
  expenses?: number;
  saleProfit?: number;
}

export interface SaleNote {
  noteId: number;
  userId: number;
  firstName: string;
  lastName: string;
  noteText: string;
  createdAt: string;
}

export interface SaleCustomerNote extends SaleNote {
  contactMethod?: string;
  contactDate?: string;
  followUpDate?: string;
}

export interface SaleDetail {
  saleId: number;
  saleDate: string;
  saleStatus: SaleStatus;
  contractAmount: number;
  isActive: boolean;
  cancelledAt?: string;
  cancellationReason?: string;
  projectType: { projectTypeId: number; projectTypeName: string };
  generationType?: { generationTypeId: number; generationTypeName: string } | null;
  customer: Customer;
  participants: SaleParticipant[];
  saleTotal?: SaleTotal | null;
  projectPayouts: ProjectPayout[];
  allocations: UnifiedAllocation[];
  overrides: OverrideAllocationSummary[];
  clawbacks: ClawbackSummary[];
  industryDetail?: IndustryDetail | null;
  createdAt: string;
  updatedAt: string;
}

export interface SaleFilters {
  page?: number;
  pageSize?: number;
  status?: SaleStatus;
  dateFrom?: string;
  dateTo?: string;
  projectTypeId?: number;
  userId?: number;
  installerId?: number;
  customerId?: number;
  generationTypeId?: number;
  contractAmountMin?: number;
  contractAmountMax?: number;
  sortBy?: 'date' | 'amount' | 'status' | 'customer';
  sortDir?: 'asc' | 'desc';
}

// ============================================================================
// Allocation Types
// ============================================================================

export interface UnifiedAllocation {
  allocationId: number;
  industry: Industry;
  saleId: number;
  userId: number;
  allocationTypeId: number;
  milestoneNumber: number;
  allocatedAmount: number;
  isApproved: boolean;
  approvedAt?: string | null;
  approvedBy?: number | null;
  isPaid: boolean;
  paidAt?: string | null;
  payrollBatchId?: number | null;
  bucketId?: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface OverrideAllocationSummary {
  allocationId: number;
  userId: number;
  firstName: string;
  lastName: string;
  overrideLevel: number;
  allocatedAmount: number;
  isApproved: boolean;
  isPaid: boolean;
}

export interface ClawbackSummary {
  clawbackId: number;
  userId: number;
  clawbackAmount: number;
  clawbackReason: string;
  clawbackDate: string;
  isProcessed: boolean;
}

export interface AllocationFilters {
  page?: number;
  pageSize?: number;
  userId?: number;
  saleId?: number;
  isApproved?: boolean;
  isPaid?: boolean;
  milestoneNumber?: number;
  industry?: Industry;
  dateFrom?: string;
  dateTo?: string;
}

export interface BatchApproveItem {
  industry: Industry;
  allocationId: number;
}

export interface BatchApproveResult {
  approved: number;
  total: number;
  errors: string[];
}

// ============================================================================
// Override & Clawback
// ============================================================================

export interface OverrideAllocation {
  allocationId: number;
  saleId: number;
  userId: number;
  overrideLevel: number;
  allocatedAmount: number;
  isApproved: boolean;
  approvedAt?: string;
  isPaid: boolean;
  paidAt?: string;
  payrollBatchId?: number;
  bucketId?: number;
  createdAt: string;
}

export interface Clawback {
  clawbackId: number;
  saleId: number;
  userId: number;
  clawbackAmount: number;
  clawbackReason: string;
  clawbackDate: string;
  isProcessed: boolean;
  processedAt?: string;
  processedBy?: number;
  createdAt: string;
}

// ============================================================================
// Commission Rate Types
// ============================================================================

export interface CommissionRate {
  rateId: number;
  userId?: number;
  roleId?: number;
  roleName?: string;
  installerId?: number;
  stateCode?: string;
  percentMp1?: number;
  flatMp1?: number;
  percentMp2?: number;
  flatMp2?: number;
  effectiveStartDate: string;
  effectiveEndDate?: string;
  isActive: boolean;
}

export interface RateFilters {
  page?: number;
  pageSize?: number;
  userId?: number;
  roleId?: number;
  installerId?: number;
  stateCode?: string;
  isActive?: boolean;
}

export interface CreateRateRequest {
  userId?: number;
  roleId?: number;
  installerId?: number;
  stateCode?: string;
  percentMp1?: number;
  flatMp1?: number;
  percentMp2?: number;
  flatMp2?: number;
  effectiveStartDate: string;
  effectiveEndDate?: string;
}

export type CreateCommissionRateRequest = CreateRateRequest;
export type UpdateCommissionRateRequest = Partial<CreateRateRequest>;

// ============================================================================
// Dashboard Types
// ============================================================================

export interface DashboardStats {
  sales: {
    totalCount: number;
    totalValue: number;
    byStatus: Record<SaleStatus, { count: number; value: number }>;
  };
  commissions: {
    pending: number;
    approved: number;
    paid: number;
  };
  approvalQueueCount: number;
}

export interface LeaderboardEntry {
  userId: number;
  name: string;
  profileImageUrl?: string;
  totalCommissions: number;
  rank: number;
}

// ============================================================================
// Payroll Types
// ============================================================================

export interface PayrollBatch {
  batchId: number;
  description?: string;
  periodStart?: string;
  periodEnd?: string;
  status: BatchStatus;
  totalAmount: number;
  recordCount: number;
  submittedAt?: string;
  submittedBy?: number;
  approvedAt?: string;
  approvedBy?: number;
  exportedAt?: string;
  exportedBy?: number;
  paidAt?: string;
  paidBy?: number;
  createdAt: string;
  createdBy: number;
}

export interface PayrollBatchDetail extends PayrollBatch {
  allocations: UnifiedAllocation[];
}

export interface CreatePayrollBatchRequest {
  description?: string;
  periodStart?: string;
  periodEnd?: string;
}

export interface PayrollDealWithParticipants {
  saleId: number;
  customerFirstName: string;
  customerLastName: string;
  projectType: string;
  stage: string;
  status: string;
  signedDate?: string;
  updatedAt: string;
  createdAt: string;
  setters: Array<{ userId: number; firstName: string; lastName: string; splitPercent: number }>;
  closers: Array<{ userId: number; firstName: string; lastName: string; splitPercent: number }>;
}

// ============================================================================
// Paystub Types
// ============================================================================

export interface SalaryPayout {
  payoutId: number;
  payoutDate: string;
  payoutAmount: number;
  isPaid: boolean;
  paidAt?: string;
  hoursWorked?: number;
  overtimeHoursWorked?: number;
}

export interface CommissionPayoutEntry {
  type: Industry;
  amount: number;
  paidAt?: string;
}

export interface PaystubsResponse {
  paystubs: SalaryPayout[];
}

export interface CommissionHistoryResponse {
  roofing: CommissionPayoutEntry[];
  solar: CommissionPayoutEntry[];
  fiber: CommissionPayoutEntry[];
  pest: CommissionPayoutEntry[];
}

// ============================================================================
// Goal Types
// ============================================================================

export interface GoalProgress {
  goalId: number;
  goalName?: string;
  goalLevel: GoalLevel;
  goalTypeId: number;
  targetValue: number;
  currentValue: number;
  progressPercent: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  teamId?: number;
  officeId?: number;
  regionId?: number;
  userId?: number;
}

export interface GoalDetail extends GoalProgress {
  milestones?: Array<{
    milestoneId: number;
    targetValue: number;
    isAchieved: boolean;
    achievedAt?: string;
  }>;
  assignments?: Array<{ userId: number; firstName: string; lastName: string }>;
}

export interface GoalLeaderboardEntry {
  userId: number;
  name: string;
  currentValue: number;
  targetValue: number;
  progressPercent: number;
  rank: number;
}

export interface CreateGoalRequest {
  goalName: string;
  goalTypeId: number;
  goalLevel: GoalLevel;
  targetValue: number;
  startDate: string;
  endDate: string;
  teamId?: number;
  officeId?: number;
  regionId?: number;
}

// ============================================================================
// Announcement Types
// ============================================================================

export interface AnnouncementTarget {
  targetType: 'TEAM' | 'OFFICE' | 'REGION' | 'COMPANY';
  targetId?: number;
}

export interface Announcement {
  announcementId: number;
  title: string;
  body: string;
  priority?: string;
  isPinned: boolean;
  isActive: boolean;
  postDate: string;
  expiresAt?: string;
  isAcknowledged?: boolean;
  targets?: AnnouncementTarget[];
  createdAt: string;
}

export interface CreateAnnouncementRequest {
  title: string;
  body: string;
  priority?: string;
  expiresAt?: string;
  targets?: AnnouncementTarget[];
}

// ============================================================================
// Ticket Types
// ============================================================================

export interface TicketComment {
  commentId: number;
  ticketId: number;
  userId: number;
  firstName: string;
  lastName: string;
  body: string;
  createdAt: string;
}

export interface TicketStatusHistoryEntry {
  historyId: number;
  fromStatus?: TicketStatus;
  toStatus: TicketStatus;
  changedBy: number;
  changedAt: string;
}

export interface Ticket {
  ticketId: number;
  subject: string;
  description?: string;
  status: TicketStatus;
  priority?: TicketPriority;
  categoryId?: number;
  createdByUserId: number;
  assignedToUserId?: number;
  resolvedAt?: string;
  closedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface TicketDetail extends Ticket {
  comments: TicketComment[];
  statusHistory: TicketStatusHistoryEntry[];
}

export interface CreateTicketRequest {
  subject: string;
  description?: string;
  priority?: TicketPriority;
  categoryId?: number;
}

export interface TicketFilters {
  page?: number;
  pageSize?: number;
  status?: TicketStatus;
  priority?: TicketPriority;
  assignedTo?: number;
}

// ============================================================================
// Admin / Collaborator Types
// ============================================================================

export interface CollaboratorBase {
  id: number;
  name: string;
  isActive: boolean;
  isPreferred?: boolean;
  notes?: string;
}

export interface Installer extends CollaboratorBase {
  coverageAreas?: Array<{ projectType: string; stateCode: string }>;
}

export interface Dealer extends CollaboratorBase {}

export interface FinanceCompany extends CollaboratorBase {}

export interface Partner extends CollaboratorBase {}

// ============================================================================
// Team Types
// ============================================================================

export interface TeamMember {
  userId: number;
  firstName: string;
  lastName: string;
  title?: string;
  teamName?: string;
  officeName?: string;
  regionName?: string;
  saleCount: number;
  totalCommissions: number;
  profileImageUrl?: string;
}

export interface TeamPerformance {
  period: { start: string; end: string };
  teamSize: number;
  totalSales: number;
  totalValue: number;
  totalCommissions: number;
  byMember: Array<{
    userId: number;
    name: string;
    sales: number;
    commissions: number;
  }>;
}

// ============================================================================
// Profile Types
// ============================================================================

export interface CommissionSummary {
  period: { start: string; end: string };
  byIndustry: Array<{
    industry: Industry;
    total: number;
    pending: number;
    approved: number;
    paid: number;
    count: number;
  }>;
  grandTotal: number;
  overrides?: {
    total: number;
    pending: number;
    paid: number;
  };
  clawbacks: number;
}

// ============================================================================
// Company Settings / Reference Data
// ============================================================================

export interface ProjectTypeRef {
  id: number;
  name: string;
}

export interface SalesRoleRef {
  id: number;
  name: string;
}

export interface GenerationTypeRef {
  id: number;
  name: string;
}

export interface AllocationTypeRef {
  id: number;
  name: string;
}

export interface CompanySettings {
  subdomain: string;
  projectTypes: ProjectTypeRef[];
  salesRoles: SalesRoleRef[];
  generationTypes: GenerationTypeRef[];
  allocationTypes: AllocationTypeRef[];
}
