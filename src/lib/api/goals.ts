import { apiClient } from './client';
import type {
  ApiResponse,
  PaginatedResponse,
  GoalProgress,
  GoalDetail,
  GoalLeaderboardEntry,
  CreateGoalRequest,
} from '@/types';

export async function getGoals(): Promise<PaginatedResponse<GoalProgress>> {
  return apiClient.get<PaginatedResponse<GoalProgress>>('/api/goals');
}

export async function getGoal(goalId: number): Promise<ApiResponse<GoalDetail>> {
  return apiClient.get<ApiResponse<GoalDetail>>(`/api/goals/${goalId}`);
}

export async function getMyGoalProgress(): Promise<ApiResponse<GoalProgress[]>> {
  return apiClient.get<ApiResponse<GoalProgress[]>>('/api/goals/my-progress');
}

export async function getGoalLeaderboard(
  leaderboardId: number,
): Promise<ApiResponse<GoalLeaderboardEntry[]>> {
  return apiClient.get<ApiResponse<GoalLeaderboardEntry[]>>(
    `/api/goals/leaderboard/${leaderboardId}`,
  );
}

export async function createGoal(data: CreateGoalRequest): Promise<ApiResponse<GoalDetail>> {
  return apiClient.post<ApiResponse<GoalDetail>>('/api/goals', data);
}

export async function updateGoal(
  goalId: number,
  data: Partial<CreateGoalRequest>,
): Promise<ApiResponse<GoalDetail>> {
  return apiClient.put<ApiResponse<GoalDetail>>(`/api/goals/${goalId}`, data);
}

export async function assignUsersToGoal(
  goalId: number,
  userIds: number[],
): Promise<ApiResponse<unknown>> {
  return apiClient.post<ApiResponse<unknown>>(`/api/goals/${goalId}/assign`, { userIds });
}
